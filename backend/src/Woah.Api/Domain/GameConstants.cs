namespace Woah.Api.Domain;

public static class GameConstants
{
    public const int MaxTracksPerPlayer = 10;
    public const int TrackOwnerBonusPoints = 75;
    public const int MaxConcurrencyRetries = 2;
    public const int LobbyCodeLength = 6;
    public const int ItunesSearchLimit = 12;
    public static readonly TimeSpan StaleLobbyThreshold = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan StaleSessionThreshold = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);
}
