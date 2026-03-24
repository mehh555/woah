using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Woah.Api.Contracts.Lobbies;
using Woah.Api.Domain;
using Woah.Api.Exceptions;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Infrastructure.Persistence.Models;
using Woah.Api.Services.Lobby;
using Woah.Api.Services.Notifications;
using Xunit;

namespace Woah.Api.Tests.Services.Lobby;

public class LobbyServiceTests : IDisposable
{
    private readonly WoahDbContext _db;
    private readonly Mock<ILobbyCodeGenerator> _codeGenerator = new();
    private readonly Mock<IGameNotifier> _notifier = new();
    private readonly Mock<TimeProvider> _timeProvider = new();
    private readonly LobbyService _service;
    private static readonly DateTime BaseTime = new(2025, 3, 24, 12, 0, 0, DateTimeKind.Utc);

    public LobbyServiceTests()
    {
        var options = new DbContextOptionsBuilder<WoahDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new WoahDbContext(options);
        _timeProvider.Setup(t => t.GetUtcNow()).Returns(new DateTimeOffset(BaseTime));

        _service = new LobbyService(
            _db,
            _codeGenerator.Object,
            _notifier.Object,
            _timeProvider.Object,
            NullLogger<LobbyService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateLobbyAsync_HappyPath_Works()
    {
        _codeGenerator.Setup(g => g.Generate(It.IsAny<int>())).Returns("LOBBY1");
        var req = new CreateLobbyRequest { HostNick = "Host", MaxPlayers = 8 };

        var res = await _service.CreateLobbyAsync(req);

        Assert.NotNull(res);
        Assert.Equal("LOBBY1", res.LobbyCode);
    }

    [Fact]
    public async Task LeaveLobbyAsync_HostLeaves_ClosesLobby()
    {
        var hostId = Guid.NewGuid();
        var lobby = new LobbyEntity 
        { 
            LobbyId = Guid.NewGuid(), 
            Code = "LEAVE1", 
            Status = LobbyStatus.Waiting, 
            HostPlayerId = hostId, 
            MaxPlayers = 8 
        };
        _db.Lobbies.Add(lobby);
        _db.LobbyPlayers.Add(new LobbyPlayerEntity { LobbyId = lobby.LobbyId, PlayerId = hostId, Nick = "Host", JoinedAt = BaseTime });
        await _db.SaveChangesAsync();

        var req = new LeaveLobbyRequest { PlayerId = hostId };
        await _service.LeaveLobbyAsync("LEAVE1", req);

        var updated = await _db.Lobbies.FirstAsync();
        Assert.Equal(LobbyStatus.Finished, updated.Status);
    }

    private async Task SeedLobbyAsync(string code)
    {
        var hostId = Guid.NewGuid();
        _db.Lobbies.Add(new LobbyEntity { LobbyId = Guid.NewGuid(), Code = code, Status = LobbyStatus.Waiting, HostPlayerId = hostId, MaxPlayers = 8 });
        _db.LobbyPlayers.Add(new LobbyPlayerEntity { LobbyId = Guid.NewGuid(), PlayerId = hostId, Nick = "Host", JoinedAt = BaseTime });
        await _db.SaveChangesAsync();
    }
}