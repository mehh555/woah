using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Woah.Api.Contracts.Sessions;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Infrastructure.Persistence.Models;

namespace Woah.Api.Services;

public class SessionService : ISessionService
{
    private readonly WoahDbContext _dbContext;
    private readonly ILobbyPlaylistStore _lobbyPlaylistStore;
    private readonly IAnswerNormalizer _answerNormalizer;
    private readonly IScoreCalculator _scoreCalculator;

    public SessionService(
        WoahDbContext dbContext,
        ILobbyPlaylistStore lobbyPlaylistStore,
        IAnswerNormalizer answerNormalizer,
        IScoreCalculator scoreCalculator)
    {
        _dbContext = dbContext;
        _lobbyPlaylistStore = lobbyPlaylistStore;
        _answerNormalizer = answerNormalizer;
        _scoreCalculator = scoreCalculator;
    }

    public async Task<StartSessionResponse> StartSessionAsync(
        string lobbyCode,
        StartSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedLobbyCode = NormalizeLobbyCode(lobbyCode);
        var now = DateTime.UtcNow;

        var lobby = await _dbContext.Lobbies
            .Include(x => x.LobbyPlayers)
            .FirstOrDefaultAsync(x => x.Code == normalizedLobbyCode, cancellationToken);

        if (lobby is null)
        {
            throw new InvalidOperationException("Lobby not found.");
        }

        if (lobby.Status != "Waiting")
        {
            throw new InvalidOperationException("Only waiting lobbies can start a session.");
        }

        if (lobby.HostPlayerId != request.HostPlayerId)
        {
            throw new InvalidOperationException("Only the host can start the session.");
        }

        var activePlayers = GetActivePlayers(lobby);

        if (!activePlayers.Any(x => x.PlayerId == request.HostPlayerId))
        {
            throw new InvalidOperationException("Host is not active in this lobby.");
        }

        if (activePlayers.Count < 1)
        {
            throw new InvalidOperationException("At least one active player is required.");
        }

        var existingActiveSession = await _dbContext.GameSessions
            .AnyAsync(x => x.LobbyId == lobby.LobbyId && x.EndedAt == null, cancellationToken);

        if (existingActiveSession)
        {
            throw new InvalidOperationException("An active session already exists for this lobby.");
        }

        var playlist = await _dbContext.Playlists
            .FirstOrDefaultAsync(
                x => x.PlaylistId == request.PlaylistId && x.OwnerPlayerId == request.HostPlayerId,
                cancellationToken);

        if (playlist is null)
        {
            throw new InvalidOperationException("Playlist placeholder not found for this host.");
        }

        var draftTracks = _lobbyPlaylistStore.GetTracks(lobby.Code).ToList();

        if (draftTracks.Count == 0)
        {
            throw new InvalidOperationException("Lobby playlist must contain at least one track before starting the session.");
        }

        var roundDurationSeconds = Math.Clamp(request.RoundDurationSeconds, 5, 60);

        Shuffle(draftTracks);

        var settingsJson = JsonSerializer.Serialize(new
        {
            roundDurationSeconds = roundDurationSeconds
        });

        var session = new GameSessionEntity
        {
            SessionId = Guid.NewGuid(),
            LobbyId = lobby.LobbyId,
            PlaylistId = playlist.PlaylistId,
            StartedAt = now,
            EndedAt = null,
            SettingsJson = settingsJson,
            Lobby = lobby,
            Playlist = playlist,
            Rounds = new List<RoundEntity>()
        };

        for (var index = 0; index < draftTracks.Count; index++)
        {
            var track = draftTracks[index];
            var isFirstRound = index == 0;

            var round = new RoundEntity
            {
                RoundId = Guid.NewGuid(),
                SessionId = session.SessionId,
                Session = session,
                RoundNo = index + 1,
                PlaylistId = playlist.PlaylistId,
                PlaylistItemNumber = index + 1,
                PreviewUrl = track.PreviewUrl,
                AnswerTitle = track.Title,
                AnswerNorm = _answerNormalizer.Normalize(track.Title),
                StartedAt = isFirstRound ? now : now,
                EndsAt = isFirstRound ? now.AddSeconds(roundDurationSeconds) : null,
                RevealedAt = null,
                State = isFirstRound ? "Playing" : "Pending",
                CorrectAnswers = new List<RoundCorrectAnswerEntity>()
            };

            session.Rounds.Add(round);
        }

        lobby.Status = "InGame";

        _dbContext.GameSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _lobbyPlaylistStore.Clear(lobby.Code);

        return new StartSessionResponse
        {
            SessionId = session.SessionId,
            LobbyId = lobby.LobbyId,
            PlaylistId = playlist.PlaylistId,
            HostPlayerId = request.HostPlayerId,
            StartedAt = session.StartedAt,
            LobbyStatus = lobby.Status,
            RoundCount = session.Rounds.Count
        };
    }

    public async Task<GetSessionStateResponse> GetSessionStateAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await LoadSessionAggregateAsync(sessionId, cancellationToken);

