import { createContext, useContext, useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";

const GameHubContext = createContext(null);

export function GameHubProvider({ children }) {
    const [connected, setConnected] = useState(false);
    const connRef = useRef(null);

    useEffect(() => {
        const conn = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/game")
            .withAutomaticReconnect([0, 1000, 2000, 5000, 10000])
            .build();

        connRef.current = conn;

        conn.onreconnected(() => setConnected(true));
        conn.onreconnecting(() => setConnected(false));
        conn.onclose(() => setConnected(false));

        conn.start()
            .then(() => setConnected(true))
            .catch(err => console.error("SignalR connect failed:", err));

        return () => { conn.stop(); };
    }, []);

    return (
        <GameHubContext.Provider value={{ connection: connRef, connected }}>
            {children}
        </GameHubContext.Provider>
    );
}

export function useHub() {
    return useContext(GameHubContext);
}