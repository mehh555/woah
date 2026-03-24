using Microsoft.EntityFrameworkCore;
using Woah.Api.Contracts.Sessions;
using Woah.Api.Domain;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Infrastructure.Persistence.Models;
using Woah.Api.Services.Notifications;

namespace Woah.Api.Services.Session;

public class AnswerSubmissionHandler : IAnswerSubmissionHandler
{
	private readonly WoahDbContext _dbContext;
	private readonly IAnswerNormalizer _normalizer;
	private readonly IAnswerEvaluator _answerEvaluator;
	private readonly IScoreCalculator _scoreCalculator;
	private readonly ISessionProgressEngine _progressEngine;
	private readonly IGameNotifier _notifier;
	private readonly TimeProvider _timeProvider;
	private readonly ILogger<AnswerSubmissionHandler> _logger;

<<<<<<< Updated upstream
=======
	private const int MaxConcurrencyRetries = 2;
	private const int TrackOwnerBonusPoints = 75;
>>>>>>> Stashed changes

	public AnswerSubmissionHandler(
		WoahDbContext dbContext,
		IAnswerNormalizer normalizer,
		IAnswerEvaluator answerEvaluator,
		IScoreCalculator scoreCalculator,
		ISessionProgressEngine progressEngine,
		IGameNotifier notifier,
		TimeProvider timeProvider,
		ILogger<AnswerSubmissionHandler> logger)
	{
		_dbContext = dbContext;
		_normalizer = normalizer;
		_answerEvaluator = answerEvaluator;
		_scoreCalculator = scoreCalculator;
		_progressEngine = progressEngine;
		_notifier = notifier;
		_timeProvider = timeProvider;
		_logger = logger;
	}

