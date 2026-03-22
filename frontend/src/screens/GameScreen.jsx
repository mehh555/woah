import { useCallback, useEffect, useRef, useState } from "react";
import { getSession, submitAnswer, advanceSession } from "../api/client.js";
import { useSession } from "../context/SessionContext.jsx";
import { usePolling } from "../hooks/usePolling.js";
import { useGameHub } from "../hooks/useGameHub.js";
import Timer from "../components/Timer.jsx";
import RoundSummaryModal from "../components/RoundSummaryModal.jsx";

function MaskedWord({ mask, label }) {
    if (!mask) return null;
    const chars = [...mask];
    return (
        <div className="mask-group">
            <div className="mask-label">{label}</div>
            <div className="word-mask">
                {chars.map((ch, i) =>
                    ch === " "
                        ? <span key={i} className="mask-space" />
                        : <span key={i} className="mask-char">_</span>
                )}
            </div>
        </div>
    );
}

export default function GameScreen({ onExit }) {
    const { session, clearSession } = useSession();
    const [guess, setGuess] = useState("");
    const [feedback, setFeedback] = useState(null);
    const [myTitleGuessed, setMyTitleGuessed] = useState(false);
    const [myArtistGuessed, setMyArtistGuessed] = useState(false);
    const prevRoundRef = useRef(null);
    const inputRef = useRef(null);
    const audioRef = useRef(null);

    const fetchSession = useCallback(() => getSession(session.sessionId), [session.sessionId]);
    const { data: gameState, error, refetch } = usePolling(fetchSession, 10000);

    const { invoke, connected } = useGameHub({
        SessionUpdated: () => refetch(),
        PlayerAnsweredCorrectly: () => refetch(),
    }, [refetch]);

    useEffect(() => {
        if (connected && session.sessionId) {
            invoke("JoinSession", session.sessionId);
        }
    }, [connected, invoke, session.sessionId]);

    useEffect(() => {
        if (!gameState?.currentRound) return;
        const roundId = gameState.currentRound.roundId;
        if (prevRoundRef.current && prevRoundRef.current !== roundId) {
            setMyTitleGuessed(false);
            setMyArtistGuessed(false);
            setFeedback(null);
            setGuess("");
        }
        prevRoundRef.current = roundId;
    }, [gameState?.currentRound?.roundId]);

    useEffect(() => {
        if (!gameState?.currentRound?.previewUrl || !audioRef.current) return;
        const audio = audioRef.current;
        if (gameState.currentRound.state === "Playing") {
            audio.src = gameState.currentRound.previewUrl;
            audio.currentTime = 0;
            audio.play().catch(() => { });
        } else {
            audio.pause();
        }
    }, [gameState?.currentRound?.previewUrl, gameState?.currentRound?.state]);

    useEffect(() => {
        if (!gameState?.leaderboard) return;
        const iAmParticipant = gameState.leaderboard.some(p => p.playerId === session.playerId);
        if (!iAmParticipant) {
            clearSession();
        }
    }, [gameState, session.playerId, clearSession]);

    async function handleGuess() {
        if (!guess.trim()) return;
        if (myTitleGuessed && myArtistGuessed) return;
        try {
            const res = await submitAnswer(session.sessionId, session.playerId, guess.trim());
            if (res.alreadyAnswered) {
                setFeedback({ type: "info", msg: "✅ Już odpowiedziano poprawnie." });
            } else if (res.isCorrect) {
                setFeedback({ type: "correct", msg: `🎉 Poprawna odpowiedź! +${res.pointsAwarded} pkt!` });
                setMyTitleGuessed(true);
                setMyArtistGuessed(true);
            } else if (res.accepted) {
                setFeedback({ type: "wrong", msg: "❌ Nie tym razem..." });
                setTimeout(() => setFeedback(null), 2000);
            } else {
                setFeedback({ type: "wrong", msg: `⚠️ ${res.message}` });
                setTimeout(() => setFeedback(null), 2000);
            }
            refetch();
        } catch (e) {
            setFeedback({ type: "wrong", msg: "⚠️ " + e.message });
            setTimeout(() => setFeedback(null), 2000);
        }
        setGuess("");
        inputRef.current?.focus();
    }

    async function handleAdvance() {
        try {
            await advanceSession(session.sessionId, session.playerId);
        } catch (e) {
            alert("Błąd: " + e.message);
        }
    }

    function handleBackToMenu() {
        clearSession();
        onExit();
    }

    if (!session?.sessionId) {
        return <div className="error-msg" style={{ margin: "2rem" }}>⚠️ Brak aktywnej sesji.</div>;
    }

    if (!gameState && !error) return (
        <div className="game-screen" style={{ display: "flex", alignItems: "center", justifyContent: "center" }}>
            <div style={{ color: "var(--muted)" }}>Ładuję grę…</div>
        </div>
    );

    if (error) return <div className="error-msg" style={{ margin: "2rem" }}>⚠️ {error}</div>;

    const { currentRound, totalRounds, isFinished, leaderboard, roundDurationSeconds } = gameState;
    const isRoundPlaying = currentRound?.state === "Playing";
    const isRoundRevealed = currentRound?.state === "Revealed";
    const sorted = [...(leaderboard || [])].sort((a, b) => b.score - a.score);
    const bothGuessed = myTitleGuessed && myArtistGuessed;
    const canStillGuess = isRoundPlaying && !bothGuessed;

    // ─── FINISHED SCREEN ─────────────────────────────────
    if (isFinished) {
        const winner = sorted[0];
        return (
            <div className="game-screen">
                <audio ref={audioRef} />
                <div className="game-top">
                    <div className="finish-trophy">🏆</div>
                    <div className="finish-title">Koniec gry!</div>
                    {winner && (
                        <div className="finish-winner">
                            Wygrywa <strong>{winner.nick}</strong> z wynikiem <strong>{winner.score} pkt</strong>
                        </div>
                    )}

                    <div className="finish-leaderboard">
                        <table className="leaderboard-table">
                            <thead>
                                <tr>
                                    <th>#</th>
                                    <th>Gracz</th>
                                    <th>Trafienia</th>
                                    <th>Punkty</th>
                                </tr>
                            </thead>
                            <tbody>
                                {sorted.map((p, i) => (
                                    <tr key={p.playerId} className={p.playerId === session.playerId ? "row-me" : ""}>
                                        <td className="rank">{i === 0 ? "🥇" : i === 1 ? "🥈" : i === 2 ? "🥉" : i + 1}</td>
                                        <td className="nick">{p.nick}</td>
                                        <td className="hits">{p.correctAnswers}</td>
                                        <td className="score">{p.score} pkt</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>

                    <button className="btn btn-primary" onClick={handleBackToMenu}>
                        ← Wróć do menu
                    </button>
                </div>
            </div>
        );
    }

    // ─── ROUND SUMMARY MODAL ─────────────────────────────
    if (isRoundRevealed) {
        return (
            <div className="game-screen">
                <audio ref={audioRef} />
                <RoundSummaryModal
                    round={currentRound}
                    leaderboard={sorted}
                    isHost={session.isHost}
                    onNext={handleAdvance}
                />
            </div>
        );
    }

    // ─── PLAYING UI ──────────────────────────────────────
    return (
        <div className="game-screen">
            <audio ref={audioRef} />
            <div className="game-top">
                <div className="round-badge">Runda {currentRound?.roundNo ?? "?"} / {totalRounds}</div>

                {isRoundPlaying && currentRound?.endsAt && (
                    <Timer endsAt={currentRound.endsAt} total={roundDurationSeconds} />
                )}

                <div className="game-mask-area">
                    <MaskedWord mask={currentRound?.answerTitleMask} label="Tytuł" />
                    <MaskedWord mask={currentRound?.answerArtistMask} label="Artysta" />
                </div>

                <div className="guess-status">
                    {myTitleGuessed && <span className="guess-tag correct">✓ Tytuł</span>}
                    {myArtistGuessed && <span className="guess-tag correct">✓ Artysta</span>}
                    {!myTitleGuessed && isRoundPlaying && <span className="guess-tag pending">? Tytuł</span>}
                    {!myArtistGuessed && isRoundPlaying && <span className="guess-tag pending">? Artysta</span>}
                </div>

                {canStillGuess && (
                    <div className="guess-row">
                        <input
                            ref={inputRef}
                            className="input"
                            placeholder="Wpisz tytuł lub artystę…"
                            value={guess}
                            onChange={e => setGuess(e.target.value)}
                            onKeyDown={e => e.key === "Enter" && handleGuess()}
                            autoFocus
                        />
                        <button className="btn btn-primary btn-sm" onClick={handleGuess} disabled={!guess.trim()}>
                            Zgadnij
                        </button>
                    </div>
                )}

                {bothGuessed && isRoundPlaying && (
                    <div className="feedback correct">🎉 Zgadłeś wszystko! Czekaj na koniec rundy…</div>
                )}

                {feedback && !bothGuessed && (
                    <div className={`feedback ${feedback.type}`}>{feedback.msg}</div>
                )}
            </div>

            <div className="game-leaderboard">
                <table className="leaderboard-table">
                    <thead>
                        <tr>
                            <th>#</th>
                            <th>Gracz</th>
                            <th>Punkty</th>
                        </tr>
                    </thead>
                    <tbody>
                        {sorted.map((p, i) => (
                            <tr key={p.playerId} className={p.playerId === session.playerId ? "row-me" : ""}>
                                <td className="rank">{i + 1}</td>
                                <td className="nick">{p.nick}</td>
                                <td className="score">{p.score} pkt</td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </div>
    );
}