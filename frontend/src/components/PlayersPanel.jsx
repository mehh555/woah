export default function PlayersPanel({ players = [], myId, animScores = {} }) {
  return (
    <div className="players-panel">
      {players.map(p => (
        <div key={p.id} className={["player-chip", p.guessed ? "guessed" : "", p.id === myId ? "me" : ""].filter(Boolean).join(" ")}>
          <span className="chip-nick" title={p.nickname}>{p.nickname}</span>
          <span className={`chip-score ${animScores[p.id] ? "score-animate" : ""}`}>{p.score} pkt</span>
          {p.guessed && <span className="chip-guessed">✓ trafił</span>}
        </div>
      ))}
    </div>
  );
}
