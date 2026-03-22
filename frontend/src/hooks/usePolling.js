import { useEffect, useRef, useState, useCallback } from "react";

/**
 * Polls fetchFn at `interval` ms, but ONLY when `enabled` is true.
 * Always performs one initial fetch on mount regardless of `enabled`.
 * When transitioning from disabled→enabled, does an immediate refetch.
 */
export function usePolling(fetchFn, { interval = 10000, enabled = true } = {}) {
    const [data, setData] = useState(null);
    const [error, setError] = useState(null);
    const fnRef = useRef(fetchFn);
    fnRef.current = fetchFn;
    const didInitialFetch = useRef(false);

    const refetch = useCallback(async () => {
        try {
            const r = await fnRef.current();
            setData(r);
            setError(null);
        } catch (e) {
            setError(e.message);
        }
    }, []);

    // Initial fetch (always, regardless of enabled)
    useEffect(() => {
        let cancelled = false;

        async function run() {
            try {
                const r = await fnRef.current();
                if (!cancelled) { setData(r); setError(null); }
            } catch (e) {
                if (!cancelled) setError(e.message);
            }
            didInitialFetch.current = true;
        }

        run();
        return () => { cancelled = true; };
    }, [fetchFn]);

    // Interval polling — only when enabled
    useEffect(() => {
        if (!enabled || !interval) return;

        // Catch up after reconnect: immediate fetch when enabled flips to true
        if (didInitialFetch.current) {
            refetch();
        }

        const id = setInterval(() => {
            fnRef.current()
                .then(r => { setData(r); setError(null); })
                .catch(e => setError(e.message));
        }, interval);

        return () => clearInterval(id);
    }, [interval, enabled, refetch]);

    return { data, error, refetch };
}