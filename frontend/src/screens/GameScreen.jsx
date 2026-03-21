import { useCallback, useEffect, useRef, useState } from "react";
import { getSession, submitAnswer, advanceSession } from "../api/client.js";
import { useSession } from "../context/SessionContext.jsx";
import { usePolling } from "../hooks/usePolling.js";
import { useGameHub } from "../hooks/useGameHub.js";
import PlayersPanel from "../components/PlayersPanel.jsx";
import Timer from "../components/Timer.jsx";

export default function GameScreen() {
    const { session, clearSession } = useSession();
    const [guess, setGuess] = useState("");
    const [feedback, setFeedback] = useState(null);
    const [myGuessed, setMyGuessed] = useState(false);
    const [animScores, setAnimScores] = useState({});
    const prevScoresRef = useRef({});
    const prevRoundRef = useRef(null);
    const inputRef = useRef(null);
    const audioRef = useRef(null);

    const fetchSession = useCallback(() => getSession(session.sessionId), [session.sessionId]);
    const { data: gameState, error, refetch } = usePolling(fetchSession, 10000);

    const { invoke, connected } = useGameHub({
        SessionUpdated: () => refetch(),
        PlayerAnsweredCorrectly: ({ playerId }) => {
            setAnimScores(prev => ({ ...prev, [playerId]: true }));
            setTimeout(() => setAnimScores(prev => {
                const next = { ...prev };
                delete next[playerId];
                return next;
            }), 600);
            refetch();
        }
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

        const iAmParticipant = gameState.leaderboard.some(player => player.playerId === session.playerId);
        if (!iAmParticipant) {
            clearSession();
            return;
        }

        const newScores = {};
        gameState.leaderboard.forEach(p => {
            if (prevScoresRef.current[p.playerId] !== undefined && p.score !== prevScoresRef.current[p.playerId]) {
                newScores[p.playerId] = true;
            }
        });
        if (Object.keys(newScores).length) {
            setAnimScores(prev => ({ ...prev, ...newScores }));
            setTimeout(() => setAnimScores({}), 600);
        }
        prevScoresRef.current = Object.fromEntries(gameState.leaderboard.map(p => [p.playerId, p.score]));
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

    const correctIds = new Set(currentRound?.correctPlayerIds || []);

    const songDisplay = isRoundRevealed || isFinished
        ? currentRound?.answerTitle ?? ""
        : currentRound?.answerMask ?? "";

    const playersWithGuessed = (leaderboard || []).map(p => ({
        ...p,
        guessed: correctIds.has(p.playerId)
    }));

    const sorted = [...playersWithGuessed].sort((a, b) => b.score - a.score);

    return (
        <div className="game-screen">
            <audio ref={audioRef} />
            <div className="game-top">
                <div className="round-badge">Runda {currentRound?.roundNo ?? "?"} / {totalRounds}</div>

                {isRoundPlaying && currentRound?.endsAt && (
                    <Timer endsAt={currentRound.endsAt} total={roundDurationSeconds} />
                )}

                <div className="song-area">
                    <div className="music-icon">{isRoundPlaying ? "🎵" : isRoundRevealed ? "🎶" : "🏆"}</div>
                    <div className="song-hint">
                        {isRoundPlaying ? "Odgadnij piosenkę:" : isRoundRevealed ? "Odpowiedź:" : ""}
                    </div>
                    <div className={`song-chars ${isRoundRevealed ? "revealed" : ""}`}>{songDisplay}</div>
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
                        <button className="btn btn-primary btn-sm" onClick={handleGuess} disabled={!guess.trim()}>Zgadnij</button>
                    </div>
                )}

                {myGuessed && isRoundPlaying && (
                    <div className="feedback correct">🎉 Już zgadłeś! Czekaj na koniec rundy…</div>
                )}

                {feedback && !myGuessed && (
                    <div className={`feedback ${feedback.type}`}>{feedback.msg}</div>
                )}

                {isRoundRevealed && session.isHost && (
                    <button className="btn btn-primary" onClick={handleAdvance}>▶ Następna runda</button>
                )}

                {isRoundRevealed && !session.isHost && (
                    <div className="waiting-anim" style={{ color: "var(--muted)", fontSize: ".85rem" }}>
                        Czekam na hosta…
                    </div>
                )}

                {isFinished && (
                    <div className="feedback correct" style={{ fontSize: "1.2rem", padding: "1rem" }}>
                        🏆 Gra zakończona!
                    </div>
                )}
            </div>
            <PlayersPanel players={sorted} myId={session.playerId} animScores={animScores} />
        </div>
    );
}