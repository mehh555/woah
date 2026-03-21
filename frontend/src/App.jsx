import { useEffect, useState } from "react";
import { getLobby, getSession } from "./api/client.js";
import { SessionProvider, useSession } from "./context/SessionContext.jsx";
import { GameHubProvider } from "./context/GameHubContext.jsx";
import StartScreen from "./screens/StartScreen.jsx";
import LobbyScreen from "./screens/LobbyScreen.jsx";
import GameScreen from "./screens/GameScreen.jsx";
import "./styles.css";
import "./screens.css";

function AppInner() {
    const { session, setSession, clearSession } = useSession();
    const [phase, setPhase] = useState("boot");

    useEffect(() => {
        let cancelled = false;

        async function resume() {
            if (!session?.playerId || !session?.lobbyCode) {
                setPhase("start");
                return;
            }

            setPhase("boot");

            try {
                const lobby = await getLobby(session.lobbyCode);
                if (cancelled) return;

                const isStillActive = (lobby.players || []).some(
                    player => player.playerId === session.playerId
                );

                if (!isStillActive) {
                    clearSession();
                    setPhase("start");
                    return;
                }

                const nextSession = {
                    ...session,
                    isHost: lobby.hostPlayerId === session.playerId,
                    sessionId: lobby.currentSessionId ?? session.sessionId ?? null,
                };

                setSession(nextSession);

                if (lobby.status === "Waiting") {
                    setPhase("lobby");
                    return;
                }

                if (nextSession.sessionId) {
                    setPhase("game");
                    return;
                }

                setPhase("lobby");
            } catch {
                if (session.sessionId) {
                    try {
                        const state = await getSession(session.sessionId);
                        if (cancelled) return;

                        const iAmParticipant = (state.leaderboard || []).some(
                            player => player.playerId === session.playerId
                        );

                        if (iAmParticipant) {
                            setPhase("game");
                            return;
                        }
                    } catch {
                        // fall through to reset
                    }
                }

                if (!cancelled) {
                    clearSession();
                    setPhase("start");
                }
            }
        }

        resume();

        return () => {
            cancelled = true;
        };
    }, [session?.playerId, session?.lobbyCode]);

    return (
        <div className="app-root">
            <div className="app-bg" />
            {phase === "boot" && (
                <div className="lobby-screen">
                    <div className="waiting-anim">Przywracam sesję…</div>
                </div>
            )}
            {phase === "start" && <StartScreen onEnter={() => setPhase("lobby")} />}
            {phase === "lobby" && (
                <LobbyScreen
                    onStart={() => setPhase("game")}
                    onExit={() => setPhase("start")}
                />
            )}
            {phase === "game" && <GameScreen onExit={() => setPhase("start")} />}
        </div>
    );
}

export default function App() {
    return (
        <SessionProvider>
            <GameHubProvider>
                <AppInner />
            </GameHubProvider>
        </SessionProvider>
    );
}