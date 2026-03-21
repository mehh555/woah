using System.Collections.Concurrent;
using Woah.Api.Services.Playlist;

namespace Woah.Api.Infrastructure.InMemory;

public class InMemoryLobbyPlaylistStore : ILobbyPlaylistStore
{
    public const int MaxTracks = 20;

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<long, LobbyDraftTrack>> _store =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<LobbyDraftTrack> GetTracks(string lobbyCode)
    {
        if (!_store.TryGetValue(lobbyCode, out var tracks))
            return Array.Empty<LobbyDraftTrack>();

        return tracks.Values.OrderBy(x => x.AddedAt).ToList();
    }

    public bool TryAddTrack(string lobbyCode, LobbyDraftTrack track)
    {
        var bucket = _store.GetOrAdd(lobbyCode, _ => new ConcurrentDictionary<long, LobbyDraftTrack>());

        if (!bucket.TryAdd(track.TrackId, track))
            return false;

        if (bucket.Count > MaxTracks)
        {
            bucket.TryRemove(track.TrackId, out _);
            return false;
        }

        return true;
    }

    public bool RemoveTrack(string lobbyCode, long trackId)
    {
        if (!_store.TryGetValue(lobbyCode, out var tracks))
            return false;

        return tracks.TryRemove(trackId, out _);
    }

    public void Clear(string lobbyCode)
        => _store.TryRemove(lobbyCode, out _);
}