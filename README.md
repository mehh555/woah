# woah
## Założenia (MVP)
- **Preview-only**: gra korzysta wyłącznie z krótkich preview utworów (docelowo odtwarzamy tylko 5–10 sekund).
- **Brak logowania do Spotify dla graczy**: gracze nie muszą łączyć własnego konta Spotify.
- **Lobby multiplayer (1–10 osób)**:
  - host tworzy lobby i zaprasza graczy kodem/linkiem,
  - host może kickować graczy, startować grę i startować rundy,
  - serwer jest **authoritative**: wybiera utwór, zarządza timerem i nalicza punkty.
- **Reconnect**: po odświeżeniu strony gracz powinien móc wrócić do aktualnego stanu lobby/gry (rejoin do ostatniego stanu).
- **Playlisty / utwory**:
  - na wejściu weryfikujemy, czy dany utwór ma `preview_url`,
  - utwory bez preview są odrzucane i komunikowane użytkownikowi.
  - import playlist Spotify / inne tryby źródła utworów doprecyzujemy później (na MVP dopuszczamy prostszy input).
- **Gość vs konto**:
  - gość: identyfikator + nick,
  - konto (na MVP minimalnie): umożliwia hostowanie i zarządzanie playlistami w aplikacji,
  - na MVP nie robimy rankingów/statystyk/historii gier.
## Tech stack (na teraz)
- **Backend:** .NET 10/chyba ze nie masz kurwa takiego / ASP.NET Core Web API
- **Realtime:** SignalR (lobby, start rund, timery, stan gry, reconnect)
- **DB:** PostgreSQL (docelowo; warstwa persystencji będzie oparta o EF Core)
- **Repo:** monorepo (`backend/` + `frontend/`)
## Struktura repozytorium
- `backend/` – rozwiązanie .NET
  - `src/Woah.Api` – API + realtime
  - `tests/` – testy
- `frontend/` – UI (do uzupełnienia)
## Roadmap 
1. Lobby: create/join/leave, lista graczy, host controls
2. Reconnect: odtworzenie stanu po refresh
3. Runda: start/stop, timer, scoring, reveal
4. Walidacja preview: track lookup + odrzucanie utworów bez preview
5. PostgreSQL + EF Core + migracje
6. Frontend UI