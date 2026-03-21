import { useCallback, useEffect, useRef } from "react";
import { useHub } from "../context/GameHubContext.jsx";

export function useGameHub(events = {}, deps = []) {
    const { connection, connected } = useHub();
    const eventsRef = useRef(events);
    eventsRef.current = events;

    const invoke = useCallback((method, ...args) => {
        const conn = connection.current;
        if (conn?.state === "Connected") {
            conn.invoke(method, ...args).catch(() => { });
        }
    }, [connection]);

    useEffect(() => {
        const conn = connection.current;
        if (!conn) return;

        const registered = Object.entries(eventsRef.current).map(([event, handler]) => {
            conn.on(event, handler);
            return [event, handler];
        });

        return () => {
            registered.forEach(([event, handler]) => conn.off(event, handler));
        };
    }, [connection, connected, ...deps]);

    return { invoke, connected };
}