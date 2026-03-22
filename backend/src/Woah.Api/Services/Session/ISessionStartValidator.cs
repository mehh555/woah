using Woah.Api.Contracts.Sessions;
using Woah.Api.Infrastructure.Persistence.Models;

namespace Woah.Api.Services.Session;

public interface ISessionStartValidator
{
    void Validate(LobbyEntity lobby, StartSessionRequest request);
}