using Woah.Api.Contracts.Sessions;
using Woah.Api.Domain;
using Woah.Api.Exceptions;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Infrastructure.Persistence.Models;

namespace Woah.Api.Services.Session;

public class SessionStartValidator : ISessionStartValidator
{
    private readonly ILogger<SessionStartValidator> _logger;

    public SessionStartValidator(ILogger<SessionStartValidator> logger)
    {
        _logger = logger;
    }

    public void Validate(LobbyEntity lobby, StartSessionRequest request)
    {
        if (lobby.Status != LobbyStatus.Waiting)
        {
            _logger.LogWarning("Start rejected — lobby {LobbyCode} is not waiting (Status={Status})", lobby.Code, lobby.Status);
            throw new BadRequestException("Only waiting lobbies can start a session.");
        }

        if (lobby.HostPlayerId != request.HostPlayerId)
        {
            _logger.LogWarning("Start rejected — player {PlayerId} is not host of lobby {LobbyCode}", request.HostPlayerId, lobby.Code);
            throw new ForbiddenException("Only the host can start the session.");
        }

        if (!lobby.ActivePlayers().Any(x => x.PlayerId == request.HostPlayerId))
        {
            _logger.LogWarning("Start rejected — host {PlayerId} is not active in lobby {LobbyCode}", request.HostPlayerId, lobby.Code);
            throw new BadRequestException("Host is not active in this lobby.");
        }
    }
}