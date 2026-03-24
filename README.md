<p align="center">
  <h1 align="center">🎵 WOAH</h1>
  <p align="center">
    <strong>Real-time multiplayer music guessing — built for concurrency, not just correctness.</strong>
  </p>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
  <img src="https://img.shields.io/badge/React-18-61DAFB?style=for-the-badge&logo=react&logoColor=black" />
  <img src="https://img.shields.io/badge/PostgreSQL-16-4169E1?style=for-the-badge&logo=postgresql&logoColor=white" />
  <img src="https://img.shields.io/badge/SignalR-WebSockets-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
  <img src="https://img.shields.io/badge/Docker-Compose-2496ED?style=for-the-badge&logo=docker&logoColor=white" />
  <img src="https://img.shields.io/badge/Vite-6-646CFF?style=for-the-badge&logo=vite&logoColor=white" />
</p>

<p align="center">
  <a href="https://woah-mvz6.onrender.com/">
    <img src="https://img.shields.io/badge/🚀_Play_Live_Demo-00E5A0?style=for-the-badge&logoColor=white" />
  </a>
</p>

---

Players join a lobby, each submits tracks from iTunes, and the game plays 30-second previews. Type the song title or artist before time runs out — faster answers earn more points. Tracks are sourced from the players themselves, which opens up a whole layer of strategy: you can't guess your own song, but you earn bonus points when someone else nails yours.

The interesting engineering isn't the game loop — it's what happens when 10 players submit answers to the same round within the same 200ms window.

![Gameplay Preview](docs/gameplay.gif)
![Lobby Preview](docs/lobby.gif)

---

## 🎮 Gameplay Features

- **Lobby System** — Create or join with a 6-character code. Host controls playlist and round duration (5–25s).
- **Player-Sourced Tracks** — Each player searches iTunes and adds songs to the shared playlist. The game shuffles and plays them back.
- **Live Scoring** — Points decay linearly from 100 → 1 as the round timer ticks down. Artist-only guesses award half points.
- **Track Owner Mechanic** — You can't guess your own song (input silently disabled). When another player guesses your track's title correctly, you earn a 75-point bonus — awarded exactly once per round with race-condition protection.
- **Partial Credit** — Title and artist are evaluated independently. Get one now, get the other later in the same round.
- **Volume Control** — In-game slider with persisted preference (`localStorage`). Adjustable mid-round without interruption.
- **GPU-accelerated Timer** — Progress bar runs on a single CSS transition instead of per-frame JS updates — smooth animation with zero layout thrashing.
- **Real-time Updates** — Lobby joins, round transitions, score changes, and game state push instantly via WebSockets.
- **Session Persistence** — Page refresh restores your exact game state (lobby, session, progress).
- **Stale Game Cleanup** — Background hosted service auto-closes abandoned lobbies (30 min) and sessions (15 min).

---

## ⚙️ Engineering & Architecture

### Concurrency Control — 3 Tiers

This was the hardest part of the project. A music guessing game sounds simple until multiple players answer simultaneously and your database starts handing out duplicate points.

| Tier | Mechanism | Where | Why |
|------|-----------|-------|-----|
| **Pessimistic** | `SELECT ... FOR UPDATE` | Lobby joins | Prevents two players from taking the last slot simultaneously |
| **Serializable** | `IsolationLevel.Serializable` | Session creation | Guarantees exactly one active session per lobby — no phantom reads |
| **Optimistic** | PostgreSQL `xmin` row versioning + retry loop | Answer submission | Handles concurrent answer writes without locking the entire round |

The answer submission path deserves a closer look. When a player submits, the handler:
1. Reads the player's existing `RoundCorrectAnswer` row (if any)
2. Calculates new points based on what they haven't already guessed
3. Attempts to write — if `xmin` has changed (another concurrent write), it reloads and retries up to 2 times
4. Falls back to `DbUpdateException` catch for true duplicate inserts

