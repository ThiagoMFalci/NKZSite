import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import axios from "axios";
import { BsShieldLockFill } from "react-icons/bs";
import { saveSession } from "../../utils/auth";
import "./style.css";

const API_BASE_URL = import.meta.env.VITE_API_URL || "";

function getApiMessage(error) {
    return (
        error?.response?.data?.message ||
        error?.response?.data?.Message ||
        error?.message ||
        "Erro ao realizar login"
    );
}

export default function Login() {
    const navigate = useNavigate();
    const [formData, setFormData] = useState({ email: "", password: "" });
    const [loading, setLoading] = useState(false);
    const [feedback, setFeedback] = useState({ type: "", message: "" });

    function handleChange(event) {
        const { name, value } = event.target;
        setFormData((current) => ({ ...current, [name]: value }));
    }

    async function handleSubmit(event) {
        event.preventDefault();
        setFeedback({ type: "", message: "" });

        if (!formData.email || !formData.password) {
            setFeedback({ type: "error", message: "Preencha email e senha." });
            return;
        }

        try {
            setLoading(true);
            const response = await axios.post(`${API_BASE_URL}/api/auth/User/Login`, {
                email: formData.email,
                password: formData.password,
                passwordSalt: formData.password,
            });

            const success = response.data?.success ?? response.data?.Success ?? true;
            const token = response.data?.data ?? response.data?.Data;

            if (!success || !token) {
                setFeedback({
                    type: "error",
                    message: response.data?.message || response.data?.Message || "Login invalido.",
                });
                return;
            }

            saveSession(token);
            navigate("/dashboard");
        } catch (error) {
            setFeedback({ type: "error", message: getApiMessage(error) });
        } finally {
            setLoading(false);
        }
    }

    return (
        <main className="auth-page">
            <div className="auth-grid" />
            <section className="auth-card">
                <div className="auth-icon"><BsShieldLockFill /></div>
                <p className="auth-eyebrow">Conta NKZ</p>
                <h1>Entrar</h1>
                <p className="auth-desc">Acesse sua conta para acompanhar seu desempenho competitivo.</p>

                <form className="auth-form" onSubmit={handleSubmit}>
                    <label>
                        Email
                        <input
                            type="email"
                            name="email"
                            value={formData.email}
                            onChange={handleChange}
                            autoComplete="email"
                            placeholder="voce@email.com"
                        />
                    </label>

                    <label>
                        Senha
                        <input
                            type="password"
                            name="password"
                            value={formData.password}
                            onChange={handleChange}
                            autoComplete="current-password"
                            placeholder="Sua senha"
                        />
                    </label>

                    {feedback.message && (
                        <div className={`auth-feedback ${feedback.type}`}>{feedback.message}</div>
                    )}

                    <button className="btn-primary auth-submit" type="submit" disabled={loading}>
                        {loading ? "Entrando..." : "Entrar"}
                    </button>
                </form>

                <p className="auth-switch">
                    Ainda nao tem conta? <Link to="/register">Criar conta</Link>
                </p>
            </section>
        </main>
    );
}
