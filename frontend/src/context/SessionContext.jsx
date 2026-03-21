import { createContext, useContext, useEffect, useMemo, useState } from "react";

const STORAGE_KEY = "woah.session";
const SessionContext = createContext(null);

function readStoredSession() {
    try {
        const raw = window.localStorage.getItem(STORAGE_KEY);
        if (!raw) return null;
        return JSON.parse(raw);
    } catch {
        return null;
    }
}

export function SessionProvider({ children }) {
    const [session, setSessionState] = useState(() => readStoredSession());

    useEffect(() => {
        try {
            if (session) {
                window.localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
            } else {
                window.localStorage.removeItem(STORAGE_KEY);
            }
        } catch {
            // ignore storage errors in dev
        }
    }, [session]);

    const value = useMemo(() => ({
        session,
        setSession: setSessionState,
        clearSession: () => setSessionState(null),
    }), [session]);

    return (
        <SessionContext.Provider value={value}>
            {children}
        </SessionContext.Provider>
    );
}

export function useSession() {
    return useContext(SessionContext);
}