using Microsoft.EntityFrameworkCore;
using Woah.Api.Domain;
using Woah.Api.Infrastructure.Persistence;

namespace Woah.Api.Services.Cleanup;

public class StaleGameCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StaleLobbyThreshold = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan StaleSessionThreshold = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StaleGameCleanupService> _logger;

    public StaleGameCleanupService(IServiceScopeFactory scopeFactory, ILogger<StaleGameCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StaleGameCleanupService started (interval={Interval}, lobbyThreshold={LobbyThreshold}, sessionThreshold={SessionThreshold})",
            Interval, StaleLobbyThreshold, StaleSessionThreshold);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, stoppingToken);
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

    private async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WoahDbContext>();

        var closedLobbies = await CloseStaleLobbiesAsync(db, ct);
        var closedSessions = await CloseStaleSessionsAsync(db, ct);

        if (closedLobbies > 0 || closedSessions > 0)
            _logger.LogInformation("Cleanup completed: {Lobbies} lobbies closed, {Sessions} sessions closed", closedLobbies, closedSessions);
    }

    private async Task<int> CloseStaleLobbiesAsync(WoahDbContext db, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow - StaleLobbyThreshold;

        var staleLobbies = await db.Lobbies
            .Include(l => l.LobbyPlayers)
            .Where(l => l.Status == LobbyStatus.Waiting && l.CreatedAt < cutoff)
            .ToListAsync(ct);

        foreach (var lobby in staleLobbies)
        {
            lobby.Status = LobbyStatus.Finished;

            foreach (var member in lobby.LobbyPlayers.Where(m => m.LeftAt == null))
                member.LeftAt = DateTime.UtcNow;

            _logger.LogInformation("Closed stale lobby {LobbyCode} (created {CreatedAt})", lobby.Code, lobby.CreatedAt);
        }

        if (staleLobbies.Count > 0)
            await db.SaveChangesAsync(ct);

        return staleLobbies.Count;
    }

    private async Task<int> CloseStaleSessionsAsync(WoahDbContext db, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow - StaleSessionThreshold;

        var activeSessions = await db.GameSessions
            .Include(s => s.Rounds)
            .Where(s => s.EndedAt == null)
            .ToListAsync(ct);

        var closed = 0;

        foreach (var session in activeSessions)
        {
            var lastRoundEnd = session.Rounds
                .Where(r => r.EndsAt != null)
                .Max(r => (DateTime?)r.EndsAt);

            if (lastRoundEnd is null || lastRoundEnd.Value >= cutoff)
                continue;

            session.EndedAt = DateTime.UtcNow;

            foreach (var round in session.Rounds.Where(r => r.State == RoundState.Playing || r.State == RoundState.Revealed))
                round.State = RoundState.Finished;

            var lobby = await db.Lobbies.FirstOrDefaultAsync(l => l.LobbyId == session.LobbyId, ct);
            if (lobby is not null && lobby.Status == LobbyStatus.InGame)
                lobby.Status = LobbyStatus.Finished;

            _logger.LogInformation("Closed stale session {SessionId} (last round ended {LastRoundEnd})", session.SessionId, lastRoundEnd);
            closed++;
        }

        if (closed > 0)
            await db.SaveChangesAsync(ct);

        return closed;
    }
}