 # Woah — Real-Time Multiplayer Music Guessing Game

  > Compete with friends to identify songs from audio previews. The faster you guess, the more points you earn.

  ![Gameplay Demo](docs/screenshots/gameplay-demo.gif)
  <!-- TODO: Record a 30-second GIF showing: lobby creation → song playing → guessing → leaderboard -->

  ![Lobby View](docs/screenshots/lobby.png)
  <!-- TODO: Screenshot of lobby with players and playlist panel -->

  ![Round Summary](docs/screenshots/round-summary.png)
  <!-- TODO: Screenshot of revealed round with artwork and scores -->

  ---

  ## What is this?

  Woah is a party game where players join a lobby, each add up to 10 songs, then compete to identify randomized tracks from 30-second audio previews. Points
   are awarded based on speed — guess the title in 2 seconds and score 80 points; wait 9 seconds and score 10.

  The twist: **you can't guess your own songs**. Instead, if someone else correctly identifies a song you added, you earn a 75-point bonus. This
  incentivizes adding recognizable tracks rather than obscure ones.

  ---

  ## Tech Stack

  | Layer | Technology |
  |-------|-----------|
  | **Frontend** | React 18, Vite, SignalR Client |
  | **Backend** | ASP.NET Core 8, C# 12 |
  | **Real-time** | SignalR (WebSocket + Long Polling fallback) |
  | **Database** | PostgreSQL 16 + Entity Framework Core 8 |
  | **Music Data** | iTunes Search API (30-second previews) |
  | **Infrastructure** | Docker (multi-stage Alpine builds), Health Checks |

  ---

  ## Key Features

  - **Real-time multiplayer** — SignalR pushes instant updates; 10-second polling fallback ensures reliability
  - **Smart answer matching** — Unicode normalization, diacritics removal, leet-speak handling (`Beyoncé` = `beyonce` = `b3yonc3`)
  - **Anti-cheat mechanics** — Players cannot guess songs they added; guessing input is hidden server-side and client-side
  - **Track owner bonus** — 75 points awarded to the song's contributor when another player correctly identifies the title
  - **Time-based scoring** — Linear decay from 100 to 1 points based on answer speed
  - **Session persistence** — Page refresh restores your game state (lobby, session, progress)
  - **Stale game cleanup** — Background service auto-closes abandoned lobbies (30 min) and sessions (15 min)
  - **Concurrency-safe** — Row-level locking on joins, serializable isolation on session start, optimistic versioning on answers

  ---

  ## Architecture & Design Decisions

  ### Why SignalR + Polling?
  WebSockets provide instant updates but can fail behind corporate proxies. The frontend subscribes via SignalR and automatically falls back to HTTP polling
   (10s interval) when the connection drops. The `usePolling` hook disables polling when SignalR is connected, preventing redundant requests.

  ### Why PostgreSQL Row-Level Locking?
  Lobby joins are a classic race condition — two players joining simultaneously could exceed `MaxPlayers`. Instead of application-level mutexes, we use
  `SELECT ... FOR UPDATE` to lock the lobby row during the join transaction. This is database-native, deadlock-safe, and works across multiple API
  instances.

  ### Why Optimistic Concurrency on Answers?
  Multiple players submit answers simultaneously. Rather than serializing all writes, each `RoundCorrectAnswerEntity` uses PostgreSQL's `xmin` system column
   as a row version. On conflict, the handler retries up to 2 times. This maximizes throughput while preventing double-scoring.

  ### Why Answer Normalization?
  iTunes metadata contains accented characters, parenthetical info, and inconsistent formatting. The normalization pipeline (NFD decomposition → diacritics
  removal → leet-speak mapping → whitespace collapse) ensures `"Héllo Wörld (feat. Someone)"` matches a guess of `"hello world"`.

  Client (React)
    │
    ├── REST API (/api/*) ──────► ASP.NET Core Controllers
    │                                    │
    │                              Service Layer (DI)
    │                                    │
    │                              EF Core ──► PostgreSQL
    │
    └── SignalR (/hubs/game) ───► GameHub (group-based broadcast)

  ---

  ## How to Run Locally

  ### Prerequisites
  - [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
  - [Node.js 20+](https://nodejs.org/)
  - [PostgreSQL 16](https://www.postgresql.org/download/) (or Docker)

  ### 1. Database

  ```bash
  # Option A: Docker
  docker run -d --name woah-db \
    -e POSTGRES_USER=woah_user \
    -e POSTGRES_PASSWORD=woah_pass \
    -e POSTGRES_DB=woah \
    -p 5432:5432 postgres:16-alpine

  # Option B: Local PostgreSQL — create database "woah"

  2. Backend

  cd backend/src/Woah.Api

  # Configure connection string
  dotnet user-secrets set "ConnectionStrings:WoahDb" \
    "Host=localhost;Port=5432;Database=woah;Username=woah_user;Password=woah_pass"

  dotnet run
  # → API running on http://localhost:5234
  # → Swagger UI at http://localhost:5234/swagger

  3. Frontend

  cd frontend
  npm install
  npm run dev
  # → App running on http://localhost:5173 (proxies API to :5234)

  4. Docker (Full Stack)

  docker build -t woah .
  docker run -d -p 8080:8080 \
    -e ConnectionStrings__WoahDb="Host=host.docker.internal;Port=5432;Database=woah;Username=woah_user;Password=woah_pass" \
    woah
  # → App running on http://localhost:8080

  ---
  API Documentation

  See API_DOCUMENTATION.md for the full REST + SignalR reference.

  ---
  License

  This project is for educational and portfolio purposes. Music previews are provided by the iTunes Search API under Apple's terms of service.

  ---