        await EnsureSessionProgressAsync(session, cancellationToken);

        return await BuildSessionStateAsync(session, cancellationToken);
    }

    public async Task<SubmitAnswerResponse> SubmitAnswerAsync(
        Guid sessionId,
        SubmitAnswerRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await LoadSessionAggregateAsync(sessionId, cancellationToken);

        await EnsureSessionProgressAsync(session, cancellationToken);

        if (session.EndedAt is not null)
        {
            return new SubmitAnswerResponse
            {
                Accepted = false,
                IsCorrect = false,
                AlreadyAnswered = false,
                PointsAwarded = 0,
                Message = "Session has already finished."
            };
        }

        var lobby = await _dbContext.Lobbies
            .Include(x => x.LobbyPlayers)
            .FirstAsync(x => x.LobbyId == session.LobbyId, cancellationToken);

        var playerIsActive = GetActivePlayers(lobby)
            .Any(x => x.PlayerId == request.PlayerId);

        if (!playerIsActive)
        {
            return new SubmitAnswerResponse
            {
                Accepted = false,
                IsCorrect = false,
                AlreadyAnswered = false,
                PointsAwarded = 0,
                Message = "Player is not active in this lobby."
            };
        }

        var currentRound = session.Rounds?
            .OrderBy(x => x.RoundNo)
            .FirstOrDefault(x => x.State == "Playing");

        if (currentRound is null)
        {
            return new SubmitAnswerResponse
            {
                Accepted = false,
                IsCorrect = false,
                AlreadyAnswered = false,
                PointsAwarded = 0,
                Message = "There is no active round to answer."
            };
        }

        if (currentRound.EndsAt is not null && DateTime.UtcNow >= currentRound.EndsAt.Value)
        {
            await EnsureSessionProgressAsync(session, cancellationToken);

            return new SubmitAnswerResponse
            {
                Accepted = false,
                IsCorrect = false,
                AlreadyAnswered = false,
                PointsAwarded = 0,
                Message = "Round has already ended."
            };
        }

        var alreadyAnsweredCorrectly = (currentRound.CorrectAnswers ?? new List<RoundCorrectAnswerEntity>())
            .Any(x => x.PlayerId == request.PlayerId);

        if (alreadyAnsweredCorrectly)
        {
            return new SubmitAnswerResponse
            {
                Accepted = true,
                IsCorrect = true,
                AlreadyAnswered = true,
                PointsAwarded = 0,
                Message = "Player has already answered this round correctly."
            };
        }

        var normalizedAnswer = _answerNormalizer.Normalize(request.Answer);

        if (!string.Equals(normalizedAnswer, currentRound.AnswerNorm, StringComparison.Ordinal))
        {
            return new SubmitAnswerResponse
            {
                Accepted = true,
                IsCorrect = false,
                AlreadyAnswered = false,
                PointsAwarded = 0,
                Message = "Incorrect answer."
            };
        }

        var roundDurationSeconds = GetRoundDurationSeconds(session);
        var elapsedSeconds = Math.Max((DateTime.UtcNow - currentRound.StartedAt).TotalSeconds, 0);
        var points = _scoreCalculator.Calculate(roundDurationSeconds, elapsedSeconds);

        var answer = new RoundCorrectAnswerEntity
        {
            RoundId = currentRound.RoundId,
            PlayerId = request.PlayerId,
            AnsweredAt = DateTime.UtcNow,
            Points = points
        };

        _dbContext.RoundCorrectAnswers.Add(answer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SubmitAnswerResponse
        {
            Accepted = true,
            IsCorrect = true,
            AlreadyAnswered = false,
            PointsAwarded = points,
            Message = "Correct answer."
        };
    }

    public async Task<GetSessionStateResponse> AdvanceSessionAsync(
        Guid sessionId,
        AdvanceSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await LoadSessionAggregateAsync(sessionId, cancellationToken);

        var lobby = await _dbContext.Lobbies
            .FirstAsync(x => x.LobbyId == session.LobbyId, cancellationToken);

        if (lobby.HostPlayerId != request.HostPlayerId)
        {
            throw new InvalidOperationException("Only the host can advance the session.");
        }

        await EnsureSessionProgressAsync(session, cancellationToken);

        if (session.EndedAt is not null)
        {
            return await BuildSessionStateAsync(session, cancellationToken);
        }

        var orderedRounds = (session.Rounds ?? new List<RoundEntity>())
            .OrderBy(x => x.RoundNo)
            .ToList();

        var currentPlaying = orderedRounds.FirstOrDefault(x => x.State == "Playing");

        if (currentPlaying is not null)
        {
            currentPlaying.State = "Revealed";
            currentPlaying.RevealedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return await BuildSessionStateAsync(session, cancellationToken);
        }

        var currentRevealed = orderedRounds.FirstOrDefault(x => x.State == "Revealed");

        if (currentRevealed is not null)
        {
            currentRevealed.State = "Finished";

            var nextPending = orderedRounds.FirstOrDefault(x => x.State == "Pending");

            if (nextPending is null)
            {
                session.EndedAt = DateTime.UtcNow;
                lobby.Status = "Finished";
            }
            else
            {
                var roundDurationSeconds = GetRoundDurationSeconds(session);
                nextPending.State = "Playing";
                nextPending.StartedAt = DateTime.UtcNow;
                nextPending.EndsAt = nextPending.StartedAt.AddSeconds(roundDurationSeconds);
                nextPending.RevealedAt = null;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return await BuildSessionStateAsync(session, cancellationToken);
    }

    private async Task<GameSessionEntity> LoadSessionAggregateAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _dbContext.GameSessions
            .Include(x => x.Rounds)
                .ThenInclude(x => x.CorrectAnswers)
            .FirstOrDefaultAsync(x => x.SessionId == sessionId, cancellationToken);

        if (session is null)
        {
            throw new InvalidOperationException("Session not found.");
        }

        return session;
    }

    private async Task EnsureSessionProgressAsync(GameSessionEntity session, CancellationToken cancellationToken)
    {
        if (session.EndedAt is not null)
        {
            return;
        }

        var currentPlaying = (session.Rounds ?? new List<RoundEntity>())
            .OrderBy(x => x.RoundNo)
            .FirstOrDefault(x => x.State == "Playing");

        if (currentPlaying is not null &&
            currentPlaying.EndsAt is not null &&
            DateTime.UtcNow >= currentPlaying.EndsAt.Value)
        {
            currentPlaying.State = "Revealed";
            currentPlaying.RevealedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<GetSessionStateResponse> BuildSessionStateAsync(
        GameSessionEntity session,
        CancellationToken cancellationToken)
    {
        var lobby = await _dbContext.Lobbies
            .Include(x => x.LobbyPlayers)
            .FirstAsync(x => x.LobbyId == session.LobbyId, cancellationToken);

        var orderedRounds = (session.Rounds ?? new List<RoundEntity>())
            .OrderBy(x => x.RoundNo)
            .ToList();

        var currentRound = orderedRounds
            .FirstOrDefault(x => x.State == "Playing")
            ?? orderedRounds.FirstOrDefault(x => x.State == "Revealed");

        var activePlayers = GetActivePlayers(lobby);

        var leaderboard = activePlayers
            .Select(player =>
            {
                var playerAnswers = orderedRounds
                    .SelectMany(x => x.CorrectAnswers ?? new List<RoundCorrectAnswerEntity>())
                    .Where(x => x.PlayerId == player.PlayerId)
                    .ToList();

                return new SessionLeaderboardEntryResponse
                {
                    PlayerId = player.PlayerId,
                    Nick = player.Nick,
                    Score = playerAnswers.Sum(x => x.Points),
                    CorrectAnswers = playerAnswers.Count
                };
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.CorrectAnswers)
            .ThenBy(x => x.Nick)
            .ToList();

        return new GetSessionStateResponse
        {
            SessionId = session.SessionId,
            LobbyId = session.LobbyId,
            LobbyStatus = lobby.Status,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            IsFinished = session.EndedAt is not null,
            TotalRounds = orderedRounds.Count,
            CompletedRounds = orderedRounds.Count(x => x.State == "Finished"),
            CurrentRound = currentRound is null
                ? null
                : new SessionRoundResponse
                {
                    RoundId = currentRound.RoundId,
                    RoundNo = currentRound.RoundNo,
                    State = currentRound.State,
                    PreviewUrl = currentRound.PreviewUrl,
                    StartedAt = currentRound.StartedAt,
                    EndsAt = currentRound.EndsAt,
                    RevealedAt = currentRound.RevealedAt,
                    AnswerTitle = currentRound.State == "Revealed" || currentRound.State == "Finished"
                        ? currentRound.AnswerTitle
                        : null,
                    CorrectAnswerCount = (currentRound.CorrectAnswers ?? new List<RoundCorrectAnswerEntity>()).Count
                },
            Leaderboard = leaderboard
        };
    }

    private static List<LobbyPlayerEntity> GetActivePlayers(LobbyEntity lobby)
    {
        return (lobby.LobbyPlayers ?? new List<LobbyPlayerEntity>())
            .Where(x => x.LeftAt == null)
            .OrderBy(x => x.JoinedAt)
            .ToList();
    }

    private static string NormalizeLobbyCode(string lobbyCode)
    {
        return lobbyCode.Trim().ToUpperInvariant();
    }

    private static int GetRoundDurationSeconds(GameSessionEntity session)
    {
        if (string.IsNullOrWhiteSpace(session.SettingsJson))
        {
            return 15;
        }

        using var document = JsonDocument.Parse(session.SettingsJson);

        if (document.RootElement.TryGetProperty("roundDurationSeconds", out var property) &&
            property.TryGetInt32(out var value))
        {
            return Math.Clamp(value, 5, 60);
        }

        return 15;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}