using Microsoft.EntityFrameworkCore;
using Woah.Api.Contracts.Sessions;
using Woah.Api.Domain;
using Woah.Api.Exceptions;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Infrastructure.Persistence.Models;
using Woah.Api.Services.Notifications;
using Woah.Api.Services.Playlist;

namespace Woah.Api.Services.Session;

public class SessionService : ISessionService
{
    private readonly WoahDbContext _dbContext;
    private readonly ILobbyPlaylistStore _playlistStore;
    private readonly IAnswerNormalizer _normalizer;
    private readonly IScoreCalculator _scoreCalculator;
    private readonly ISessionProgressEngine _progressEngine;
    private readonly ISessionStateBuilder _stateBuilder;
    private readonly IGameNotifier _notifier;
    private readonly ILogger<SessionService> _logger;

    public SessionService(
        WoahDbContext dbContext,
        ILobbyPlaylistStore playlistStore,
        IAnswerNormalizer normalizer,
        IScoreCalculator scoreCalculator,
        ISessionProgressEngine progressEngine,
        ISessionStateBuilder stateBuilder,
        IGameNotifier notifier,
        ILogger<SessionService> logger)
    {
        _dbContext = dbContext;
        _playlistStore = playlistStore;
        _normalizer = normalizer;
        _scoreCalculator = scoreCalculator;
        _progressEngine = progressEngine;
        _stateBuilder = stateBuilder;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<StartSessionResponse> StartSessionAsync(string lobbyCode, StartSessionRequest request, CancellationToken ct = default)
    {
        var lobby = await GetLobbyWithPlayersAsync(lobbyCode.NormalizeCode(), ct);

        ValidateSessionStart(lobby, request, _logger);

        var playlist = await _dbContext.Playlists
            .FirstOrDefaultAsync(x => x.PlaylistId == request.PlaylistId && x.OwnerPlayerId == request.HostPlayerId, ct);

        if (playlist is null)
        {
            _logger.LogWarning("Start rejected — playlist {PlaylistId} not found for host {HostPlayerId}", request.PlaylistId, request.HostPlayerId);
            throw new NotFoundException("Playlist not found for this host.");
        }

        var tracks = _playlistStore.GetTracks(lobby.Code).ToList();

        if (tracks.Count == 0)
        {
            _logger.LogWarning("Start rejected — empty playlist for lobby {LobbyCode}", lobby.Code);
            throw new BadRequestException("Playlist must contain at least one track before starting.");
        }

        var settings = new SessionSettings(
            Math.Clamp(request.RoundDurationSeconds, 5, 15));

        Shuffle(tracks);

        await using var tx = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        try
        {
            if (await _dbContext.GameSessions.AnyAsync(x => x.LobbyId == lobby.LobbyId && x.EndedAt == null, ct))
            {
                _logger.LogWarning("Start rejected — active session already exists for lobby {LobbyCode}", lobby.Code);
                throw new BadRequestException("An active session already exists for this lobby.");
            }

            var session = BuildSession(lobby, playlist, tracks, settings, _normalizer);
            lobby.Status = LobbyStatus.InGame;

            _dbContext.GameSessions.Add(session);
            await _dbContext.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _playlistStore.Clear(lobby.Code);

            _logger.LogInformation(
                "Session {SessionId} started in lobby {LobbyCode} with {RoundCount} rounds (duration={Duration}s)",
                session.SessionId, lobby.Code, session.Rounds.Count, settings.RoundDurationSeconds);

            await _notifier.SessionStarted(lobby.Code, session.SessionId);

            return new StartSessionResponse
            {
                SessionId = session.SessionId,
                LobbyId = lobby.LobbyId,
                PlaylistId = playlist.PlaylistId,
                HostPlayerId = request.HostPlayerId,
                StartedAt = session.StartedAt,
                LobbyStatus = lobby.Status.ToString(),
                RoundCount = session.Rounds.Count
            };
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<GetSessionStateResponse> GetSessionStateAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await LoadSessionAsync(sessionId, ct);
        await _progressEngine.EnsurePlayingToRevealedAsync(session, ct);
        return await _stateBuilder.BuildAsync(session, ct);
    }

    public async Task<SubmitAnswerResponse> SubmitAnswerAsync(Guid sessionId, SubmitAnswerRequest request, CancellationToken ct = default)
    {
        var session = await LoadSessionAsync(sessionId, ct);
        await _progressEngine.EnsurePlayingToRevealedAsync(session, ct);

        if (session.EndedAt is not null)
        {
            _logger.LogWarning("Answer rejected — session {SessionId} already finished (PlayerId={PlayerId})", sessionId, request.PlayerId);
            return Reject("Session has already finished.");
        }

        var lobby = await _dbContext.Lobbies
            .Include(x => x.LobbyPlayers)
            .FirstAsync(x => x.LobbyId == session.LobbyId, ct);

        if (!lobby.ActivePlayers().Any(x => x.PlayerId == request.PlayerId))
        {
            _logger.LogWarning("Answer rejected — player {PlayerId} not active in lobby for session {SessionId}", request.PlayerId, sessionId);
            return Reject("Player is not active in this lobby.");
        }

        var round = SessionProgressEngine.OrderedRounds(session)
            .FirstOrDefault(x => x.State == RoundState.Playing);

        if (round is null)
        {
            _logger.LogWarning("Answer rejected — no active round in session {SessionId} (PlayerId={PlayerId})", sessionId, request.PlayerId);
            return Reject("There is no active round to answer.");
        }

        if (round.EndsAt is not null && DateTime.UtcNow >= round.EndsAt.Value)
        {
            _logger.LogDebug("Answer rejected — round {RoundNo} timer expired in session {SessionId} (PlayerId={PlayerId})", round.RoundNo, sessionId, request.PlayerId);
            await _progressEngine.EnsurePlayingToRevealedAsync(session, ct);
            return Reject("Round has already ended.");
        }

        if (round.CorrectAnswers.Any(x => x.PlayerId == request.PlayerId))
        {
            _logger.LogDebug("Answer rejected — player {PlayerId} already answered round {RoundNo} in session {SessionId}", request.PlayerId, round.RoundNo, sessionId);
            return AlreadyAnswered();
        }

        if (!string.Equals(_normalizer.Normalize(request.Answer), round.AnswerNorm, StringComparison.Ordinal))
        {
            _logger.LogDebug("Incorrect answer from {PlayerId} in session {SessionId} round {RoundNo}", request.PlayerId, sessionId, round.RoundNo);
            return new SubmitAnswerResponse { Accepted = true, IsCorrect = false, AlreadyAnswered = false, PointsAwarded = 0, Message = "Incorrect answer." };
        }

        var settings = SessionSettings.Parse(session.SettingsJson);
        var elapsed = Math.Max((DateTime.UtcNow - round.StartedAt).TotalSeconds, 0);
        var points = _scoreCalculator.Calculate(settings.RoundDurationSeconds, elapsed);

        var player = lobby.ActivePlayers().First(x => x.PlayerId == request.PlayerId);

        try
        {
            _dbContext.RoundCorrectAnswers.Add(new RoundCorrectAnswerEntity
            {
                RoundId = round.RoundId,
                PlayerId = request.PlayerId,
                AnsweredAt = DateTime.UtcNow,
                Points = points
            });
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            _logger.LogDebug("Answer rejected (concurrency) — player {PlayerId} already answered round {RoundNo} in session {SessionId}", request.PlayerId, round.RoundNo, sessionId);
            return AlreadyAnswered();
        }

        _logger.LogInformation(
            "Correct answer from {Nick} ({PlayerId}) in session {SessionId} round {RoundNo} — {Points} pts",
            player.Nick, request.PlayerId, sessionId, round.RoundNo, points);

        await _notifier.PlayerAnsweredCorrectly(sessionId, request.PlayerId, player.Nick, points);

        return new SubmitAnswerResponse { Accepted = true, IsCorrect = true, AlreadyAnswered = false, PointsAwarded = points, Message = "Correct answer." };
    }

    public async Task<GetSessionStateResponse> AdvanceSessionAsync(Guid sessionId, AdvanceSessionRequest request, CancellationToken ct = default)
    {
        var session = await LoadSessionAsync(sessionId, ct);
        var lobby = await _dbContext.Lobbies.FirstAsync(x => x.LobbyId == session.LobbyId, ct);

        if (lobby.HostPlayerId != request.HostPlayerId)
        {
            _logger.LogWarning("Advance rejected — player {PlayerId} is not host of session {SessionId}", request.HostPlayerId, sessionId);
            throw new ForbiddenException("Only the host can advance the session.");
        }

        await _progressEngine.EnsurePlayingToRevealedAsync(session, ct);

        if (session.EndedAt is not null)
            return await _stateBuilder.BuildAsync(session, ct);

        var rounds = SessionProgressEngine.OrderedRounds(session);

        var playing = rounds.FirstOrDefault(x => x.State == RoundState.Playing);
        if (playing is not null)
        {
            playing.State = RoundState.Revealed;
            playing.RevealedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Round {RoundNo} revealed (skipped) in session {SessionId}", playing.RoundNo, sessionId);

            await _notifier.SessionUpdated(sessionId);
            return await _stateBuilder.BuildAsync(session, ct);
        }

        var revealed = rounds.FirstOrDefault(x => x.State == RoundState.Revealed);
        if (revealed is not null)
        {
            await _progressEngine.AdvanceFromRevealedAsync(session, lobby, revealed, rounds, ct);

            if (session.EndedAt is not null)
                _logger.LogInformation("Session {SessionId} finished in lobby {LobbyCode}", sessionId, lobby.Code);
        }

        return await _stateBuilder.BuildAsync(session, ct);
    }

    private async Task<GameSessionEntity> LoadSessionAsync(Guid sessionId, CancellationToken ct) =>
        await _dbContext.GameSessions
            .Include(x => x.Rounds).ThenInclude(x => x.CorrectAnswers)
            .FirstOrDefaultAsync(x => x.SessionId == sessionId, ct)
        ?? throw new NotFoundException("Session not found.");

    private async Task<LobbyEntity> GetLobbyWithPlayersAsync(string normalizedCode, CancellationToken ct) =>
        await _dbContext.Lobbies
            .Include(x => x.LobbyPlayers)
            .FirstOrDefaultAsync(x => x.Code == normalizedCode, ct)
        ?? throw new NotFoundException("Lobby not found.");

    private static void ValidateSessionStart(LobbyEntity lobby, StartSessionRequest request, ILogger logger)
    {
        if (lobby.Status != LobbyStatus.Waiting)
        {
            logger.LogWarning("Start rejected — lobby {LobbyCode} is not waiting (Status={Status})", lobby.Code, lobby.Status);
            throw new BadRequestException("Only waiting lobbies can start a session.");
        }

        if (lobby.HostPlayerId != request.HostPlayerId)
        {
            logger.LogWarning("Start rejected — player {PlayerId} is not host of lobby {LobbyCode}", request.HostPlayerId, lobby.Code);
            throw new ForbiddenException("Only the host can start the session.");
        }

        if (!lobby.ActivePlayers().Any(x => x.PlayerId == request.HostPlayerId))
        {
            logger.LogWarning("Start rejected — host {PlayerId} is not active in lobby {LobbyCode}", request.HostPlayerId, lobby.Code);
            throw new BadRequestException("Host is not active in this lobby.");
        }
    }

    private static GameSessionEntity BuildSession(
        LobbyEntity lobby,
        PlaylistEntity playlist,
        IList<LobbyDraftTrack> tracks,
        SessionSettings settings,
        IAnswerNormalizer normalizer)
    {
        var now = DateTime.UtcNow;
        var session = new GameSessionEntity
        {
            SessionId = Guid.NewGuid(),
            LobbyId = lobby.LobbyId,
            PlaylistId = playlist.PlaylistId,
            StartedAt = now,
            EndedAt = null,
            SettingsJson = settings.Serialize(),
            Lobby = lobby,
            Playlist = playlist,
            Rounds = new List<RoundEntity>()
        };

        for (var i = 0; i < tracks.Count; i++)
        {
            var isFirst = i == 0;
            session.Rounds.Add(new RoundEntity
            {
                RoundId = Guid.NewGuid(),
                SessionId = session.SessionId,
                Session = session,
                RoundNo = i + 1,
                PlaylistId = playlist.PlaylistId,
                PlaylistItemNumber = i + 1,
                PreviewUrl = tracks[i].PreviewUrl,
                AnswerTitle = tracks[i].Title,
                AnswerArtist = tracks[i].Artist,
                AnswerNorm = normalizer.Normalize(tracks[i].Title),
                ArtworkUrl = tracks[i].ArtworkUrl,
                ItunesTrackId = tracks[i].TrackId,
                StartedAt = now,
                EndsAt = isFirst ? now.AddSeconds(settings.RoundDurationSeconds) : null,
                RevealedAt = null,
                State = isFirst ? RoundState.Playing : RoundState.Pending,
                CorrectAnswers = new List<RoundCorrectAnswerEntity>()
            });
        }

        return session;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static SubmitAnswerResponse Reject(string message) =>
        new() { Accepted = false, IsCorrect = false, AlreadyAnswered = false, PointsAwarded = 0, Message = message };

    private static SubmitAnswerResponse AlreadyAnswered() =>
        new() { Accepted = true, IsCorrect = true, AlreadyAnswered = true, PointsAwarded = 0, Message = "Player has already answered this round correctly." };
}