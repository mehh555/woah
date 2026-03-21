import { useCallback, useEffect } from "react";
import { getLobby, createSession, leaveLobby } from "../api/client.js";
import { useSession } from "../context/SessionContext.jsx";
import { usePolling } from "../hooks/usePolling.js";
import DotPulse from "../components/DotPulse.jsx";

export default function LobbyScreen({ onStart }) {
  const { session, setSession } = useSession();
  const fetchLobby = useCallback(() => getLobby(session.lobbyCode), [session.lobbyCode]);
  const { data: lobby, error } = usePolling(fetchLobby);

  const amIHost = lobby ? lobby.hostPlayerId === session.playerId : session.isHost;

  // Wyślij leave gdy gracz zamknie okno/tab
  useEffect(() => {
    function handleUnload() {
      navigator.sendBeacon(
        `/api/lobbies/${session.lobbyCode}/leave`,
        JSON.stringify({ playerId: session.playerId })
      );
    }
    window.addEventListener("beforeunload", handleUnload);
    return () => window.removeEventListener("beforeunload", handleUnload);
  }, [session.lobbyCode, session.playerId]);

  async function handleStart() {
    try {
      const res = await createSession(session.lobbyCode);
      setSession(prev => ({ ...prev, sessionId: res.sessionId }));
      onStart(res.sessionId);
    } catch (e) { alert("Błąd startu: " + e.message); }
  }

  async function handleLeave() {
    try {
      await leaveLobby(session.lobbyCode, session.playerId);
    } catch (_) {}
    window.location.reload();
  }

  if (!lobby && !error) return (
    <div className="lobby-screen">
      <div className="waiting-anim">Ładuję lobby <DotPulse /></div>
    </div>
  );

  if (error) return (
    <div className="lobby-screen">
      <div className="error-msg">⚠️ {error}</div>
      <button className="btn btn-secondary" style={{ maxWidth: 200, marginTop: "1rem" }} onClick={() => window.location.reload()}>
        ← Wróć
      </button>
    </div>
  );

  // Jeśli lobby zamknięte przez hosta
  if (lobby.status === "Closed" || lobby.status === "closed") {
    return (
      <div className="lobby-screen">
        <div className="error-msg">⚠️ Host zamknął lobby.</div>
        <button className="btn btn-secondary" style={{ maxWidth: 200, marginTop: "1rem" }} onClick={() => window.location.reload()}>
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
        <div className="lobby-count">{lobby.playerCount} / {lobby.maxPlayers} graczy • Zaproś znajomych!</div>
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
      <div style={{ display: "flex", gap: "1rem", width: "100%", maxWidth: 320, flexDirection: "column" }}>
        {amIHost
          ? <button className="btn btn-primary" onClick={handleStart}>▶ Start gry</button>
          : <div className="waiting-anim">Czekam na start od hosta <DotPulse /></div>
        }
        <button className="btn btn-secondary" onClick={handleLeave}>← Wyjdź z lobby</button>
      </div>
    </div>
  );
}
