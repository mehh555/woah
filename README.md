<p align="center">
  <h1 align="center">🎵 WOAH</h1>
  <p align="center">
    <strong>Real-time multiplayer music guessing game built for high concurrency and robust state management.</strong>
  </p>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET_8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 8" />
  <img src="https://img.shields.io/badge/React_18-20232A?style=for-the-badge&logo=react&logoColor=61DAFB" alt="React 18" />
  <img src="https://img.shields.io/badge/PostgreSQL_16-316192?style=for-the-badge&logo=postgresql&logoColor=white" alt="PostgreSQL 16" />
  <img src="https://img.shields.io/badge/SignalR-0078D4?style=for-the-badge&logo=microsoft&logoColor=white" alt="SignalR" />
  <img src="https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white" alt="Docker" />
</p>

<p align="center">
  <a href="https://woah-mvz6.onrender.com/">
    <img src="https://img.shields.io/badge/🚀_Play_Live_Demo-00E5A0?style=for-the-badge&logoColor=white" alt="Live Demo" />
  </a>
</p>

> **⚠️ Live Demo Notice:** The provided link is a proof-of-concept deployment running on Render's free tier. Due to the platform's strict CPU/memory throttling and shared resources, you may experience noticeable latency during gameplay or WebSocket synchronization. 

---

## 📖 Overview

Woah is a real-time web application where players create lobbies, collaboratively build a playlist using the iTunes Search API, and compete to guess playing tracks. Built with a focus on **data integrity under concurrent load**, sophisticated **text normalization**, and **resilient WebSocket communication**.

---
**Project Origins:** *Woah* was created simply as a fun multiplayer game to host locally and play with friends — and that remains its primary purpose. We still host it on our own machines for game nights. The live version currently deployed on Render is strictly a public demo so others can see how it works without setting it up themselves.
---

## 🚀 Quick Start (Local Development)

The easiest way to run the application is via Docker Compose.

```bash
# 1. Clone the repository
git clone https://github.com/mehh/woah.git
cd woah

# 2. Start the containers
docker compose up --build

# 3. Open your browser
# The application will be running at http://localhost:5173
```

### Without Docker

```bash
# 1. Backend
cd backend/src/Woah.Api
dotnet user-secrets set "ConnectionStrings:WoahDb" \
  "Host=localhost;Port=5432;Database=woah;Username=woah_user;Password=woah_pass"
dotnet run
# → http://localhost:5234 | Swagger at /swagger (dev only)

# 2. Frontend (separate terminal)
cd frontend
npm install && npm run dev
# → http://localhost:5173 (proxies /api and /hubs to backend)
```

> **Tip:** The Vite dev server proxies both REST (`/api`) and WebSocket (`/hubs`) traffic to the backend — no CORS configuration needed in development.

---

## 🔐 Environment Variables

For manual deployment or Docker configuration, the following variables are required:

| Variable | Description | Required | Default |
|----------|-------------|----------|---------|
| `ConnectionStrings__WoahDb` | PostgreSQL connection string | **Yes** | — |
| `ASPNETCORE_ENVIRONMENT` | Environment toggle (`Production`, `Development`) | No | `Production` |
| `AllowedCorsOrigins__0` | Allowed CORS origin | No | `http://localhost:5173` |
| `Itunes__Market` | Regional market for the iTunes API | No | `US` |

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

## 📄 Disclaimer

This is an educational portfolio project. Audio previews are sourced from the iTunes Search API.
