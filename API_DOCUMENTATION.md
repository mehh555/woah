# Woah - API Documentation

## Base URL

All REST endpoints are prefixed with `/api`. SignalR hub is available at `/hubs/game`.

## Error Format

All errors (including validation, rate limiting, and server errors) are returned as `application/problem+json`:

```json
{
  "status": 400,
  "title": "Bad Request",
  "detail": "Descriptive error message.",
  "instance": "/api/lobbies"
}
```

Common HTTP status codes across all endpoints:

| Code | Meaning |
|------|---------|
| 400  | Bad Request / Validation error |
| 403  | Forbidden (e.g., not the host) |
| 404  | Resource not found |
| 409  | Conflict (concurrent modification, duplicate record) |
| 429  | Rate limit exceeded |
| 500  | Internal server error |

---

## REST Endpoints

### Lobbies

#### POST /api/lobbies

Create a new lobby. Rate limited: 3 requests per 30 seconds per IP.

**Request body:**
```json
{
  "hostNick": "string (1-20 chars, required)",
  "maxPlayers": 8  // optional, range 2-20, default 8
}
```

**Response 200:**
```json
{
  "lobbyId": "guid",
  "lobbyCode": "string (e.g. ABCD12)",
  "hostPlayerId": "guid",
  "playlistId": "guid"
}
```

---

#### POST /api/lobbies/{lobbyCode}/join

Join an existing lobby.

**Request body:**
```json
{
  "nick": "string (1-20 chars, required)"
}
```

**Response 200:**
```json
{
  "playerId": "guid",
  "lobbyId": "guid",
  "lobbyCode": "string",
  "nick": "string"
}
```

**Errors:** 400 if lobby is not in `Waiting` status, lobby is full, or nick is already taken.

---

#### GET /api/lobbies/{lobbyCode}

Get lobby state with player list.

**Response 200:**
```json
{
  "lobbyId": "guid",
  "code": "string",
  "status": "Waiting | InGame | Finished",
  "maxPlayers": 8,
  "hostPlayerId": "guid",
  "playerCount": 3,
  "currentSessionId": "guid | null",
  "activePlaylistId": "guid",
  "players": [
    {
      "playerId": "guid",
      "nick": "string",
      "isHost": true,
      "joinedAt": "datetime"
    }
  ]
}
```

---

#### POST /api/lobbies/{lobbyCode}/leave

Leave a lobby. If the host leaves, the lobby is closed for all players.

**Request body:**
```json
{
  "playerId": "guid (required, non-empty)"
}
```

**Response 200:**
```json
{
  "lobbyId": "guid",
  "lobbyCode": "string",
  "playerId": "guid",
  "wasHost": true,
  "lobbyStatus": "Waiting | Finished"
}
```

**Errors:** 400 if lobby is not in `Waiting` status or player is not an active member.

---

### Playlist

#### GET /api/lobbies/{lobbyCode}/playlist

Get the active playlist for a lobby.

**Response 200:**
```json
{
  "lobbyCode": "string",
  "trackCount": 5,
  "tracks": [
    {
      "trackId": 123456,
      "title": "string",
      "artist": "string",
      "previewUrl": "string (URL)",
      "artworkUrl": "string | null",
      "durationMs": 30000,
      "addedAt": "datetime",
      "addedByPlayerId": "guid"
    }
  ]
}
```

---

#### POST /api/lobbies/{lobbyCode}/playlist/tracks

Add a track to the lobby playlist.

**Request body:**
```json
{
  "playerId": "guid (required, non-empty)",
  "trackId": 123456  // required, positive long
}
```

**Response 200:** Returns updated `GetLobbyPlaylistResponse` (same as GET).

---

#### DELETE /api/lobbies/{lobbyCode}/playlist/tracks/{trackId}

Remove a track from the lobby playlist.

**Request body:**
```json
{
  "playerId": "guid (required, non-empty)"
}
```

**Response 200:** Returns updated `GetLobbyPlaylistResponse` (same as GET).

---

### iTunes Search

#### GET /api/itunes/search?term={term}

Search for tracks via iTunes API. Rate limited: 5 requests per 10 seconds per IP.

**Query parameters:**
- `term` (required) - search query string

**Response 200:**
```json
[
  {
    "trackId": 123456,
    "title": "string",
    "artist": "string",
    "previewUrl": "string (URL)",
    "artworkUrl": "string | null",
    "durationMs": 30000,
    "collectionName": "string | null"
  }
]
```

**Errors:** 400 if `term` is empty or whitespace.

---

### Sessions

#### POST /api/lobbies/{lobbyCode}/session

Start a new game session for a lobby.

**Request body:**
```json
{
  "hostPlayerId": "guid (required, non-empty)",
  "playlistId": "guid (required, non-empty)",
  "roundDurationSeconds": 10  // optional, range 5-15, default 10
}
```

**Response 200:**
```json
{
  "sessionId": "guid",
  "lobbyId": "guid",
  "playlistId": "guid",
  "hostPlayerId": "guid",
  "startedAt": "datetime",
  "lobbyStatus": "InGame",
  "roundCount": 5
}
```

