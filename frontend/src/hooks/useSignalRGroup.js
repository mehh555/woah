import { useEffect, useRef, useCallback } from "react";
import { useHub } from "../context/GameHubContext.jsx";

export function useSignalRGroup(joinMethod, leaveMethod, groupId, handlers = {}) {
    const { connection, connected } = useHub();
    const handlersRef = useRef(handlers);
    handlersRef.current = handlers;

    const invoke = useCallback((method, ...args) => {
        const conn = connection.current;
        if (conn?.state === "Connected") {
            conn.invoke(method, ...args).catch(() => { });
        }
    }, [connection]);

    useEffect(() => {
        if (!connected || !groupId) return;

        invoke(joinMethod, groupId);

        return () => {
            invoke(leaveMethod, groupId);
        };
    }, [connected, groupId, invoke, joinMethod, leaveMethod]);

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
