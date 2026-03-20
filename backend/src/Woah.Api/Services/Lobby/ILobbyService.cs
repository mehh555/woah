using Woah.Api.Contracts.Lobbies;

namespace Woah.Api.Services.Lobby;

public interface ILobbyService
{
    Task<CreateLobbyResponse> CreateLobbyAsync(CreateLobbyRequest request, CancellationToken ct = default);
    Task<JoinLobbyResponse> JoinLobbyAsync(string lobbyCode, JoinLobbyRequest request, CancellationToken ct = default);
    Task<GetLobbyResponse> GetLobbyAsync(string lobbyCode, CancellationToken ct = default);
    Task<LeaveLobbyResponse> LeaveLobbyAsync(string lobbyCode, LeaveLobbyRequest request, CancellationToken ct = default);
}