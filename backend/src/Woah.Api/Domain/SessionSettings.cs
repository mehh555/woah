using System.Text.Json;

namespace Woah.Api.Domain;

public record SessionSettings(int RoundDurationSeconds, int RevealDurationSeconds)
{
    public static readonly SessionSettings Default = new(30, 5);

    public static SessionSettings Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Default;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var round = root.TryGetProperty("roundDurationSeconds", out var r) && r.TryGetInt32(out var rv)
            ? Math.Clamp(rv, 5, 60)
            : Default.RoundDurationSeconds;

        var reveal = root.TryGetProperty("revealDurationSeconds", out var v) && v.TryGetInt32(out var vv)
            ? Math.Clamp(vv, 3, 15)
            : Default.RevealDurationSeconds;

        return new SessionSettings(round, reveal);
    }

    public string Serialize() =>
        JsonSerializer.Serialize(new
        {
            roundDurationSeconds = RoundDurationSeconds,
            revealDurationSeconds = RevealDurationSeconds
        });
}