using Woah.Api.Contracts.Sessions;
using Woah.Api.Infrastructure.Persistence.Models;

namespace Woah.Api.Services.Session;

public interface ISessionStateBuilder
{
    Task<GetSessionStateResponse> BuildAsync(GameSessionEntity session, CancellationToken ct);
}