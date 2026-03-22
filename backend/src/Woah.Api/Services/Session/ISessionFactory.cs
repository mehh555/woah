using Woah.Api.Domain;
using Woah.Api.Infrastructure.Persistence.Models;

namespace Woah.Api.Services.Session;

public interface ISessionFactory
{
    GameSessionEntity Create(LobbyEntity lobby, PlaylistEntity playlist, IList<PlaylistTrackEntity> tracks, SessionSettings settings);
}