```csharp
for (var attempt = 0; ; attempt++)
{
    var existing = round.CorrectAnswers.FirstOrDefault(x => x.PlayerId == request.PlayerId);
    // ... calculate points ...
    try
    {
        await _dbContext.SaveChangesAsync(ct);
        break;
    }
    catch (DbUpdateConcurrencyException) when (attempt < GameConstants.MaxConcurrencyRetries)
    {
        if (existing is not null)
            await _dbContext.Entry(existing).ReloadAsync(ct);
        continue;
    }
}
```

### Text Normalization Engine

Answer checking in a music game is deceptively hard. Players type `"naïve"`, `"NAIVE"`, `"n@1ve"`, or `"ñaive"` — and all should match.

The normalizer runs a **5-stage pipeline**:

1. **Character mapping** — Leet-speak (`$→s`, `@→a`, `3→e`, `0→o`) and special characters (`ł→l`, `ß→ss`, `ø→o`)
2. **Lowercasing** — `ToLowerInvariant()`
3. **NFD decomposition** — Unicode Normalization Form D splits characters into base + combining marks
4. **Diacritic stripping** — Filters `UnicodeCategory.NonSpacingMark` (the combining marks from step 3)
5. **NFC recomposition** — Normalizes back to composed form, collapses whitespace

The evaluator then checks three match paths: full match (`"artist title"` or `"title artist"`), title-only, and artist-only — all against pre-normalized stored values using ordinal comparison.

### Security & Production Hardening

| Concern | Solution |
|---------|----------|
| **Brute-force lobby codes** | Fixed-window rate limiting on `JoinLobby` — 10 req / 60s per IP |
| **Answer spam** | `SubmitAnswer` capped at 5 req / 10s per IP |
| **Swagger exposure** | Gated behind `IsDevelopment()` — never served in production |
| **Auto-migration in prod** | Gated behind `IsDevelopment()` — production requires explicit migration runs |
| **Credential leaks** | `dotnet user-secrets` locally, environment variables in production. No secrets in repo. |
| **Race conditions on bonus** | Track owner bonus wrapped in `try/catch (DbUpdateException)` — concurrent inserts resolve silently |

All rate-limit violations return `429` with a standards-compliant `application/problem+json` body.

### Real-time Communication

SignalR WebSocket hub with automatic long-polling fallback. Clients subscribe to group-scoped channels:

- `lobby:{code}` — Player joins/leaves, playlist changes, session start signals
- `session:{id}` — Round transitions, score updates, correct answer notifications

Frontend uses a generic `useSignalRGroup` hook that handles connection lifecycle, group join/leave, and handler registration — with `useLobbySubscription` and `useSessionSubscription` as thin wrappers.

> When SignalR is connected, HTTP polling is automatically disabled to prevent redundant requests. When the WebSocket drops, polling re-enables at a 10-second interval.


## 🏗️ System Architecture

```
┌─────────────┐         WebSocket (SignalR)          ┌──────────────────┐
│             │ ◄──────────────────────────────────── │                  │
│   React 18  │                                      │   .NET 8 API     │
│   (Vite)    │ ────── REST (JSON) ────────────────► │                  │
│             │                                      │  Controllers     │
└──────┬──────┘                                      │    ↓             │
       │                                             │  Services (DI)   │
  Vercel CDN                                         │    ↓             │
                                                     │  EF Core         │
                                                     │    ↓             │
                                                     └───────┬──────────┘
                                                             │  Render
                                                     ┌───────▼──────────┐
                                                     │  PostgreSQL 16   │
                                                     │                  │
                                                     │  xmin versioning │
                                                     │  FOR UPDATE      │
                                                     │  SERIALIZABLE    │
                                                     └──────────────────┘
```

**Request flow:** React (Vercel CDN) → REST or SignalR → Controller → Service layer → EF Core → PostgreSQL (Render). All business logic lives in the service layer. Controllers are thin dispatchers. Persistence models are separate from domain contracts — no entity leaks into API responses.

---

## ☁️ Cloud Infrastructure & Deployment

