using Woah.Api.Domain;
using Woah.Api.Infrastructure.Persistence.Models;

namespace Woah.Api.Services.Session;

public class SessionFactory : ISessionFactory
{
    private readonly IAnswerNormalizer _normalizer;

    public SessionFactory(IAnswerNormalizer normalizer)
    {
        _normalizer = normalizer;
    }

    public GameSessionEntity Create(
        LobbyEntity lobby,
        PlaylistEntity playlist,
        IList<PlaylistTrackEntity> tracks,
        SessionSettings settings)
    {
        Shuffle(tracks);

        var now = DateTime.UtcNow;
        var session = new GameSessionEntity
        {
            SessionId = Guid.NewGuid(),
            LobbyId = lobby.LobbyId,
            PlaylistId = playlist.PlaylistId,
            StartedAt = now,
            EndedAt = null,
            SettingsJson = settings.Serialize(),
            Rounds = new List<RoundEntity>()
        };

        for (var i = 0; i < tracks.Count; i++)
        {
            var isFirst = i == 0;
            session.Rounds.Add(new RoundEntity
            {
                RoundId = Guid.NewGuid(),
                SessionId = session.SessionId,
                RoundNo = i + 1,
                PlaylistId = playlist.PlaylistId,
                PlaylistItemNumber = i + 1,
                PreviewUrl = tracks[i].PreviewUrl,
                AnswerTitle = tracks[i].Title,
                AnswerArtist = tracks[i].Artist,
                AnswerNorm = _normalizer.Normalize(tracks[i].Title),
                AnswerArtistNorm = _normalizer.Normalize(tracks[i].Artist),
                ArtworkUrl = tracks[i].ArtworkUrl,
                ItunesTrackId = tracks[i].ItunesTrackId,
                StartedAt = now,
                EndsAt = isFirst ? now.AddSeconds(settings.RoundDurationSeconds) : null,
                RevealedAt = null,
                State = isFirst ? RoundState.Playing : RoundState.Pending,
                CorrectAnswers = new List<RoundCorrectAnswerEntity>()
            });
        }

        return session;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}