	public async Task<SubmitAnswerResponse> HandleAsync(Guid sessionId, SubmitAnswerRequest request, CancellationToken ct = default)
	{
		var session = await LoadSessionAsync(sessionId, ct);
		await _progressEngine.EnsurePlayingToRevealedAsync(session, ct);

		if (session.EndedAt is not null)
			return SubmitAnswerResponse.Rejected("Session has already finished.");

		var lobby = await _dbContext.Lobbies
			.Include(x => x.LobbyPlayers)
			.FirstAsync(x => x.LobbyId == session.LobbyId, ct);

		var activePlayers = lobby.ActivePlayers();

		if (!activePlayers.Any(x => x.PlayerId == request.PlayerId))
			return SubmitAnswerResponse.Rejected("Player is not active in this lobby.");

		var round = SessionProgressEngine.OrderedRounds(session)
			.FirstOrDefault(x => x.State == RoundState.Playing);

		if (round is null)
			return SubmitAnswerResponse.Rejected("There is no active round to answer.");

		var trackOwnerId = round.ItunesTrackId is not null
			? await _dbContext.PlaylistTracks
				.Where(pt => pt.PlaylistId == round.PlaylistId && pt.ItunesTrackId == round.ItunesTrackId)
				.Select(pt => (Guid?)pt.AddedByPlayerId)
				.FirstOrDefaultAsync(ct)
			: null;

		if (trackOwnerId == request.PlayerId)
			return SubmitAnswerResponse.Rejected("You cannot guess your own track.");

		var now = _timeProvider.GetUtcNow().UtcDateTime;

		if (round.EndsAt is not null && now >= round.EndsAt.Value)
		{
			await _progressEngine.EnsurePlayingToRevealedAsync(session, ct);
			return SubmitAnswerResponse.Rejected("Round has already ended.");
		}

		var normalizedGuess = _normalizer.Normalize(request.Answer);
		var match = _answerEvaluator.Evaluate(normalizedGuess, round.AnswerNorm, round.AnswerArtistNorm);

		var settings = SessionSettings.Parse(session.SettingsJson);
		var elapsed = Math.Max((now - round.StartedAt).TotalSeconds, 0);
		var basePoints = _scoreCalculator.Calculate(settings.RoundDurationSeconds, elapsed);
		var player = activePlayers.First(x => x.PlayerId == request.PlayerId);

		bool newTitle, newArtist;
		int points;

		for (var attempt = 0; ; attempt++)
		{
			var existing = round.CorrectAnswers.FirstOrDefault(x => x.PlayerId == request.PlayerId);
			var alreadyGotTitle = existing?.GotTitle ?? false;
			var alreadyGotArtist = existing?.GotArtist ?? false;

			if (alreadyGotTitle && alreadyGotArtist)
				return SubmitAnswerResponse.AlreadyFullyAnswered();

			newTitle = match.TitleMatched && !alreadyGotTitle;
			newArtist = match.ArtistMatched && !alreadyGotArtist;

			if (!newTitle && !newArtist)
			{
				_logger.LogDebug("Incorrect answer from {PlayerId} in session {SessionId} round {RoundNo}",
					request.PlayerId, sessionId, round.RoundNo);
				return new SubmitAnswerResponse { Accepted = true, Message = "Incorrect answer." };
			}

			var titlePoints = newTitle ? basePoints : 0;
			var artistPoints = newArtist ? Math.Max(basePoints / 2, 1) : 0;
			points = titlePoints + artistPoints;

			try
			{
				if (existing is null)
				{
					_dbContext.RoundCorrectAnswers.Add(new RoundCorrectAnswerEntity
					{
						RoundId = round.RoundId,
						PlayerId = request.PlayerId,
						AnsweredAt = now,
						Points = points,
						GotTitle = newTitle,
						GotArtist = newArtist
					});
				}
				else
				{
					if (newTitle) existing.GotTitle = true;
					if (newArtist) existing.GotArtist = true;
					existing.Points += points;
				}

				await _dbContext.SaveChangesAsync(ct);
				break;
			}
			catch (DbUpdateConcurrencyException) when (attempt < GameConstants.MaxConcurrencyRetries)
			{
				_logger.LogDebug(
					"Concurrency conflict on answer for player {PlayerId} round {RoundNo} — retry {Attempt}",
					request.PlayerId, round.RoundNo, attempt + 1);

				if (existing is not null)
					await _dbContext.Entry(existing).ReloadAsync(ct);

				continue;
			}
			catch (DbUpdateException)
			{
				_logger.LogDebug(
					"Answer rejected (duplicate insert) — player {PlayerId} round {RoundNo} session {SessionId}",
					request.PlayerId, round.RoundNo, sessionId);
				return SubmitAnswerResponse.AlreadyFullyAnswered();
			}
		}

<<<<<<< Updated upstream
=======
		// Award track owner bonus (once) when the first other player guesses the title correctly
>>>>>>> Stashed changes
		if (newTitle && trackOwnerId.HasValue)
		{
			var ownerAlreadyRewarded = round.CorrectAnswers.Any(a => a.PlayerId == trackOwnerId.Value);
			if (!ownerAlreadyRewarded)
			{
<<<<<<< Updated upstream
				try
				{
					_dbContext.RoundCorrectAnswers.Add(new RoundCorrectAnswerEntity
					{
						RoundId = round.RoundId,
						PlayerId = trackOwnerId.Value,
						AnsweredAt = now,
						Points = GameConstants.TrackOwnerBonusPoints,
						GotTitle = false,
						GotArtist = false
					});
					await _dbContext.SaveChangesAsync(ct);

					_logger.LogInformation(
						"Track owner bonus {Bonus} pts awarded to {OwnerId} in session {SessionId} round {RoundNo}",
						GameConstants.TrackOwnerBonusPoints, trackOwnerId.Value, sessionId, round.RoundNo);
				}
				catch (DbUpdateException)
				{
					_logger.LogDebug(
						"Track owner bonus already awarded (concurrent insert) for round {RoundNo} session {SessionId}",
						round.RoundNo, sessionId);
				}
=======
				_dbContext.RoundCorrectAnswers.Add(new RoundCorrectAnswerEntity
				{
					RoundId = round.RoundId,
					PlayerId = trackOwnerId.Value,
					AnsweredAt = now,
					Points = TrackOwnerBonusPoints,
					GotTitle = false,
					GotArtist = false
				});
				await _dbContext.SaveChangesAsync(ct);

				_logger.LogInformation(
					"Track owner bonus {Bonus} pts awarded to {OwnerId} in session {SessionId} round {RoundNo}",
					TrackOwnerBonusPoints, trackOwnerId.Value, sessionId, round.RoundNo);
>>>>>>> Stashed changes
			}
		}

		_logger.LogInformation(
			"Correct answer from {Nick} ({PlayerId}) in session {SessionId} round {RoundNo} — {Points} pts (title={Title}, artist={Artist})",
			player.Nick, request.PlayerId, sessionId, round.RoundNo, points, newTitle, newArtist);

		await _notifier.PlayerAnsweredCorrectly(sessionId, request.PlayerId, player.Nick, points);

		return new SubmitAnswerResponse
		{
			Accepted = true,
			IsCorrect = true,
			TitleCorrect = newTitle,
			ArtistCorrect = newArtist,
			PointsAwarded = points,
			Message = newTitle && newArtist ? "Both correct!" : newTitle ? "Title correct!" : "Artist correct!"
		};
	}

	private Task<GameSessionEntity> LoadSessionAsync(Guid sessionId, CancellationToken ct) =>
		_dbContext.GameSessions.GetSessionWithRoundsAsync(sessionId, ct);
}