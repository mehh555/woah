using Woah.Api.Domain;
using Woah.Api.Infrastructure.Persistence.Models;
using Woah.Api.Services.Playlist;

namespace Woah.Api.Services.Session;

public interface ISessionFactory
{
    GameSessionEntity Create(LobbyEntity lobby, PlaylistEntity playlist, IList<LobbyDraftTrack> tracks, SessionSettings settings);
}