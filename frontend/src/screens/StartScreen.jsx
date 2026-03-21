import { useState } from "react";
import { createLobby, joinLobby } from "../api/client.js";
import { useSession } from "../context/SessionContext.jsx";

export default function StartScreen({ onEnter }) {
    const { setSession } = useSession();
    const [nick, setNick] = useState("");
    const [maxPlayers, setMaxPlayers] = useState(20);
    const [code, setCode] = useState("");
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(false);

    async function handleCreate() {
        if (!nick.trim()) { setError("Wpisz nick!"); return; }
        setLoading(true); setError("");
        try {
            const res = await createLobby(nick.trim(), maxPlayers);
            const sess = {
                playerId: res.hostPlayerId,
                lobbyCode: res.lobbyCode,
                nick: nick.trim(),
                isHost: true,
                playlistId: res.playlistId,
            };
            setSession(sess);
            onEnter(sess);
        } catch (e) { setError(e.message); }
        finally { setLoading(false); }
    }

    async function handleJoin() {
        if (!nick.trim()) { setError("Wpisz nick!"); return; }
        if (!code.trim()) { setError("Wpisz kod lobby!"); return; }
        setLoading(true); setError("");
        try {
            const res = await joinLobby(code.trim().toUpperCase(), nick.trim());
            const sess = {
                playerId: res.playerId,
                lobbyCode: res.lobbyCode || code.trim().toUpperCase(),
                nick: nick.trim(),
                isHost: false,
                playlistId: null,
            };
            setSession(sess);
            onEnter(sess);
        } catch (e) { setError(e.message); }
        finally { setLoading(false); }
    }

    return (
        <div className="start-screen">
            <div className="logo anim-fadeDown">
                <div className="logo-title">Muzyczne<br />Kalambury</div>
                <div className="logo-sub">🎵 Zgadnij piosenkę!</div>
            </div>
            <div className="cards-row anim-fadeUp">
                <div className="card">
                    <div className="card-label">🎮 Stwórz lobby</div>
                    <input className="input" placeholder="Twój nick" value={nick}
                        onChange={e => setNick(e.target.value)} onKeyDown={e => e.key === "Enter" && handleCreate()} maxLength={20} />
                    <div style={{ display: "flex", alignItems: "center", gap: ".75rem" }}>
                        <label style={{ color: "var(--muted)", fontSize: ".9rem", whiteSpace: "nowrap" }}>Max graczy:</label>
                        <input className="input" type="number" min={2} max={20} value={maxPlayers}
                            onChange={e => setMaxPlayers(Math.min(20, Math.max(2, Number(e.target.value))))}
                            style={{ width: "80px", textAlign: "center" }} />
                    </div>
                    <button className="btn btn-primary" onClick={handleCreate} disabled={loading}>
                        {loading ? "Tworzę…" : "Utwórz lobby"}
                    </button>
                </div>
                <div className="card">
                    <div className="card-label">🚪 Dołącz do gry</div>
                    <input className="input" placeholder="Twój nick" value={nick}
                        onChange={e => setNick(e.target.value)} maxLength={20} />
                    <input className="input" placeholder="Kod lobby (np. DTQRXR)" value={code}
                        onChange={e => setCode(e.target.value.toUpperCase())} onKeyDown={e => e.key === "Enter" && handleJoin()} maxLength={8} />
                    <button className="btn btn-secondary" onClick={handleJoin} disabled={loading}>
                        {loading ? "Dołączam…" : "Dołącz do lobby"}
                    </button>
                </div>
            </div>
            {error && <div className="error-msg">⚠️ {error}</div>}
        </div>
    );
}