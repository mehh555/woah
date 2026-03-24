<p align="center">
  <h1 align="center">🎵 WOAH</h1>
  <p align="center">
    <strong>Real-time multiplayer music guessing — built for concurrency, not just correctness.</strong>
  </p>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 8" />
  <img src="https://img.shields.io/badge/React-18-61DAFB?style=for-the-badge&logo=react&logoColor=black" alt="React 18" />
  <img src="https://img.shields.io/badge/PostgreSQL-16-4169E1?style=for-the-badge&logo=postgresql&logoColor=white" alt="PostgreSQL 16" />
  <img src="https://img.shields.io/badge/SignalR-WebSockets-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="SignalR" />
  <img src="https://img.shields.io/badge/Docker-Compose-2496ED?style=for-the-badge&logo=docker&logoColor=white" alt="Docker" />
  <img src="https://img.shields.io/badge/Vite-6-646CFF?style=for-the-badge&logo=vite&logoColor=white" alt="Vite" />
</p>

<p align="center">
  <a href="https://woah-mvz6.onrender.com/">
    <img src="https://img.shields.io/badge/🚀_Play_Live_Demo-00E5A0?style=for-the-badge&logoColor=white" alt="Live Demo" />
  </a>
</p>

---

Players join a lobby, curate a shared playlist using the iTunes API, and compete in real-time to guess the currently playing track. Type the title or artist before the 30-second timer runs out — faster answers yield more points. You cannot guess your own submitted tracks, but you are rewarded with a bonus when others guess them correctly.

![Gameplay Preview](docs/gameplay.gif)
![Lobby Preview](docs/lobby.gif)

---

## 🎮 Gameplay Features

* **Lobby System:** Create or join using a secure 6-character code. The host controls the playlist and round duration (5–25s).
* **Player-Curated Playlists:** Search and add songs via the iTunes API. The game randomizes playback order.
* **Live Scoring:** Points decay linearly from 100 to 1 as the timer drops. Guessing only the artist awards half points.
* **Track Owner Mechanics:** Anti-cheat prevents guessing your own track. You receive a one-time 75-point bonus when another player successfully guesses your submission.
* **Partial Points:** Title and artist are evaluated independently. You can secure points for one and guess the other later in the same round.
* **GPU-Accelerated Timer:** The progress bar utilizes a single CSS animation, eliminating JavaScript overhead and re-renders.
* **Real-Time Sync:** Lobby joins, round transitions, and scoring are pushed instantly via SignalR WebSockets.
* **Session Resilience:** Refreshing the page (F5) flawlessly restores the exact game state, lobby data, and round progress.

---

## ⚙️ Engineering & Architecture

### Concurrency Control (3-Tier System)

| Level | Mechanism | Scope | Purpose |
| :--- | :--- | :--- | :--- |
| **Pessimistic** | `SELECT ... FOR UPDATE` | Lobby Joins | Prevents race conditions when multiple players claim the last available slot. |
| **Serializable** | `IsolationLevel.Serializable` | Session Initialization | Guarantees exactly one active session is created per lobby. |
| **Optimistic** | PostgreSQL `xmin` + Retry Loop | Answer Submissions | Handles highly concurrent write bursts without locking the entire round table. |

```csharp
for (var attempt = 0; ; attempt++)
{
    var existing = round.CorrectAnswers.FirstOrDefault(x => x.PlayerId == request.PlayerId);
    // ... point calculation logic ...
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
