using Microsoft.EntityFrameworkCore;
using Woah.Api.Contracts.Sessions;
using Woah.Api.Domain;
using Woah.Api.Exceptions;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Infrastructure.Persistence.Models;
using Woah.Api.Services.Notifications;

namespace Woah.Api.Services.Session;

public class SessionService : ISessionService
{
    private readonly WoahDbContext _dbContext;
    private readonly IAnswerNormalizer _normalizer;
    private readonly IAnswerEvaluator _answerEvaluator;
    private readonly IScoreCalculator _scoreCalculator;
    private readonly ISessionFactory _sessionFactory;
    private readonly ISessionStartValidator _startValidator;
    private readonly ISessionProgressEngine _progressEngine;
    private readonly ISessionStateBuilder _stateBuilder;
    private readonly IGameNotifier _notifier;
    private readonly ILogger<SessionService> _logger;

    public SessionService(
        WoahDbContext dbContext,
        IAnswerNormalizer normalizer,
        IAnswerEvaluator answerEvaluator,
        IScoreCalculator scoreCalculator,
        ISessionFactory sessionFactory,
        ISessionStartValidator startValidator,
        ISessionProgressEngine progressEngine,
        ISessionStateBuilder stateBuilder,
        IGameNotifier notifier,
        ILogger<SessionService> logger)
    {
        _dbContext = dbContext;
        _normalizer = normalizer;
        _answerEvaluator = answerEvaluator;
        _scoreCalculator = scoreCalculator;
        _sessionFactory = sessionFactory;
        _startValidator = startValidator;
        _progressEngine = progressEngine;
        _stateBuilder = stateBuilder;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<StartSessionResponse> StartSessionAsync(string lobbyCode, StartSessionRequest request, CancellationToken ct = default)
    {
        var lobby = await _dbContext.Lobbies.GetLobbyWithPlayersAsync(lobbyCode.NormalizeCode(), ct);

        _startValidator.Validate(lobby, request);

        var playlist = await _dbContext.Playlists
            .FirstOrDefaultAsync(x => x.PlaylistId == request.PlaylistId && x.OwnerPlayerId == request.HostPlayerId, ct)
            ?? throw new NotFoundException("Playlist not found for this host.");

        var tracks = await _dbContext.PlaylistTracks
            .Where(x => x.PlaylistId == playlist.PlaylistId)
            .OrderBy(x => x.AddedAt)
            .ToListAsync(ct);

        if (tracks.Count == 0)
            throw new BadRequestException("Playlist must contain at least one track before starting.");

        var settings = new SessionSettings(Math.Clamp(request.RoundDurationSeconds, 5, 15));

        await using var tx = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        try
        {
            if (await _dbContext.GameSessions.AnyAsync(x => x.LobbyId == lobby.LobbyId && x.EndedAt == null, ct))
                throw new BadRequestException("An active session already exists for this lobby.");

            var session = _sessionFactory.Create(lobby, playlist, tracks, settings);
            lobby.Status = LobbyStatus.InGame;

            _dbContext.GameSessions.Add(session);
            await _dbContext.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

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
            return SubmitAnswerResponse.Rejected("Session has already finished.");

        var lobby = await _dbContext.Lobbies
            .Include(x => x.LobbyPlayers)
            .FirstAsync(x => x.LobbyId == session.LobbyId, ct);

        var activePlayers = lobby.ActivePlayers();

        if (!activePlayers.Any(x => x.PlayerId == request.PlayerId))
            return SubmitAnswerResponse.Rejected("Player is not active in this lobby.");

        var round = SessionProgressEngine.OrderedRounds(session)
            .FirstOrDefault(x => x.State == RoundState.Playing);

        if (round is null)
            return SubmitAnswerResponse.Rejected("There is no active round to answer.");

        if (round.EndsAt is not null && DateTime.UtcNow >= round.EndsAt.Value)
        {
            await _progressEngine.EnsurePlayingToRevealedAsync(session, ct);
            return SubmitAnswerResponse.Rejected("Round has already ended.");
        }

        var normalizedGuess = _normalizer.Normalize(request.Answer);
        var match = _answerEvaluator.Evaluate(normalizedGuess, round.AnswerNorm, round.AnswerArtistNorm);

        var settings = SessionSettings.Parse(session.SettingsJson);
        var elapsed = Math.Max((DateTime.UtcNow - round.StartedAt).TotalSeconds, 0);
        var pointsPerMatch = _scoreCalculator.Calculate(settings.RoundDurationSeconds, elapsed);
        var player = activePlayers.First(x => x.PlayerId == request.PlayerId);

        bool newTitle, newArtist;
        int points;

        for (var attempt = 0; ; attempt++)
        {
            var existing = round.CorrectAnswers.FirstOrDefault(x => x.PlayerId == request.PlayerId);
            var alreadyGotTitle = existing?.GotTitle ?? false;
            var alreadyGotArtist = existing?.GotArtist ?? false;

            if (alreadyGotTitle && alreadyGotArtist)
                return SubmitAnswerResponse.AlreadyFullyAnswered();

            newTitle = match.TitleMatched && !alreadyGotTitle;
            newArtist = match.ArtistMatched && !alreadyGotArtist;

            if (!newTitle && !newArtist)
            {
                _logger.LogDebug("Incorrect answer from {PlayerId} in session {SessionId} round {RoundNo}",
                    request.PlayerId, sessionId, round.RoundNo);
                return new SubmitAnswerResponse { Accepted = true, Message = "Incorrect answer." };
            }

            var matchCount = (newTitle ? 1 : 0) + (newArtist ? 1 : 0);
            points = pointsPerMatch * matchCount;

            try
            {
                if (existing is null)
                {
                    _dbContext.RoundCorrectAnswers.Add(new RoundCorrectAnswerEntity
                    {
                        RoundId = round.RoundId,
                        PlayerId = request.PlayerId,
                        AnsweredAt = DateTime.UtcNow,
                        Points = points,
                        GotTitle = newTitle,
                        GotArtist = newArtist
                    });
                }
                else
                {
                    if (newTitle) existing.GotTitle = true;
                    if (newArtist) existing.GotArtist = true;
                    existing.Points += points;
                }

                await _dbContext.SaveChangesAsync(ct);
                break; 
            }
            catch (DbUpdateConcurrencyException) when (attempt < 2)
            {
                _logger.LogDebug(
                    "Concurrency conflict on answer for player {PlayerId} round {RoundNo} — retry {Attempt}",
                    request.PlayerId, round.RoundNo, attempt + 1);

                if (existing is not null)
                    await _dbContext.Entry(existing).ReloadAsync(ct);

                continue;
            }
            catch (DbUpdateException)
            {
                _logger.LogDebug(
                    "Answer rejected (duplicate insert) — player {PlayerId} round {RoundNo} session {SessionId}",
                    request.PlayerId, round.RoundNo, sessionId);
                return SubmitAnswerResponse.AlreadyFullyAnswered();
            }
        }

        _logger.LogInformation(
            "Correct answer from {Nick} ({PlayerId}) in session {SessionId} round {RoundNo} — {Points} pts (title={Title}, artist={Artist})",
            player.Nick, request.PlayerId, sessionId, round.RoundNo, points, newTitle, newArtist);

        await _notifier.PlayerAnsweredCorrectly(sessionId, request.PlayerId, player.Nick, points);

        return new SubmitAnswerResponse
        {
            Accepted = true,
            IsCorrect = true,
            TitleCorrect = newTitle,
            ArtistCorrect = newArtist,
            PointsAwarded = points,
            Message = newTitle && newArtist ? "Both correct!" : newTitle ? "Title correct!" : "Artist correct!"
        };
    }

    public async Task<GetSessionStateResponse> AdvanceSessionAsync(Guid sessionId, AdvanceSessionRequest request, CancellationToken ct = default)
    {
        var session = await LoadSessionAsync(sessionId, ct);
        var lobby = await _dbContext.Lobbies.FirstAsync(x => x.LobbyId == session.LobbyId, ct);

        if (lobby.HostPlayerId != request.HostPlayerId)
            throw new ForbiddenException("Only the host can advance the session.");

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

    public async Task<ReturnToLobbyResponse> ReturnToLobbyAsync(Guid sessionId, ReturnToLobbyRequest request, CancellationToken ct = default)
    {
        var session = await LoadSessionAsync(sessionId, ct);

        if (session.EndedAt is null)
            throw new BadRequestException("Session is still in progress.");

        var lobby = await _dbContext.Lobbies
            .Include(x => x.LobbyPlayers)
            .FirstAsync(x => x.LobbyId == session.LobbyId, ct);

        if (lobby.HostPlayerId != request.HostPlayerId)
            throw new ForbiddenException("Only the host can return to lobby.");

        lobby.Status = LobbyStatus.Waiting;

        var playlist = new PlaylistEntity
        {
            PlaylistId = Guid.NewGuid(),
            OwnerPlayerId = request.HostPlayerId,
            Name = $"Lobby {lobby.Code}",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Playlists.Add(playlist);

        lobby.ActivePlaylistId = playlist.PlaylistId;

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Lobby {LobbyCode} returned to waiting (new PlaylistId={PlaylistId})", lobby.Code, playlist.PlaylistId);

        await _notifier.ReturnToLobby(sessionId, lobby.Code, playlist.PlaylistId);

        return new ReturnToLobbyResponse
        {
            LobbyId = lobby.LobbyId,
            LobbyCode = lobby.Code,
            PlaylistId = playlist.PlaylistId,
            LobbyStatus = lobby.Status.ToString()
        };
    }

    private async Task<GameSessionEntity> LoadSessionAsync(Guid sessionId, CancellationToken ct) =>
        await _dbContext.GameSessions
            .Include(x => x.Rounds).ThenInclude(x => x.CorrectAnswers)
            .FirstOrDefaultAsync(x => x.SessionId == sessionId, ct)
        ?? throw new NotFoundException("Session not found.");
}