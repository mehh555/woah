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

Gracze dołączają do lobby, każdy dodaje utwory z iTunes, a gra odtwarza 30-sekundowe fragmenty. Wpisz tytuł lub artystę zanim skończy się czas — szybciej = więcej punktów. Nie możesz zgadnąć własnej piosenki, ale zdobywasz bonus, gdy ktoś inny zgadnie Twój utwór.

![Gameplay Preview](docs/gameplay.gif)
![Lobby Preview](docs/lobby.gif)

---

## 🎮 Funkcje rozgrywki

- **System Lobby** — Twórz lub dołączaj przy użyciu 6-znakowego kodu. Host kontroluje playlistę i czas trwania rundy (5–25s).  
- **Utwory dodawane przez graczy** — Każdy wyszukuje utwory w iTunes i dodaje je do wspólnej playlisty. Gra losowo odtwarza utwory.  
- **Live Scoring** — Punkty maleją liniowo od 100 do 1 w miarę upływu czasu. Zgadywanie tylko artysty daje połowę punktów.  
- **Mechanika właściciela utworu** — Nie możesz zgadywać własnego utworu. Gdy inny gracz zgadnie Twój utwór, otrzymujesz bonus 75 punktów, przyznawany tylko raz na rundę.  
- **Punkty częściowe** — Tytuł i artysta oceniane niezależnie, możesz zdobyć jeden teraz, a drugi później w tej samej rundzie.  
- **Regulacja głośności** — Suwak w grze z zapamiętaną preferencją (`localStorage`). Możliwa zmiana w trakcie rundy.  
- **GPU-accelerated Timer** — Pasek postępu działa na pojedynczej animacji CSS, bez obciążania JS.  
- **Aktualizacje w czasie rzeczywistym** — Dołączenia do lobby, zmiany rund, wyników i stanu gry przesyłane natychmiast przez WebSocket (SignalR).  
- **Utrzymanie sesji** — Odświeżenie strony przywraca dokładny stan gry (lobby, sesja, postęp).  
- **Sprzątanie nieaktywnych gier** — Usługa w tle automatycznie zamyka opuszczone lobby (30 min) i sesje (15 min).

---

## ⚙️ Inżynieria i architektura

### Kontrola współbieżności — 3 poziomy

| Poziom | Mechanizm | Gdzie | Dlaczego |
|--------|-----------|-------|----------|
| **Pesymistyczny** | `SELECT ... FOR UPDATE` | Dołączanie do lobby | Zapobiega jednoczesnemu zajęciu ostatniego miejsca |
| **Serializable** | `IsolationLevel.Serializable` | Tworzenie sesji | Gwarantuje dokładnie jedną aktywną sesję na lobby |
| **Optymistyczny** | PostgreSQL `xmin` row versioning + retry | Przesyłanie odpowiedzi | Obsługuje współbieżne zapisy bez blokowania całej rundy |

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
Silnik normalizacji tekstu

Normalizacja odpowiada za dopasowanie wpisów typu "naïve", "NAIVE", "n@1ve", "ñaive" do tytułu lub artysty, usuwa diakrytyki i normalizuje format.

Pipeline:

Mapowanie znaków — Leet-speak ($→s, @→a, 3→e, 0→o) i specjalne (ł→l, ß→ss, ø→o)
Małe litery — ToLowerInvariant()
Decompozycja NFD — rozdzielenie bazowego znaku + kombinujących
Usuwanie diakrytyków — UnicodeCategory.NonSpacingMark
Rekompozycja NFC — złożenie znaków, redukcja spacji
Bezpieczeństwo i produkcja
Problem	Rozwiązanie
Brute-force kodów lobby	Limit żądań JoinLobby 10 req/60s/IP
Spam odpowiedzi	SubmitAnswer capped 5 req/10s/IP
Ekspozycja Swagger	Tylko w IsDevelopment()
Auto-migracje w prod	Tylko manualnie w prod
Wycieki danych	dotnet user-secrets lokalnie, zmienne środowiskowe w prod
Race condition bonusów	Obsługa w try/catch (DbUpdateException)
Komunikacja w czasie rzeczywistym

SignalR WebSocket + fallback long-polling. Kanały grupowe:

lobby:{code} — dołączenia, zmiany playlisty, start sesji
session:{id} — zmiany rund, aktualizacja wyników, powiadomienia o poprawnych odpowiedziach

Frontend używa hooka useSignalRGroup z wrapperami useLobbySubscription i useSessionSubscription.

🏗️ Architektura systemu
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
☁️ Chmura i wdrożenie
Komponent	Platforma	Szczegóły
Frontend	Vercel	SPA z Vite, CDN, SSL
Backend	Render	Docker .NET 8, multi-stage Alpine (~80MB), health checks
Baza	Render	PostgreSQL 16, connection string z env

Keep-Alive: cron co 14 min pingujący /health/live → SignalR nie traci połączeń.

🚀 Uruchamianie lokalne

Docker Compose (zalecane):

git clone https://github.com/mehh/woah.git && cd woah
docker compose up --build
# http://localhost:5173

Bez Dockera:

cd backend/src/Woah.Api
dotnet user-secrets set "ConnectionStrings:WoahDb" "Host=localhost;Port=5432;Database=woah;Username=woah_user;Password=woah_pass"
dotnet run
# http://localhost:5234

cd frontend
npm install && npm run dev
# http://localhost:5173
🔐 Zmienne środowiskowe

Backend:

ConnectionStrings__WoahDb — wymagane
AllowedCorsOrigins__0 — domyślnie http://localhost:5173
ASPNETCORE_ENVIRONMENT — domyślnie Production
Itunes__Market — domyślnie US

Frontend:

VITE_API_URL — domyślnie /api
📁 Struktura projektu
woah/
├── backend/
│   └── src/Woah.Api/
│       ├── Contracts/
│       ├── Controllers/
│       ├── Domain/
│       ├── Hubs/
│       ├── Infrastructure/Persistence/
│       ├── Integrations/Itunes/
│       ├── Middleware/
│       └── Services/
├── frontend/src/
│   ├── api/
│   ├── components/
│   ├── context/
│   ├── hooks/
│   └── screens/
├── docker-compose.yml
└── Dockerfile
📄 Licencja

Projekt edukacyjny / portfolio. Fragmenty muzyczne pochodzą z iTunes Search API zgodnie z regulaminem Apple.
