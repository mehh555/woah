export default function AboutModal({ onClose }) {
    return (
        <div className="modal-overlay" onClick={onClose}>
            <div className="modal-card anim-fadeUp" onClick={e => e.stopPropagation()}>
                <div className="modal-header">
                    <span className="round-badge">O projekcie</span>
                </div>

                <div className="about-content">
                    <p className="about-text">
                        <strong>Woah</strong> to multiplayer'owa gra muzyczna w której gracze rywalizują
                        o punkty zgadując tytuły i artystów na podstawie krótkich fragmentów piosenek.
                    </p>

                    <div className="about-section">
                        <div className="about-label">Jak grać?</div>
                        <p className="about-text">
                            Stwórz lobby, zaproś znajomych kodem, każdy dodaje swoje piosenki (max 10),
                            host startuje grę. Słuchaj fragmentu, wpisuj tytuł lub artystę — im szybciej, tym więcej punktów!
                        </p>
                    </div>

                    <div className="about-section">
                        <div className="about-label">Stos technologiczny</div>
                        <p className="about-text">
                            C# / ASP.NET Core 8 • React 18 • SignalR • PostgreSQL • EF Core • Docker • iTunes Search API
                        </p>
                    </div>

                    <div className="about-section">
                        <div className="about-label">Autorzy</div>
                        <div className="about-authors">
                            <a href="https://github.com/mehh555" target="_blank" rel="noopener noreferrer" className="author-link">
                                <img src="https://github.com/mehh555.png?size=48" alt="" className="author-avatar" />
                                <span>mehh555</span>
                            </a>
                            <a href="https://github.com/Pakanor" target="_blank" rel="noopener noreferrer" className="author-link">
                                <img src="https://github.com/Pakanor.png?size=48" alt="" className="author-avatar" />
                                <span>Pakanor</span>
                            </a>
                        </div>
                    </div>
                </div>
                <div className="about-section">
                    <div className="about-label">Informacje prawne</div>
                    <p className="about-text" style={{ fontSize: ".78rem" }}>
                        Woah jest niezależnym projektem niekomercyjnym i nie jest powiązany z Apple Inc.
                        Fragmenty utworów udostępniane są za pośrednictwem iTunes Search API wyłącznie
                        w celach demonstracyjnych (preview). Wszystkie prawa do utworów muzycznych należą
                        do ich właścicieli. Apple, Apple Music i iTunes są znakami towarowymi Apple Inc.
                    </p>
                </div>
                <button className="btn btn-secondary" onClick={onClose}>← Zamknij</button>
            </div>
        </div>
    );
}