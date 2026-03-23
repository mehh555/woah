import { useState } from "react";
import { createLobby, joinLobby } from "../api/client.js";
import { useSession } from "../context/SessionContext.jsx";
import AboutModal from "../components/AboutModal.jsx";

export default function StartScreen({ onEnter }) {
    const { setSession } = useSession();
    const [mode, setMode] = useState(null);
    const [nick, setNick] = useState("");
    const [maxPlayers, setMaxPlayers] = useState(8);
    const [code, setCode] = useState("");
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(false);
    const [showAbout, setShowAbout] = useState(false);
    const [showGoblinek, setShowGoblinek] = useState(false);

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
                <div className="logo-title">Woah</div>
                <div className="logo-sub">
                    Honorable Mention —{" "}
                    <span className="goblinek-trigger" onClick={() => setShowGoblinek(true)}>goblinek</span>
                </div>
            </div>

            {showGoblinek && (
                <div className="modal-overlay" onClick={() => setShowGoblinek(false)}>
                    <img src="/goblinek.png" alt="goblinek" className="goblinek-img anim-fadeUp" />
                </div>
            )}

            {!mode && (
                <div className="mode-picker anim-fadeUp">
                    <button className="btn btn-primary mode-btn" onClick={() => setMode("create")}>
                        🎮 Stwórz lobby
                    </button>
                    <button className="btn btn-secondary mode-btn" onClick={() => setMode("join")}>
                        🚪 Dołącz do gry
                    </button>
                </div>
            )}

            {mode === "create" && (
                <div className="card form-card anim-fadeUp">
                    <div className="card-label">🎮 Stwórz lobby</div>
                    <input className="input" placeholder="Twój nick" value={nick}
                        onChange={e => setNick(e.target.value)}
                        onKeyDown={e => e.key === "Enter" && handleCreate()}
                        maxLength={20} autoFocus />
                    <div className="form-row">
                        <label className="form-label">Max graczy:</label>
                        <input className="input input-small" type="number" min={2} max={20} value={maxPlayers}
                            onChange={e => setMaxPlayers(Math.min(20, Math.max(2, Number(e.target.value))))} />
                    </div>
                    <button className="btn btn-primary" onClick={handleCreate} disabled={loading}>
                        {loading ? "Tworzę…" : "Utwórz lobby"}
                    </button>
                    <button className="btn-link" onClick={() => { setMode(null); setError(""); }}>
                        ← Wróć
                    </button>
                </div>
            )}

            {mode === "join" && (
                <div className="card form-card anim-fadeUp">
                    <div className="card-label">🚪 Dołącz do gry</div>
                    <input className="input" placeholder="Twój nick" value={nick}
                        onChange={e => setNick(e.target.value)} maxLength={20} autoFocus />
                    <input className="input" placeholder="Kod lobby (np. DTQRXR)" value={code}
                        onChange={e => setCode(e.target.value.toUpperCase())}
                        onKeyDown={e => e.key === "Enter" && handleJoin()} maxLength={8} />
                    <button className="btn btn-primary" onClick={handleJoin} disabled={loading}>
                        {loading ? "Dołączam…" : "Dołącz do lobby"}
                    </button>
                    <button className="btn-link" onClick={() => { setMode(null); setError(""); }}>
                        ← Wróć
                    </button>
                </div>
            )}

            {error && <div className="error-msg anim-fadeUp">⚠️ {error}</div>}

            <button className="btn-link about-btn" onClick={() => setShowAbout(true)}>
                ℹ️ O projekcie
            </button>

            {showAbout && <AboutModal onClose={() => setShowAbout(false)} />}
        </div>
    );
}