using System.Threading;
using System.Threading.Tasks;
using Woah.Api.Contracts.Lobbies;

namespace Woah.Api.Services;

public interface ILobbyService
{
    Task<CreateLobbyResponse> CreateLobbyAsync(
        CreateLobbyRequest request,
        CancellationToken cancellationToken = default);
}