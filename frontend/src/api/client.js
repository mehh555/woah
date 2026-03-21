const BASE = "/api";

async function request(method, path, body) {
    const res = await fetch(`${BASE}${path}`, {
        method,
        headers: { "Content-Type": "application/json" },
        body: body !== undefined ? JSON.stringify(body) : undefined,
    });

    if (!res.ok) {
        const err = await res.json().catch(() => ({ message: res.statusText }));
        throw new Error(err.message || `HTTP ${res.status}`);
    }

    if (res.status === 204) return null;
    return res.json();
}

export function createLobby(nickname, maxPlayers = 20) {
    return request("POST", "/lobbies", { hostNick: nickname, maxPlayers });
}

export function joinLobby(lobbyCode, nickname) {
    return request("POST", `/lobbies/${lobbyCode}/join`, { nick: nickname });
}

export function getLobby(lobbyCode) {
    return request("GET", `/lobbies/${lobbyCode}`);
}

export function leaveLobby(lobbyCode, playerId) {
    return request("POST", `/lobbies/${lobbyCode}/leave`, { playerId });
}

export function getPlaylist(lobbyCode) {
    return request("GET", `/lobbies/${lobbyCode}/playlist`);
}

export function searchTracks(term) {
    return request("GET", `/itunes/search?term=${encodeURIComponent(term)}`);
}

export function addTrack(lobbyCode, hostPlayerId, trackId) {
    return request("POST", `/lobbies/${lobbyCode}/playlist/tracks`, { hostPlayerId, trackId });
}

export function removeTrack(lobbyCode, hostPlayerId, trackId) {
    return request("DELETE", `/lobbies/${lobbyCode}/playlist/tracks/${trackId}?hostPlayerId=${hostPlayerId}`);
}

export function createSession(lobbyCode, hostPlayerId, playlistId, roundDurationSeconds = 10) {
    return request("POST", `/lobbies/${lobbyCode}/session`, {
        hostPlayerId,
        playlistId,
        roundDurationSeconds,
    });
}

export function getSession(sessionId) {
    return request("GET", `/sessions/${sessionId}`);
}

export function submitAnswer(sessionId, playerId, answer) {
    return request("POST", `/sessions/${sessionId}/answer`, { playerId, answer });
}

export function advanceSession(sessionId, hostPlayerId) {
    return request("POST", `/sessions/${sessionId}/advance`, { hostPlayerId });
}