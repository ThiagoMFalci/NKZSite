import { useNavigate } from "react-router-dom";
import "./style.css";

export default function Index() {
    const navigate = useNavigate();

    return (
        <div className="notfound-page">
            <div className="notfound-grid" />

            <div className="notfound-content">

                <span className="notfound-badge">
                    Página não encontrada
                </span>

                <h1 className="notfound-code">404</h1>

                <h2 className="notfound-title">Você se perdeu no mapa</h2>

                <p className="notfound-desc">
                    A rota que você tentou acessar não existe ou foi removida.
                    Volte para a base e tente novamente.
                </p>

                <div className="notfound-actions">
                    <button
                        className="btn-nf-primary"
                        onClick={() => navigate("/")}
                    >
                        Voltar para Home
                    </button>
                    <button
                        className="btn-nf-ghost"
                        onClick={() => navigate(-1)}
                    >
                        Página anterior
                    </button>
                </div>
            </div>

            <div className="notfound-line" />
        </div>
    );
}