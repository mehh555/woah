using Microsoft.EntityFrameworkCore;
using Woah.Api.Contracts;
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
    private readonly ISessionFactory _sessionFactory;
    private readonly ISessionStartValidator _startValidator;
    private readonly ISessionProgressEngine _progressEngine;
    private readonly ISessionStateBuilder _stateBuilder;
    private readonly IAnswerSubmissionHandler _answerHandler;
    private readonly IGameNotifier _notifier;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SessionService> _logger;

    public SessionService(
        WoahDbContext dbContext,
        ISessionFactory sessionFactory,
        ISessionStartValidator startValidator,
        ISessionProgressEngine progressEngine,
        ISessionStateBuilder stateBuilder,
        IAnswerSubmissionHandler answerHandler,
        IGameNotifier notifier,
        TimeProvider timeProvider,
        ILogger<SessionService> logger)
    {
        _dbContext = dbContext;
        _sessionFactory = sessionFactory;
        _startValidator = startValidator;
        _progressEngine = progressEngine;
        _stateBuilder = stateBuilder;
        _answerHandler = answerHandler;
        _notifier = notifier;
        _timeProvider = timeProvider;
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

        var settings = new SessionSettings(Math.Clamp(request.RoundDurationSeconds, 5, 25));

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
                LobbyStatus = lobby.Status.ToContract(),
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

    public Task<SubmitAnswerResponse> SubmitAnswerAsync(Guid sessionId, SubmitAnswerRequest request, CancellationToken ct = default)
        => _answerHandler.HandleAsync(sessionId, request, ct);

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
            playing.RevealedAt = _timeProvider.GetUtcNow().UtcDateTime;
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
            .FirstAsync(x => x.LobbyId == session.LobbyId, ct);

        if (lobby.HostPlayerId != request.HostPlayerId)
            throw new ForbiddenException("Only the host can return to lobby.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        lobby.Status = LobbyStatus.Waiting;

        var playlist = new PlaylistEntity
        {
            PlaylistId = Guid.NewGuid(),
            OwnerPlayerId = request.HostPlayerId,
            Name = $"Lobby {lobby.Code}",
            CreatedAt = now
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
            LobbyStatus = lobby.Status.ToContract()
        };
    }

    private Task<GameSessionEntity> LoadSessionAsync(Guid sessionId, CancellationToken ct) =>
        _dbContext.GameSessions.GetSessionWithRoundsAsync(sessionId, ct);
}