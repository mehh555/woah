import { useCallback, useEffect, useState } from "react";
import { getLobby, createSession, leaveLobby } from "../api/client.js";
import { useSession } from "../context/SessionContext.jsx";
import { usePolling } from "../hooks/usePolling.js";
import { useLobbySubscription } from "../hooks/useLobbySubscription.js";
import DotPulse from "../components/DotPulse.jsx";
import PlaylistPanel from "../components/PlaylistPanel.jsx";

export default function LobbyScreen({ onStart, onExit }) {
    const { session, setSession, clearSession } = useSession();
    const [roundDuration, setRoundDuration] = useState(10);
    const [startError, setStartError] = useState("");

    const fetchLobby = useCallback(() => getLobby(session.lobbyCode), [session.lobbyCode]);

    const { connected } = useLobbySubscription(session.lobbyCode, {
        LobbyUpdated: () => refetch(),
        SessionStarted: ({ sessionId }) => {
            setSession(prev => ({ ...prev, sessionId }));
            onStart();
        },
    });

    const { data: lobby, error, refetch } = usePolling(fetchLobby, {
        interval: 10000,
        enabled: !connected,
    });

    const amIHost = lobby ? lobby.hostPlayerId === session.playerId : session.isHost;

    useEffect(() => {
        if (!lobby) return;

        const activePlayerIds = new Set((lobby.players || []).map(player => player.playerId));
        if (!activePlayerIds.has(session.playerId)) {
            clearSession();
            onExit();
            return;
        }

        setSession(prev => {
            const newIsHost = lobby.hostPlayerId === prev.playerId;
            const newPlaylistId = lobby.activePlaylistId ?? prev.playlistId;
            const newSessionId = lobby.currentSessionId ?? prev.sessionId ?? null;

            if (prev.isHost === newIsHost && prev.playlistId === newPlaylistId && prev.sessionId === newSessionId) {
                return prev;
            }

            return { ...prev, isHost: newIsHost, playlistId: newPlaylistId, sessionId: newSessionId };
        });

        if (lobby.status === "InGame" && lobby.currentSessionId) {
            setSession(prev => prev.sessionId === lobby.currentSessionId ? prev : { ...prev, sessionId: lobby.currentSessionId });
            onStart();
        }
    }, [lobby, session.playerId, setSession, clearSession, onStart, onExit]);

    async function handleStart() {
        try {
            const res = await createSession(
                session.lobbyCode,
                session.playerId,
                session.playlistId,
                roundDuration
            );
            setSession(prev => ({ ...prev, sessionId: res.sessionId }));
            onStart();
        } catch (e) {
            setStartError(e.message);
            setTimeout(() => setStartError(""), 4000);
        }
    }

    async function handleLeave() {
        try {
            await leaveLobby(session.lobbyCode, session.playerId);
        } catch (e) {
            console.error(e);
        }
        clearSession();
        onExit();
    }

    if (!lobby && !error) return (
        <div className="lobby-screen">
            <div className="waiting-anim">Ładuję lobby <DotPulse /></div>
        </div>
    );

    if (error) return (
        <div className="lobby-screen">
            <div className="error-msg">⚠️ {error}</div>
            <button className="btn btn-secondary" style={{ maxWidth: 200, marginTop: "1rem" }} onClick={() => {
                clearSession();
                onExit();
            }}>
                ← Wróć
            </button>
        </div>
    );

    if (lobby.status === "Finished" || lobby.status === "finished") {
        return (
            <div className="lobby-screen">
                <div className="error-msg">⚠️ Host zamknął lobby.</div>
                <button className="btn btn-secondary" style={{ maxWidth: 200, marginTop: "1rem" }} onClick={() => {
                    clearSession();
                    onExit();
                }}>
                    ← Wróć do menu
                </button>
            </div>
        );
    }

    return (
        <div className="lobby-screen anim-fadeUp">
            <div className="lobby-header">
                <div className="lobby-code-label">Kod lobby</div>
                <div className="lobby-code">{lobby.code}</div>
                <div className="lobby-count">{lobby.playerCount} / {lobby.maxPlayers} graczy</div>
            </div>

            <div className="players-list">
                {lobby.players.map((p, i) => (
                    <div className="player-row" key={p.playerId} style={{ animationDelay: `${i * 0.05}s` }}>
                        <span className="player-dot" />
                        <span className="player-nick">
                            {p.nick}
                            {p.playerId === session.playerId && (
                                <span style={{ color: "var(--muted)", fontSize: ".8rem" }}> (Ty)</span>
                            )}
                        </span>
                        {p.isHost && <span className="host-badge">👑 Host</span>}
                    </div>
                ))}
            </div>

            {amIHost && (
                <PlaylistPanel lobbyCode={session.lobbyCode} playerId={session.playerId} />
            )}

            <div className="lobby-actions">
                {amIHost ? (
                    <>
                        <div className="round-duration-picker">
                            <label className="round-duration-label">Czas rundy:</label>
                            <select
                                className="input round-duration-select"
                                value={roundDuration}
                                onChange={e => setRoundDuration(Number(e.target.value))}
                            >
                                {[5, 7, 10, 12, 15].map(s => (
                                    <option key={s} value={s}>{s}s</option>
                                ))}
                            </select>
                        </div>
                        <button className="btn btn-primary" onClick={handleStart}>▶ Start gry</button>
                        {startError && <div className="error-msg" style={{ fontSize: ".85rem" }}>{startError}</div>}
                    </>
                ) : (
                    <div className="waiting-anim">Czekam na start od hosta <DotPulse /></div>
                )}
                <button className="btn btn-secondary" onClick={handleLeave}>← Wyjdź z lobby</button>
            </div>
        </div>
    );
}