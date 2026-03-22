using Woah.Api.Contracts.Sessions;

namespace Woah.Api.Services.Session;

public interface IAnswerSubmissionHandler
{
    Task<SubmitAnswerResponse> HandleAsync(Guid sessionId, SubmitAnswerRequest request, CancellationToken ct = default);
}