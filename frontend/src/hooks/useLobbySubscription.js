import { useEffect, useRef, useCallback } from "react";
import { useHub } from "../context/GameHubContext.jsx";

/**
 * Manages SignalR lobby group membership + event handlers.
 * Symmetrically joins on mount/reconnect, leaves on unmount/code change.
 */
export function useLobbySubscription(lobbyCode, handlers = {}) {
    const { connection, connected } = useHub();
    const handlersRef = useRef(handlers);
    handlersRef.current = handlers;

    const invoke = useCallback((method, ...args) => {
        const conn = connection.current;
        if (conn?.state === "Connected") {
            conn.invoke(method, ...args).catch(() => { });
        }
    }, [connection]);

    // Group join/leave lifecycle
    useEffect(() => {
        if (!connected || !lobbyCode) return;

        invoke("JoinLobby", lobbyCode);

        return () => {
            invoke("LeaveLobby", lobbyCode);
        };
    }, [connected, lobbyCode, invoke]);

    // Event registration
    useEffect(() => {
        const conn = connection.current;
        if (!conn) return;

        const entries = Object.entries(handlersRef.current);
        entries.forEach(([event, handler]) => conn.on(event, handler));

        return () => {
            entries.forEach(([event, handler]) => conn.off(event, handler));
        };
    }, [connection, connected]);

    return { invoke, connected };
}