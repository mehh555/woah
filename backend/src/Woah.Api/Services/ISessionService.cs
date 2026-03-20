using Woah.Api.Contracts.Sessions;

namespace Woah.Api.Services;

public interface ISessionService
{
    Task<StartSessionResponse> StartSessionAsync(
        string lobbyCode,
        StartSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<GetSessionStateResponse> GetSessionStateAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<SubmitAnswerResponse> SubmitAnswerAsync(
        Guid sessionId,
        SubmitAnswerRequest request,
        CancellationToken cancellationToken = default);

    Task<GetSessionStateResponse> AdvanceSessionAsync(
        Guid sessionId,
        AdvanceSessionRequest request,
        CancellationToken cancellationToken = default);
}