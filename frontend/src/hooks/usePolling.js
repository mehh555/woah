import { useEffect, useRef, useState } from "react";

export function usePolling(fetchFn, interval = 1500) {
  const [data, setData] = useState(null);
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(true);
  const fnRef = useRef(fetchFn);
  fnRef.current = fetchFn;

  useEffect(() => {
    let cancelled = false;
    async function run() {
      try {
        const r = await fnRef.current();
        if (!cancelled) { setData(r); setError(null); }
      } catch (e) {
        if (!cancelled) setError(e.message);
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    run();
    const id = setInterval(run, interval);
    return () => { cancelled = true; clearInterval(id); };
  }, [interval]);

  return { data, error, loading };
}
