using System.Text.Json;

namespace Woah.Api.Domain;

public record SessionSettings(int RoundDurationSeconds)
{
    public static readonly SessionSettings Default = new(10);

    public static SessionSettings Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Default;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var round = root.TryGetProperty("roundDurationSeconds", out var r) && r.TryGetInt32(out var rv)
            ? Math.Clamp(rv, 5, 15)
            : Default.RoundDurationSeconds;

        return new SessionSettings(round);
    }

    public string Serialize() =>
        JsonSerializer.Serialize(new
        {
            roundDurationSeconds = RoundDurationSeconds
        });
}