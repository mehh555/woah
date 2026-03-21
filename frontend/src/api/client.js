
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


/**
 
 * @param {{ nickname: string }} body
 * @returns {{ lobbyCode: string, playerId: string }}
 */
export function createLobby(nickname, maxPlayers = 20) {
  return request("POST", "/lobbies", { hostNick: nickname, maxPlayers });
}

/**
 * @param {string} lobbyCode
 * @param {{ nickname: string }} body
 * @returns {{ playerId: string }}
 */
export function joinLobby(lobbyCode, nickname) {
  return request("POST", `/lobbies/${lobbyCode}/join`, { nick: nickname });
}

/**
 * Pobiera stan lobby (gracze, host, status).
 * @param {string} lobbyCode
 * @returns {{ code: string, host: string, players: Player[], status: string }}
 */
export function getLobby(lobbyCode) {
  return request("GET", `/lobbies/${lobbyCode}`);
}

/**
 * Opuszcza lobby.
 * @param {string} lobbyCode
 * @param {string} playerId
 */
export function leaveLobby(lobbyCode, playerId) {
  return request("POST", `/lobbies/${lobbyCode}/leave`, { playerId });
}

// ─── LobbyPlaylist ─────────────────────────────────────────────────────────

/**
 * Pobiera playlistę lobby.
 * @param {string} lobbyCode
 * @returns {{ tracks: Track[] }}
 */
export function getPlaylist(lobbyCode) {
  return request("GET", `/lobbies/${lobbyCode}/playlist`);
}

/**
 * Dodaje utwór do playlisty.
 * @param {string} lobbyCode
 * @param {{ title: string, artist: string }} track
 */
export function addTrack(lobbyCode, track) {
  return request("POST", `/lobbies/${lobbyCode}/playlist/tracks`, track);
}

/**
 * Usuwa utwór z playlisty.
 * @param {string} lobbyCode
 * @param {string} trackId
 */
export function removeTrack(lobbyCode, trackId) {
  return request("DELETE", `/lobbies/${lobbyCode}/playlist/tracks/${trackId}`);
}

// ─── Sessions ──────────────────────────────────────────────────────────────

/**
 * Startuje sesję gry (host wywołuje ten endpoint).
 * @param {string} lobbyCode
 * @returns {{ sessionId: string }}
 */
export function createSession(lobbyCode) {
  return request("POST", `/lobbies/${lobbyCode}/session`);
}

/**
 * Pobiera stan aktualnej sesji (polling).
 * @param {string} sessionId
 * @returns {{ sessionId, round, totalRounds, status, currentTrack, players: Player[] }}
 */
export function getSession(sessionId) {
  return request("GET", `/sessions/${sessionId}`);
}

/**
 * Wysyła odpowiedź gracza.
 * @param {string} sessionId
 * @param {{ playerId: string, answer: string }} body
 * @returns {{ correct: boolean, points: number }}
 */
export function submitAnswer(sessionId, playerId, answer) {
  return request("POST", `/sessions/${sessionId}/answer`, { playerId, answer });
}

/**
 * Przechodzi do następnej rundy (host).
 * @param {string} sessionId
 */
export function advanceSession(sessionId) {
  return request("POST", `/sessions/${sessionId}/advance`);
}