| Component | Platform | Details |
|-----------|----------|---------|
| **Frontend** | Vercel | Static SPA deployed from `frontend/` with Vite build. CDN-backed, zero-config SSL. |
| **Backend** | Render | Dockerized .NET 8 API. Multi-stage Alpine build (~80MB image). Health checks on `/health/live` and `/health/ready`. |
| **Database** | Render | Managed PostgreSQL 16. Connection string injected via environment variable. |

### Keep-Alive Architecture

Render's free tier spins down idle services after 15 minutes — fatal for a WebSocket-dependent game. The solution:

- A **cron job** hits the `/health/live` endpoint every 14 minutes
- The health check is lightweight (no DB hit — just returns `200 OK`)
- A separate `/health/ready` endpoint validates the DB connection for deployment readiness

This keeps the container warm, SignalR connections alive, and cold-start latency out of the player experience.

---

## 🚀 Getting Started

### With Docker Compose (Recommended)

```bash
# 1. Clone
git clone https://github.com/mehh/woah.git && cd woah

# 2. Start everything
docker compose up --build

# 3. Open http://localhost:5173
```

That's it. The database creates itself, migrations run automatically, and both services start.

### Without Docker

```bash
# 1. Backend
cd backend/src/Woah.Api
dotnet user-secrets set "ConnectionStrings:WoahDb" \
  "Host=localhost;Port=5432;Database=woah;Username=woah_user;Password=woah_pass"
dotnet run
# → http://localhost:5234 | Swagger at /swagger

# 2. Frontend (separate terminal)
cd frontend
npm install && npm run dev
# → http://localhost:5173 (proxies /api and /hubs to backend)
```

> **Tip:** The Vite dev server proxies both REST (`/api`) and WebSocket (`/hubs`) traffic to the backend — no CORS config needed in development.

---

## 🔐 Environment Variables

### Backend

| Variable | Description | Required | Default |
|----------|-------------|----------|---------|
| `ConnectionStrings__WoahDb` | PostgreSQL connection string | **Yes** | — |
| `AllowedCorsOrigins__0` | Allowed CORS origin | No | `http://localhost:5173` |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | No | `Production` |
| `Itunes__Market` | iTunes Store region for search results | No | `US` |

> Double underscores (`__`) map to `:` in .NET configuration hierarchy. For local dev, prefer `dotnet user-secrets` over `.env` files to keep credentials out of source control.

### Frontend

| Variable | Description | Default |
|----------|-------------|---------|
| `VITE_API_URL` | Backend API base URL | `/api` (proxied in dev) |

---

## 📁 Project Structure

```
woah/
├── backend/
│   └── src/Woah.Api/
│       ├── Contracts/              # Request/response DTOs
│       ├── Controllers/            # Thin API endpoints
│       ├── Domain/                 # GameConstants, enums, value objects
│       ├── Hubs/                   # SignalR GameHub
│       ├── Infrastructure/
│       │   └── Persistence/        # EF Core context, entity models, migrations
│       ├── Integrations/Itunes/    # iTunes search API client
│       ├── Middleware/             # Global exception handler, rate limiting
│       └── Services/
│           ├── Cleanup/            # Background stale-game cleanup
│           ├── Lobby/              # Lobby CRUD, code generation
│           ├── Notifications/      # SignalR push layer
│           ├── Playlist/           # Track search & management
│           └── Session/            # Game engine, scoring, answer normalization
├── frontend/
│   └── src/
│       ├── api/                    # REST client + SignalR connection
│       ├── components/             # ErrorBoundary, PlaylistPanel, Timer
│       ├── context/                # SessionContext, GameHubContext
│       ├── hooks/                  # usePolling, useSignalRGroup, subscriptions
│       └── screens/               # StartScreen, LobbyScreen, GameScreen
├── docker-compose.yml
└── Dockerfile                      # Multi-stage: frontend build → backend publish → Alpine runtime
```

---

## 📄 License

This project is for educational and portfolio purposes. Music previews are provided by the iTunes Search API under Apple's terms of service.
