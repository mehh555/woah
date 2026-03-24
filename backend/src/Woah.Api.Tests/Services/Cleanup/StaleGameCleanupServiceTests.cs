using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Woah.Api.Domain;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Infrastructure.Persistence.Models;
using Woah.Api.Services.Cleanup;
using Xunit;

namespace Woah.Api.Tests.Services.Cleanup;

public class StaleGameCleanupServiceTests
{
    private readonly WoahDbContext _db;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Mock<TimeProvider> _timeProvider = new();
    private static readonly DateTime BaseTime = new(2025, 3, 24, 12, 0, 0, DateTimeKind.Utc);

    public StaleGameCleanupServiceTests()
    {
        var options = new DbContextOptionsBuilder<WoahDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new WoahDbContext(options);

        var services = new ServiceCollection();
        services.AddSingleton(_db);
        var provider = services.BuildServiceProvider();

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(provider);
        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(x => x.CreateScope()).Returns(scope.Object);
        _scopeFactory = factory.Object;

        _timeProvider.Setup(t => t.GetUtcNow()).Returns(new DateTimeOffset(BaseTime));
    }

    [Fact]
    public async Task Cleanup_StaleLobby_Works()
    {
        var lobby = new LobbyEntity 
        { 
            LobbyId = Guid.NewGuid(), Code = "OLD", Status = LobbyStatus.Waiting, 
            CreatedAt = BaseTime.AddMinutes(-60), HostPlayerId = Guid.NewGuid(), MaxPlayers = 8 
        };
        _db.Lobbies.Add(lobby);
        _db.LobbyPlayers.Add(new LobbyPlayerEntity { LobbyId = lobby.LobbyId, PlayerId = Guid.NewGuid(), Nick = "Player", JoinedAt = BaseTime });
        await _db.SaveChangesAsync();

        var service = new TestCleanupService(_scopeFactory, _timeProvider.Object);
        await service.RunCleanup(CancellationToken.None);

        var updated = await _db.Lobbies.FirstAsync();
        Assert.Equal(LobbyStatus.Finished, updated.Status);
    }

    private class TestCleanupService : StaleGameCleanupService
    {
        public TestCleanupService(IServiceScopeFactory f, TimeProvider t) : base(f, t, NullLogger<StaleGameCleanupService>.Instance) { }
        public Task RunCleanup(CancellationToken ct) => CleanupAsync(ct);
    }
}