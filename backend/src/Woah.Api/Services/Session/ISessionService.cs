using Woah.Api.Contracts.Sessions;

namespace Woah.Api.Services.Session;

public interface ISessionService
{
    Task<StartSessionResponse> StartSessionAsync(string lobbyCode, StartSessionRequest request, CancellationToken ct = default);
    Task<GetSessionStateResponse> GetSessionStateAsync(Guid sessionId, CancellationToken ct = default);
    Task<SubmitAnswerResponse> SubmitAnswerAsync(Guid sessionId, SubmitAnswerRequest request, CancellationToken ct = default);
    Task<GetSessionStateResponse> AdvanceSessionAsync(Guid sessionId, AdvanceSessionRequest request, CancellationToken ct = default);
    Task<ReturnToLobbyResponse> ReturnToLobbyAsync(Guid sessionId, ReturnToLobbyRequest request, CancellationToken ct = default);
}