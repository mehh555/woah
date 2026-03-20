using System.Threading;
using System.Threading.Tasks;
using Woah.Api.Contracts.Sessions;

namespace Woah.Api.Services;

public interface ISessionService
{
    Task<StartSessionResponse> StartSessionAsync(
        string lobbyCode,
        StartSessionRequest request,
        CancellationToken cancellationToken = default);
}