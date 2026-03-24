using Microsoft.EntityFrameworkCore;
using Woah.Api.Domain;
using Woah.Api.Infrastructure.Persistence;

namespace Woah.Api.Services.Cleanup;

public class StaleGameCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<StaleGameCleanupService> _logger;

    public StaleGameCleanupService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<StaleGameCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "StaleGameCleanupService started (interval={GameConstants.CleanupInterval}, lobbyThreshold={LobbyThreshold}, sessionThreshold={SessionThreshold})",
            GameConstants.CleanupInterval, GameConstants.StaleLobbyThreshold, GameConstants.StaleSessionThreshold);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(GameConstants.CleanupInterval, stoppingToken);
                await CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during stale game cleanup");
            }
        }
    }

    protected async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WoahDbContext>();

        var closedLobbies = await CloseStaleLobbiesAsync(db, ct);
        var closedSessions = await CloseStaleSessionsAsync(db, ct);

        if (closedLobbies > 0 || closedSessions > 0)
            _logger.LogInformation("Cleanup completed: {Lobbies} lobbies closed, {Sessions} sessions closed",
                closedLobbies, closedSessions);
    }

    private async Task<int> CloseStaleLobbiesAsync(WoahDbContext db, CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var cutoff = now - GameConstants.StaleLobbyThreshold;

        var staleLobbies = await db.Lobbies
            .Where(l => l.Status == LobbyStatus.Waiting && l.CreatedAt < cutoff)
            .Select(l => new
            {
                Lobby = l,
                ActiveMembers = l.LobbyPlayers.Where(m => m.LeftAt == null).ToList()
            })
            .ToListAsync(ct);

        foreach (var entry in staleLobbies)
        {
            entry.Lobby.Status = LobbyStatus.Finished;

            foreach (var member in entry.ActiveMembers)
                member.LeftAt = now;

            _logger.LogInformation("Closed stale lobby {LobbyCode} (created {CreatedAt})", entry.Lobby.Code, entry.Lobby.CreatedAt);
        }

        if (staleLobbies.Count > 0)
            await db.SaveChangesAsync(ct);

        return staleLobbies.Count;
    }

    private async Task<int> CloseStaleSessionsAsync(WoahDbContext db, CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var cutoff = now - GameConstants.StaleSessionThreshold;

        var activeSessions = await db.GameSessions
            .Where(s => s.EndedAt == null)
            .Select(s => new
            {
                Session = s,
                LastRoundEnd = s.Rounds
                    .Where(r => r.EndsAt != null)
                    .Max(r => (DateTime?)r.EndsAt),
                ActiveRounds = s.Rounds
                    .Where(r => r.State == RoundState.Playing || r.State == RoundState.Revealed)
                    .ToList()
            })
            .ToListAsync(ct);

        var closed = 0;

        foreach (var entry in activeSessions)
        {
            if (entry.LastRoundEnd is null || entry.LastRoundEnd.Value >= cutoff)
                continue;

            entry.Session.EndedAt = now;

            foreach (var round in entry.ActiveRounds)
                round.State = RoundState.Finished;

            var lobby = await db.Lobbies.FirstOrDefaultAsync(l => l.LobbyId == entry.Session.LobbyId, ct);
            if (lobby is not null && lobby.Status == LobbyStatus.InGame)
                lobby.Status = LobbyStatus.Finished;

            _logger.LogInformation("Closed stale session {SessionId} (last round ended {LastRoundEnd})",
                entry.Session.SessionId, entry.LastRoundEnd);
            closed++;
        }

        if (closed > 0)
            await db.SaveChangesAsync(ct);

        return closed;
    }
}