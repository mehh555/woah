import { useState, useEffect } from "react";

export default function Timer({ endsAt, total }) {
    const [seconds, setSeconds] = useState(0);

    useEffect(() => {
        if (!endsAt) return;

        function tick() {
            const remaining = Math.max(0, (new Date(endsAt).getTime() - Date.now()) / 1000);
            setSeconds(remaining);
        }

        tick();
        const id = setInterval(tick, 200);
        return () => clearInterval(id);
    }, [endsAt]);

    const pct = total > 0 ? Math.max(0, Math.min(100, (seconds / total) * 100)) : 0;

    return (
        <div className="timer-wrap">
            <div
                className={`timer-bar ${pct < 25 ? "danger" : ""}`}
                style={{ width: `${pct}%`, transition: "width 0.2s linear" }}
            />
            <div className="timer-text">{Math.ceil(seconds)}s</div>
        </div>
    );
}