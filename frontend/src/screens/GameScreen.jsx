import { useCallback, useEffect, useRef, useState } from "react";
import { getSession, submitAnswer, advanceSession } from "../api/client.js";
import { useSession } from "../context/SessionContext.jsx";
import { usePolling } from "../hooks/usePolling.js";
import { useGameHub } from "../hooks/useGameHub.js";
import Timer from "../components/Timer.jsx";
import RoundSummaryModal from "../components/RoundSummaryModal.jsx";

function MaskedWord({ mask }) {
    if (!mask) return null;
    const chars = [...mask];
    return (
        <div className="word-mask">
            {chars.map((ch, i) =>
                ch === " " ? (
                    <span key={i} className="mask-space" />
                ) : (
                    <span key={i} className="mask-char">_</span>
                )
            )}
        </div>
    );
}

export default function GameScreen() {
    const { session, clearSession } = useSession();
    const [guess, setGuess] = useState("");
    const [feedback, setFeedback] = useState(null);
    const [myGuessed, setMyGuessed] = useState(false);
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
            setMyGuessed(false);
            setFeedback(null);
            setGuess("");
        }
        prevRoundRef.current = roundId;
    }, [gameState?.currentRound?.roundId]);

    useEffect(() => {
        if (!gameState?.currentRound?.previewUrl || !audioRef.current) return;
        const audio = audioRef.current;
        const isPlaying = gameState.currentRound.state === "Playing";

        if (isPlaying) {
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
        if (!guess.trim() || myGuessed) return;
        try {
            const res = await submitAnswer(session.sessionId, session.playerId, guess.trim());
            if (res.isCorrect) {
                setFeedback({ type: "correct", msg: `🎉 Brawo! +${res.pointsAwarded} pkt!` });
                setMyGuessed(true);
            } else {
                setFeedback({ type: "wrong", msg: "❌ Nie tym razem..." });
                setTimeout(() => setFeedback(null), 2000);
            }
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

    return (
        <div className="game-screen">
            <audio ref={audioRef} />

            {/* Round summary modal */}
            {(isRoundRevealed || isFinished) && (
                <RoundSummaryModal
                    round={currentRound}
                    leaderboard={sorted}
                    isHost={session.isHost}
                    onNext={handleAdvance}
                />
            )}

            {/* Playing UI */}
            <div className="game-top">
                <div className="round-badge">
                    Runda {currentRound?.roundNo ?? "?"} / {totalRounds}
                </div>

                {isRoundPlaying && currentRound?.endsAt && (
                    <Timer endsAt={currentRound.endsAt} total={roundDurationSeconds} />
                )}

                <div className="game-mask-area">
                    <MaskedWord mask={currentRound?.answerMask} />
                </div>

                {!myGuessed && isRoundPlaying && (
                    <div className="guess-row">
                        <input
                            ref={inputRef}
                            className="input"
                            placeholder="Wpisz nazwę piosenki…"
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

                {myGuessed && isRoundPlaying && (
                    <div className="feedback correct">🎉 Już zgadłeś! Czekaj na koniec rundy…</div>
                )}

                {feedback && !myGuessed && (
                    <div className={`feedback ${feedback.type}`}>{feedback.msg}</div>
                )}
            </div>

            {/* Leaderboard table — always visible at bottom */}
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
                            <tr key={p.playerId}
                                className={[
                                    p.playerId === session.playerId ? "row-me" : "",
                                    currentRound?.correctPlayerIds?.includes(p.playerId) ? "row-correct" : ""
                                ].filter(Boolean).join(" ")}
                            >
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