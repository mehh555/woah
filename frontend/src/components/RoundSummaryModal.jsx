export default function RoundSummaryModal({ round, leaderboard, isHost, onNext }) {
    if (!round) return null;

    return (
        <div className="modal-overlay">
            <div className="modal-card anim-fadeUp">
                <div className="modal-header">
                    <span className="round-badge">Runda {round.roundNo}</span>
                </div>

                <div className="modal-song">
                    {round.artworkUrl && (
                        <img src={round.artworkUrl} alt="" className="modal-artwork" />
                    )}
                    <div className="modal-song-info">
                        <div className="modal-song-title">{round.answerTitle}</div>
                        <div className="modal-song-artist">{round.answerArtist}</div>
                    </div>
                </div>

                {round.itunesUrl && (
                    
                        href={round.itunesUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="btn btn-itunes"
                    >
                        🎵 Sprawdź na iTunes
                    </a>
                )}

                <div className="modal-stats">
                    <div className="modal-stat">
                        <span className="modal-stat-value">{round.correctAnswerCount}</span>
                        <span className="modal-stat-label">trafiło</span>
                    </div>
                </div>

                <div className="modal-leaderboard">
                    <table className="leaderboard-table">
                        <thead>
                            <tr>
                                <th>#</th>
                                <th>Gracz</th>
                                <th>Punkty</th>
                            </tr>
                        </thead>
                        <tbody>
                            {leaderboard.map((p, i) => (
                                <tr key={p.playerId} className={round.correctPlayerIds?.includes(p.playerId) ? "row-correct" : ""}>
                                    <td className="rank">{i + 1}</td>
                                    <td className="nick">{p.nick}</td>
                                    <td className="score">{p.score}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>

                {isHost ? (
                    <button className="btn btn-primary" onClick={onNext}>
                        ▶ Następna runda
                    </button>
                ) : (
                    <div className="waiting-anim" style={{ color: "var(--muted)", fontSize: ".85rem" }}>
                        Czekam na hosta…
                    </div>
                )}
            </div>
        </div>
    );
}