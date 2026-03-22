using Woah.Api.Contracts.Playlists;
using Woah.Api.Infrastructure.Persistence.Models;
using Woah.Api.Integrations.Itunes;

namespace Woah.Api.Services.Playlist;

internal static class LobbyTrackMapper
{
    public static PlaylistTrackEntity ToEntity(ItunesTrackDto track, Guid playlistId) =>
        new()
        {
            PlaylistTrackId = Guid.NewGuid(),
            PlaylistId = playlistId,
            ItunesTrackId = track.TrackId,
            Title = track.TrackName!,
            Artist = track.ArtistName!,
            PreviewUrl = track.PreviewUrl!,
            ArtworkUrl = track.ArtworkUrl100,
            DurationMs = track.TrackTimeMillis,
            AddedAt = DateTime.UtcNow
        };

    public static ItunesTrackSearchResultResponse ToSearchResult(ItunesTrackDto track) =>
        new()
        {
            TrackId = track.TrackId,
            Title = track.TrackName!,
            Artist = track.ArtistName!,
            PreviewUrl = track.PreviewUrl!,
            ArtworkUrl = track.ArtworkUrl100,
            DurationMs = track.TrackTimeMillis,
            CollectionName = track.CollectionName
        };

    public static LobbyPlaylistTrackResponse ToResponse(PlaylistTrackEntity track) =>
        new()
        {
            TrackId = track.ItunesTrackId,
            Title = track.Title,
            Artist = track.Artist,
            PreviewUrl = track.PreviewUrl,
            ArtworkUrl = track.ArtworkUrl,
            DurationMs = track.DurationMs,
            AddedAt = track.AddedAt
        };
}