import { useCallback, useEffect, useRef, useState } from "react";
import { getSession, submitAnswer, advanceSession } from "../api/client.js";
import { useSession } from "../context/SessionContext.jsx";
import { usePolling } from "../hooks/usePolling.js";
import PlayersPanel from "../components/PlayersPanel.jsx";
import Timer from "../components/Timer.jsx";

export default function GameScreen() {
    const { session } = useSession();
    const [guess, setGuess] = useState("");
    const [feedback, setFeedback] = useState(null);
    const [myGuessed, setMyGuessed] = useState(false);
    const [animScores, setAnimScores] = useState({});
    const prevScoresRef = useRef({});
    const inputRef = useRef(null);

    const fetchSession = useCallback(() => getSession(session.sessionId), [session.sessionId]);
    const { data: gameState, error } = usePolling(fetchSession, gameState?.isFinished ? null : 1500);

    useEffect(() => {
        if (!gameState?.leaderboard) return;
        const anims = {};
        gameState.leaderboard.forEach(p => {
            if (prevScoresRef.current[p.playerId] !== undefined && p.score !== prevScoresRef.current[p.playerId]) {
                anims[p.playerId] = true;
            }
        });
        if (Object.keys(anims).length) {
            setAnimScores(anims);
            setTimeout(() => setAnimScores({}), 600);
        }
        prevScoresRef.current = Object.fromEntries(gameState.leaderboard.map(p => [p.playerId, p.score]));
    }, [gameState]);

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
            setMyGuessed(false);
            setFeedback(null);
        } catch (e) { alert("Błąd: " + e.message); }
    }

    if (!gameState && !error) return (
        <div className="game-screen" style={{ display: "flex", alignItems: "center", justifyContent: "center" }}>
            <div style={{ color: "var(--muted)" }}>Ładuję grę…</div>
        </div>
    );
    if (error) return <div className="error-msg" style={{ margin: "2rem" }}>⚠️ {error}</div>;

    const { currentRound, totalRounds, isFinished, leaderboard } = gameState;
    const sorted = [...(leaderboard || [])].sort((a, b) => b.score - a.score);

    const songDisplay = session.isHost
        ? `${currentRound?.answerTitle ?? "?"}`
        : (currentRound?.answerTitle || "").split("").map(c => c === " " ? " " : "•").join("");

    const isRoundPlaying = currentRound?.state === "Playing";
    const isRoundRevealed = currentRound?.state === "Revealed";

    return (
        <div className="game-screen">
            <div className="game-top">
                <div className="round-badge">Runda {currentRound?.roundNo ?? "?"} / {totalRounds}</div>
                {isRoundPlaying && currentRound?.endsAt && (
                    <Timer endsAt={currentRound.endsAt} />
                )}
                <div className="song-area">
                    <div className="music-icon">🎵</div>
                    <div className="song-hint">{session.isHost ? "Twoja piosenka:" : "Odgadnij piosenkę:"}</div>
                    <div className="song-chars">{songDisplay}</div>
                </div>
                {!session.isHost && !myGuessed && isRoundPlaying && (
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
                        <button className="btn btn-primary btn-sm" onClick={handleGuess} disabled={!guess.trim()}>Zgadnij</button>
                    </div>
                )}
                {myGuessed && <div className="feedback correct">🎉 Już zgadłeś! Czekaj na pozostałych…</div>}
                {session.isHost && isRoundRevealed && (
                    <button className="btn btn-primary" onClick={handleAdvance}>▶ Następna runda</button>
                )}
                {feedback && !myGuessed && <div className={`feedback ${feedback.type}`}>{feedback.msg}</div>}
                {isFinished && (
                    <div className="feedback correct" style={{ fontSize: "1.2rem", padding: "1rem" }}>🏆 Gra zakończona!</div>
                )}
            </div>
            <PlayersPanel players={sorted} myId={session.playerId} animScores={animScores} />
        </div>
    );
}