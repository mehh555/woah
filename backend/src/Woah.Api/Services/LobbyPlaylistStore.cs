using System.Collections.Concurrent;

namespace Woah.Api.Services;

public class InMemoryLobbyPlaylistStore : ILobbyPlaylistStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<long, LobbyDraftTrack>> _tracksByLobbyCode =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<LobbyDraftTrack> GetTracks(string lobbyCode)
    {
        var normalizedLobbyCode = NormalizeLobbyCode(lobbyCode);

        if (!_tracksByLobbyCode.TryGetValue(normalizedLobbyCode, out var tracks))
        {
            return Array.Empty<LobbyDraftTrack>();
        }

        return tracks.Values
            .OrderBy(x => x.AddedAt)
            .ToList();
    }

    public bool TryAddTrack(string lobbyCode, LobbyDraftTrack track)
    {
        var normalizedLobbyCode = NormalizeLobbyCode(lobbyCode);

        var tracks = _tracksByLobbyCode.GetOrAdd(
            normalizedLobbyCode,
            _ => new ConcurrentDictionary<long, LobbyDraftTrack>());

        return tracks.TryAdd(track.TrackId, track);
    }

    public bool RemoveTrack(string lobbyCode, long trackId)
    {
        var normalizedLobbyCode = NormalizeLobbyCode(lobbyCode);

        if (!_tracksByLobbyCode.TryGetValue(normalizedLobbyCode, out var tracks))
        {
            return false;
        }

        return tracks.TryRemove(trackId, out _);
    }

    public void Clear(string lobbyCode)
    {
        var normalizedLobbyCode = NormalizeLobbyCode(lobbyCode);
        _tracksByLobbyCode.TryRemove(normalizedLobbyCode, out _);
    }

    private static string NormalizeLobbyCode(string lobbyCode)
    {
        return lobbyCode.Trim().ToUpperInvariant();
    }
}