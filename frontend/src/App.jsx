import { useState } from "react";
import { SessionProvider } from "./context/SessionContext.jsx";
import { GameHubProvider } from "./context/GameHubContext.jsx";
import StartScreen from "./screens/StartScreen.jsx";
import LobbyScreen from "./screens/LobbyScreen.jsx";
import GameScreen from "./screens/GameScreen.jsx";
import "./styles.css";
import "./screens.css";

function AppInner() {
    const [phase, setPhase] = useState("start");

    return (
        <div className="app-root">
            <div className="app-bg" />
            {phase === "start" && <StartScreen onEnter={() => setPhase("lobby")} />}
            {phase === "lobby" && <LobbyScreen onStart={() => setPhase("game")} />}
            {phase === "game" && <GameScreen />}
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