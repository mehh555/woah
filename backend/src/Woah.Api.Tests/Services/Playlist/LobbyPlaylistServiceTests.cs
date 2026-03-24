using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options; // Dodane dla IOptions
using Microsoft.Extensions.Logging; // Dodane dla ILogger
using Moq;
using Woah.Api.Contracts.Playlists;
using Woah.Api.Domain;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Infrastructure.Persistence.Models;
using Woah.Api.Integrations.Itunes;
using Woah.Api.Services.Notifications;
using Woah.Api.Services.Playlist;
using Xunit;

namespace Woah.Api.Tests.Services.Playlist;

public class LobbyPlaylistServiceTests : IDisposable
{
    private readonly WoahDbContext _db;
    private readonly Mock<ItunesApiClient> _itunes;
    private readonly LobbyPlaylistService _service;
    private static readonly DateTime BaseTime = new(2025, 3, 24, 12, 0, 0, DateTimeKind.Utc);

    public LobbyPlaylistServiceTests()
    {
        var options = new DbContextOptionsBuilder<WoahDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new WoahDbContext(options);

        // Tworzymy puste zależności, aby zadowolić konstruktor ItunesApiClient
        var mockHttpClient = new HttpClient();
        var mockOptions = new Mock<IOptions<ItunesSettings>>();
        mockOptions.Setup(o => o.Value).Returns(new ItunesSettings()); // Zakładam, że taka klasa istnieje
        var mockLogger = NullLogger<ItunesApiClient>.Instance;

        // Przekazujemy wszystkie 3 argumenty do Mocka
        _itunes = new Mock<ItunesApiClient>(mockHttpClient, mockOptions.Object, mockLogger);
        
        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(t => t.GetUtcNow()).Returns(new DateTimeOffset(BaseTime));

        _service = new LobbyPlaylistService(
            _db, 
            _itunes.Object, 
            Mock.Of<IGameNotifier>(), 
            timeProvider.Object, 
            NullLogger<LobbyPlaylistService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetLobbyPlaylistAsync_ReturnsTracks()
    {
        // Arrange
        var lobbyId = Guid.NewGuid();
        var playlistId = Guid.NewGuid();
        
        _db.Lobbies.Add(new LobbyEntity 
        { 
            LobbyId = lobbyId, 
            Code = "PLAY1", 
            ActivePlaylistId = playlistId, 
            HostPlayerId = Guid.NewGuid(), 
            MaxPlayers = 8,
            Status = LobbyStatus.Waiting
        });

        _db.PlaylistTracks.Add(new PlaylistTrackEntity 
        { 
            PlaylistId = playlistId, 
            ItunesTrackId = 1, 
            Title = "Title", 
            Artist = "Artist", 
            PreviewUrl = "url", 
            AddedByPlayerId = Guid.NewGuid(), 
            AddedAt = BaseTime 
        });
        
        await _db.SaveChangesAsync();

        // Act
        var res = await _service.GetLobbyPlaylistAsync("PLAY1");

        // Assert
        Assert.Single(res.Tracks);
        Assert.Equal("Title", res.Tracks[0].Title);
    }
}