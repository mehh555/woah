export default function Timer({ seconds, total }) {
  const pct = Math.max(0, Math.min(100, (seconds / total) * 100));
  return (
    <div className="timer-wrap">
      <div className={`timer-bar ${pct < 25 ? "danger" : ""}`} style={{ width: `${pct}%`, transition: "width 1s linear" }} />
    </div>
  );
}
