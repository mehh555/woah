using System.Collections.Concurrent;
using Woah.Api.Domain;
using Woah.Api.Services;

namespace Woah.Api.Infrastructure.InMemory;

public class InMemoryLobbyPlaylistStore : ILobbyPlaylistStore
{
    public const int MaxTracks = 20;

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<long, LobbyDraftTrack>> _tracksByLobbyCode =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<LobbyDraftTrack> GetTracks(string lobbyCode)
    {
        if (!_tracksByLobbyCode.TryGetValue(lobbyCode.Normalize(), out var tracks))
            return Array.Empty<LobbyDraftTrack>();

        return tracks.Values
            .OrderBy(x => x.AddedAt)
            .ToList();
    }

    public bool TryAddTrack(string lobbyCode, LobbyDraftTrack track)
    {
        var key = lobbyCode.Normalize();

        var tracks = _tracksByLobbyCode.GetOrAdd(
            key,
            _ => new ConcurrentDictionary<long, LobbyDraftTrack>());

        if (tracks.Count >= MaxTracks)
            return false;

        return tracks.TryAdd(track.TrackId, track);
    }

    public bool RemoveTrack(string lobbyCode, long trackId)
    {
        if (!_tracksByLobbyCode.TryGetValue(lobbyCode.Normalize(), out var tracks))
            return false;

        return tracks.TryRemove(trackId, out _);
    }

    public void Clear(string lobbyCode)
        => _tracksByLobbyCode.TryRemove(lobbyCode.Normalize(), out _);
}