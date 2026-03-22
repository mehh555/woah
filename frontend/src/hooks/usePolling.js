import { useEffect, useRef, useState, useCallback } from "react";

export function usePolling(fetchFn, interval = 1500) {
    const [data, setData] = useState(null);
    const [error, setError] = useState(null);
    const fnRef = useRef(fetchFn);
    fnRef.current = fetchFn;

    const refetch = useCallback(async () => {
        try {
            const r = await fnRef.current();
            setData(r);
            setError(null);
        } catch (e) {
            setError(e.message);
        }
    }, []);

    useEffect(() => {
        let cancelled = false;
        async function run() {
            try {
                const r = await fnRef.current();
                if (!cancelled) { setData(r); setError(null); }
            } catch (e) {
                if (!cancelled) setError(e.message);
            } finally {
                //noop
            }
        }
        run();
        if (interval) {
            const id = setInterval(run, interval);
            return () => { cancelled = true; clearInterval(id); };
        }
        return () => { cancelled = true; };
    }, [interval, fetchFn]);

    return { data, error, refetch };
}