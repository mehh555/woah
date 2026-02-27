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

7. ) Minimalne encje i relacje
players
Identyfikacja gracza (guest lub konto). Nick jest per lobby.
player_id UUID PK


created_at timestamp


Opcjonalnie później:
user_id (jeśli wdrożycie konta)



lobbies
Pokój (lobby).
lobby_id UUID PK


code varchar UNIQUE


status varchar (np. waiting, playing, finished)


created_at timestamp


host_player_id UUID FK → players.player_id


max_players smallint (np. 10)



lobby_players
Uczestnicy w lobby + nick per lobby.
lobby_id UUID FK → lobbies.lobby_id


player_id UUID FK → players.player_id


nick varchar


joined_at timestamp


left_at timestamp null


Klucze:
PK: (lobby_id, player_id)


UNIQUE: (lobby_id, nick)



2) Playlisty jako “tabela JSON-ów”
playlists
Playlista w aplikacji (stworzona ręcznie lub importowana później).
playlist_id UUID PK


owner_player_id UUID FK → players.player_id (albo owner_user_id, jeśli będzie user system)


name varchar


market varchar default PL


created_at timestamp



playlist_tracks
Tu jest dokładnie to, co opisałeś: “tablica rekordów, każdy rekord to osobny JSON”.
playlist_id UUID FK → playlists.playlist_id


item_no int (kolejność w playliście)


track_json jsonb NOT NULL


title text NOT NULL


preview_url text NULL


spotify_track_id varchar NULL


spotify_url text NULL


is_valid bool NOT NULL default true


invalid_reason text NULL


created_at timestamp


Klucze i indeksy:
PK: (playlist_id, item_no)


indeks po (playlist_id)


opcjonalnie UNIQUE (playlist_id, spotify_track_id) jeśli nie chcesz duplikatów


Dlaczego mimo JSON trzymam też kolumny title/preview_url:
JSON jest super jako “surowy rekord”


ale title i preview_url będziesz czytał non stop, więc warto mieć je bez parsowania JSON


is_valid i invalid_reason pozwalają łatwo raportować “które odpadły bo brak preview”


Minimalny kształt track_json (przykład logiczny):
trackId, title, previewUrl, spotifyUrl, imageUrl, durationMs
 Ale nawet jak trzymasz więcej, to ok.



3) Gra i rundy
game_sessions
Jedna rozgrywka uruchomiona w lobby na bazie wybranej playlisty.
session_id UUID PK


lobby_id UUID FK → lobbies.lobby_id


playlist_id UUID FK → playlists.playlist_id


started_at timestamp


ended_at timestamp null


settings_json jsonb (np. previewSeconds, roundDurationSeconds, pointsMax, pointsMin)



rounds
Rundy jako “source of truth” dla aktualnej piosenki i odpowiedzi.
round_id UUID PK


session_id UUID FK → game_sessions.session_id


round_no int


playlist_id UUID FK → playlists.playlist_id


playlist_item_no int (wskazuje rekord w playlist_tracks)


preview_url text (snapshot na start rundy)


answer_title text (oryginalny tytuł)


answer_norm text (znormalizowany tytuł do matchowania)


started_at timestamp


ends_at timestamp


revealed_at timestamp null


state varchar (np. running, revealed, finished)


Ograniczenia:
UNIQUE (session_id, round_no)


Dlaczego snapshot preview_url/answer_* w rundzie:
playlista może się zmienić, a runda powinna być odtwarzalna i spójna


nie musisz w rundzie parsować JSON ponownie



4) Poprawne odpowiedzi (tylko to zapisujesz)
round_correct_answers
Jedna linia na gracza, jeśli trafił w tej rundzie.
round_id UUID FK → rounds.round_id


player_id UUID FK → players.player_id


answered_at timestamp


points int


Klucze:
PK: (round_id, player_id)
 To gwarantuje, że gracz “trafi” max raz na rundę.


Punktacja “ruchoma”:
liczysz na serwerze z answered_at - rounds.started_at


zapisujesz finalne points w tej tabeli



5) Co świadomie wywalamy (zgodnie z Twoimi założeniami)
brak attempt_count


brak tabeli z wszystkimi próbami (guess_events)


brak artysty w odpowiedzi i w walidacji (trzymasz tylko title)



6) Najważniejsze indeksy (żeby to nie muliło)
lobbies(code) UNIQUE


lobby_players(lobby_id) + UNIQUE nick w lobby


playlist_tracks(playlist_id)


rounds(session_id, round_no) UNIQUE


round_correct_answers(round_id) i round_correct_answers(player_id) (do scoreboardu)


