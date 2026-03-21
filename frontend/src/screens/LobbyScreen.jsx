import { useCallback, useEffect, useState } from "react";
import { getLobby, createSession, leaveLobby } from "../api/client.js";
import { useSession } from "../context/SessionContext.jsx";
import { usePolling } from "../hooks/usePolling.js";
import { useGameHub } from "../hooks/useGameHub.js";
import DotPulse from "../components/DotPulse.jsx";
import PlaylistPanel from "../components/PlaylistPanel.jsx";

export default function LobbyScreen({ onStart, onExit }) {
    const { session, setSession, clearSession } = useSession();
    const [roundDuration, setRoundDuration] = useState(10);
    const fetchLobby = useCallback(() => getLobby(session.lobbyCode), [session.lobbyCode]);
    const { data: lobby, error, refetch } = usePolling(fetchLobby, 10000);

    const amIHost = lobby ? lobby.hostPlayerId === session.playerId : session.isHost;

    const { invoke, connected } = useGameHub({
        LobbyUpdated: () => refetch(),
        SessionStarted: ({ sessionId }) => {
            setSession(prev => ({ ...prev, sessionId }));
            onStart();
        }
    }, [refetch, setSession, onStart]);

    useEffect(() => {
        if (connected) {
            invoke("JoinLobby", session.lobbyCode);
        }
    }, [connected, invoke, session.lobbyCode]);

    useEffect(() => {
        if (!lobby) return;

        const activePlayerIds = new Set((lobby.players || []).map(player => player.playerId));
        if (!activePlayerIds.has(session.playerId)) {
            clearSession();
            onExit();
            return;
        }

        setSession(prev => ({
            ...prev,
            isHost: lobby.hostPlayerId === prev.playerId,
            sessionId: lobby.currentSessionId ?? prev.sessionId ?? null,
        }));

        if (lobby.status === "InGame" && lobby.currentSessionId) {
            setSession(prev => ({ ...prev, sessionId: lobby.currentSessionId }));
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
            alert("Błąd startu: " + e.message);
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
                <PlaylistPanel lobbyCode={session.lobbyCode} hostPlayerId={session.playerId} />
            )}

            <div style={{ display: "flex", gap: "1rem", width: "100%", maxWidth: 320, flexDirection: "column" }}>
                {amIHost ? (
                    <>
                        <div style={{ display: "flex", alignItems: "center", gap: ".75rem", justifyContent: "center" }}>
                            <label style={{ color: "var(--muted)", fontSize: ".85rem", whiteSpace: "nowrap" }}>Czas rundy:</label>
                            <select
                                className="input"
                                value={roundDuration}
                                onChange={e => setRoundDuration(Number(e.target.value))}
                                style={{ width: "auto", textAlign: "center" }}
                            >
                                {[5, 7, 10, 12, 15].map(s => (
                                    <option key={s} value={s}>{s}s</option>
                                ))}
                            </select>
                        </div>
                        <button className="btn btn-primary" onClick={handleStart}>▶ Start gry</button>
                    </>
                ) : (
                    <div className="waiting-anim">Czekam na start od hosta <DotPulse /></div>
                )}
                <button className="btn btn-secondary" onClick={handleLeave}>← Wyjdź z lobby</button>
            </div>
        </div>
    );
}