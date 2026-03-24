import { Component } from "react";

export default class ErrorBoundary extends Component {
    state = { error: null };

    static getDerivedStateFromError(error) {
        return { error };
    }

    render() {
        if (this.state.error) {
            return (
                <div className="error-msg" style={{ margin: "2rem", textAlign: "center" }}>
                    <div style={{ fontSize: "1.2rem", marginBottom: ".5rem" }}>Coś poszło nie tak.</div>
                    <button
                        className="btn btn-secondary"
                        onClick={() => {
                            this.setState({ error: null });
                            window.location.reload();
                        }}
                    >
                        Odśwież stronę
                    </button>
                </div>
            );
        }

        return this.props.children;
    }
}
