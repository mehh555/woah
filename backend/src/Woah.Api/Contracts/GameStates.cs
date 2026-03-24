using Woah.Api.Domain;

namespace Woah.Api.Contracts;

public static class GameStates
{
    public static class Round
    {
        public const string Pending = "Pending";
        public const string Playing = "Playing";
        public const string Revealed = "Revealed";
        public const string Finished = "Finished";
    }

    public static class Lobby
    {
        public const string Waiting = "Waiting";
        public const string InGame = "InGame";
        public const string Finished = "Finished";
    }

    public static string ToContract(this LobbyStatus status) => status switch
    {
        LobbyStatus.Waiting => Lobby.Waiting,
        LobbyStatus.InGame => Lobby.InGame,
        LobbyStatus.Finished => Lobby.Finished,
        _ => status.ToString()
    };

    public static string ToContract(this RoundState state) => state switch
    {
        RoundState.Pending => Round.Pending,
        RoundState.Playing => Round.Playing,
        RoundState.Revealed => Round.Revealed,
        RoundState.Finished => Round.Finished,
        _ => state.ToString()
    };
}
