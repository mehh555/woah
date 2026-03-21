import { useState, useEffect, useCallback } from "react";
import { searchTracks, getPlaylist, addTrack, removeTrack } from "../api/client.js";

export default function PlaylistPanel({ lobbyCode, hostPlayerId }) {
    const [query, setQuery] = useState("");
    const [results, setResults] = useState([]);
    const [tracks, setTracks] = useState([]);
    const [searching, setSearching] = useState(false);
    const [error, setError] = useState("");

    const refreshPlaylist = useCallback(async () => {
        try {
            const res = await getPlaylist(lobbyCode);
            setTracks(res.tracks || []);
        } catch { }
    }, [lobbyCode]);

    useEffect(() => { refreshPlaylist(); }, [refreshPlaylist]);

    async function handleSearch() {
        if (!query.trim()) return;
        setSearching(true);
        setError("");
        try {
            const res = await searchTracks(query.trim());
            setResults(res || []);
        } catch (e) {
            setError(e.message);
        } finally {
            setSearching(false);
        }
    }

    async function handleAdd(trackId) {
        setError("");
        try {
            await addTrack(lobbyCode, hostPlayerId, trackId);
            setResults(prev => prev.filter(t => t.trackId !== trackId));
            await refreshPlaylist();
        } catch (e) {
            setError(e.message);
        }
    }

    async function handleRemove(trackId) {
        setError("");
        try {
            await removeTrack(lobbyCode, hostPlayerId, trackId);
            await refreshPlaylist();
        } catch (e) {
            setError(e.message);
        }
    }

    return (
        <div className="playlist-panel">
            <div className="playlist-header">
                <span className="playlist-title">🎵 Playlista</span>
                <span className="playlist-count">{tracks.length} / 20</span>
            </div>

            <div className="search-row">
                <input
                    className="input"
                    placeholder="Szukaj piosenki..."
                    value={query}
                    onChange={e => setQuery(e.target.value)}
                    onKeyDown={e => e.key === "Enter" && handleSearch()}
                />
                <button className="btn btn-primary btn-sm" onClick={handleSearch} disabled={searching || !query.trim()}>
                    {searching ? "..." : "Szukaj"}
                </button>
            </div>

            {error && <div className="error-msg" style={{ fontSize: ".8rem" }}>⚠️ {error}</div>}

            {results.length > 0 && (
                <div className="search-results">
                    {results.map(t => (
                        <div key={t.trackId} className="track-row search-result-row">
                            {t.artworkUrl && <img src={t.artworkUrl} alt="" className="track-art" />}
                            <div className="track-info">
                                <div className="track-title">{t.title}</div>
                                <div className="track-artist">{t.artist}</div>
                            </div>
                            <button className="btn-icon btn-add" onClick={() => handleAdd(t.trackId)} title="Dodaj">+</button>
                        </div>
                    ))}
                </div>
            )}

            {tracks.length > 0 && (
                <div className="playlist-tracks">
                    {tracks.map((t, i) => (
                        <div key={t.trackId} className="track-row playlist-track-row">
                            <span className="track-number">{i + 1}</span>
                            <div className="track-info">
                                <div className="track-title">{t.title}</div>
                                <div className="track-artist">{t.artist}</div>
                            </div>
                            <button className="btn-icon btn-remove" onClick={() => handleRemove(t.trackId)} title="Usuń">×</button>
                        </div>
                    ))}
                </div>
            )}

            {tracks.length === 0 && (
                <div className="playlist-empty">Dodaj piosenki aby rozpocząć grę</div>
            )}
        </div>
    );
}