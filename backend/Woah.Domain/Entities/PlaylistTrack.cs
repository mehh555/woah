using System;

namespace Woah.Domain.Entities;

public class PlaylistTrack
{
    public Guid PlaylistId { get; private set; }
    public int ItemNo { get; private set; }
    public string TrackJson { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string? PreviewUrl { get; private set; }
    public string? SpotifyTrackId { get; private set; }
    public string? SpotifyUrl { get; private set; }
    public bool IsValid { get; private set; }
    public string? InvalidReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Playlist Playlist { get; private set; } = null!;

    private PlaylistTrack() { }

    public PlaylistTrack(Guid playlistId, int itemNo, string trackJson, string title, string? spotifyTrackId)
    {
        if (playlistId == Guid.Empty) throw new ArgumentException("PlaylistId is required.", nameof(playlistId));
        if (itemNo <= 0) throw new ArgumentOutOfRangeException(nameof(itemNo), "ItemNo must be positive.");
        if (string.IsNullOrWhiteSpace(trackJson)) throw new ArgumentException("TrackJson is required.", nameof(trackJson));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));

        PlaylistId = playlistId;
        ItemNo = itemNo;
        TrackJson = trackJson;
        Title = title;
        SpotifyTrackId = string.IsNullOrWhiteSpace(spotifyTrackId) ? null : spotifyTrackId;
        IsValid = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsInvalid(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required.", nameof(reason));

        IsValid = false;
        InvalidReason = reason;
    }
}