**Errors:** 400 if playlist is empty, an active session already exists, or lobby is not in `Waiting` status. 404 if playlist not found for this host.

---

#### GET /api/sessions/{sessionId}

Get current session state including current round and leaderboard. Automatically transitions playing rounds to revealed when their timer expires.

**Response 200:**
```json
{
  "sessionId": "guid",
  "lobbyId": "guid",
  "lobbyStatus": "InGame | Waiting | Finished",
  "startedAt": "datetime",
  "endedAt": "datetime | null",
  "isFinished": false,
  "totalRounds": 5,
  "completedRounds": 2,
  "roundDurationSeconds": 10,
  "currentRound": {
    "roundId": "guid",
    "roundNo": 3,
    "state": "Playing | Revealed",
    "previewUrl": "string (audio URL)",
    "startedAt": "datetime",
    "endsAt": "datetime",
    "revealedAt": "datetime | null",
    "answerTitle": "string | null (shown when Revealed/Finished)",
    "answerArtist": "string | null (shown when Revealed/Finished)",
    "artworkUrl": "string | null (shown when Revealed/Finished)",
    "itunesUrl": "string | null (shown when Revealed/Finished)",
    "answerTitleMask": "string (e.g. '••••• ••••')",
    "answerArtistMask": "string",
    "correctAnswerCount": 2,
    "correctPlayerIds": ["guid"],
    "correctTitlePlayerIds": ["guid"],
    "correctArtistPlayerIds": ["guid"]
  },
  "leaderboard": [
    {
      "playerId": "guid",
      "nick": "string",
      "score": 150,
      "correctAnswers": 3
    }
  ]
}
```

---

#### POST /api/sessions/{sessionId}/answer

Submit a guess for the current round. Rate limited: 5 requests per 10 seconds per IP.

**Request body:**
```json
{
  "playerId": "guid (required, non-empty)",
  "answer": "string (1-200 chars, required)"
}
```

**Response 200:**
```json
{
  "accepted": true,
  "isCorrect": true,
  "titleCorrect": true,
  "artistCorrect": false,
  "alreadyAnswered": false,
  "pointsAwarded": 50,
  "message": "string"
}
```

---

#### POST /api/sessions/{sessionId}/advance

Advance the session: skip/reveal the current round, or move to the next round. Host only.

**Request body:**
```json
{
  "hostPlayerId": "guid (required, non-empty)"
}
```

**Response 200:** Returns `GetSessionStateResponse` (same as GET /api/sessions/{sessionId}).

**Errors:** 403 if caller is not the host.

---

#### POST /api/sessions/{sessionId}/return-to-lobby

End the finished session and return all players to the lobby. Creates a new empty playlist. Host only.

**Request body:**
```json
{
  "hostPlayerId": "guid (required, non-empty)"
}
```

**Response 200:**
```json
{
  "lobbyId": "guid",
  "lobbyCode": "string",
  "playlistId": "guid (new empty playlist)",
  "lobbyStatus": "Waiting"
}
```

**Errors:** 400 if session is still in progress. 403 if caller is not the host.

---

## Health Checks

| Endpoint | Description |
|----------|-------------|
| GET /health/live | Liveness probe (always healthy) |
| GET /health/ready | Readiness probe (checks database connectivity) |

---

## SignalR Hub

**Endpoint:** `/hubs/game`

### Client-to-Server Methods (invoked by client)

| Method | Parameters | Description |
|--------|------------|-------------|
| `JoinLobby` | `lobbyCode: string` | Join lobby group for real-time updates. Code must match `[A-Z0-9]{4,8}`. |
| `LeaveLobby` | `lobbyCode: string` | Leave lobby group. |
| `JoinSession` | `sessionId: string` | Join session group for game updates. |
| `LeaveSession` | `sessionId: string` | Leave session group. |

### Server-to-Client Events (received by client)

| Event | Payload | Group | Triggered when |
|-------|---------|-------|----------------|
| `LobbyUpdated` | _(none)_ | `lobby:{code}` | A player joins or leaves the lobby. |
| `SessionStarted` | `{ sessionId: "guid" }` | `lobby:{code}` | Host starts a new game session. |
| `SessionUpdated` | _(none)_ | `session:{id}` | Round state changes (round revealed, advanced). |
| `PlayerAnsweredCorrectly` | `{ playerId: "guid", nick: "string", points: int }` | `session:{id}` | A player submits a correct answer. |
| `ReturnToLobby` | `{ lobbyCode: "string", playlistId: "guid" }` | `session:{id}` | Host returns all players to the lobby after game ends. |

---

## Enum Contract Values

These string values are used in API responses and are guaranteed stable:

**Lobby Status:** `Waiting`, `InGame`, `Finished`

**Round State:** `Pending`, `Playing`, `Revealed`, `Finished`

---

## Rate Limits

| Policy | Limit | Window | Applied to |
|--------|-------|--------|------------|
| `submit-answer` | 5 requests | 10 seconds | POST /api/sessions/{id}/answer |
| `itunes-search` | 5 requests | 10 seconds | GET /api/itunes/search |
| `create-lobby` | 3 requests | 30 seconds | POST /api/lobbies |

Rate limit is partitioned by client IP address.
