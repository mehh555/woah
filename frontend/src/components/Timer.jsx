import { useState, useEffect, useRef, memo } from "react";

export default memo(function Timer({ endsAt, total }) {
    const [displaySeconds, setDisplaySeconds] = useState(0);
    const barRef = useRef(null);

    useEffect(() => {
        if (!endsAt || !total || total <= 0) return;

        const endMs = new Date(endsAt).getTime();
        const startMs = endMs - total * 1000;
        const nowMs = Date.now();

        const initialRemaining = Math.max(0, (endMs - nowMs) / 1000);
        setDisplaySeconds(initialRemaining);

        const bar = barRef.current;
        if (bar) {
            const initialPct = Math.max(0, Math.min(100, (initialRemaining / total) * 100));
            bar.style.transition = "none";
            bar.style.width = `${initialPct}%`;

            requestAnimationFrame(() => {
                requestAnimationFrame(() => {
                    const remainingMs = Math.max(0, endMs - Date.now());
                    bar.style.transition = `width ${remainingMs}ms linear`;
                    bar.style.width = "0%";
                });
            });
        }

        const id = setInterval(() => {
            const remaining = Math.max(0, (endMs - Date.now()) / 1000);
            setDisplaySeconds(remaining);
            if (remaining <= 0) clearInterval(id);
        }, 1000);

        return () => clearInterval(id);
    }, [endsAt, total]);

    const pct = total > 0 ? Math.max(0, Math.min(100, (displaySeconds / total) * 100)) : 0;

    return (
        <div className="timer-wrap">
            <div
                ref={barRef}
                className={`timer-bar ${pct < 25 ? "danger" : ""}`}
            />
            <div className="timer-text">{Math.ceil(displaySeconds)}s</div>
        </div>
    );
});