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
    const [twoFactor, setTwoFactor] = useState({ required: false, token: "", code: "" });
    const [loading, setLoading] = useState(false);
    const [feedback, setFeedback] = useState({ type: "", message: "" });

    function handleChange(event) {
        const { name, value } = event.target;
        setFormData((current) => ({ ...current, [name]: value }));
    }

    function handleTwoFactorChange(event) {
        setTwoFactor((current) => ({ ...current, code: event.target.value }));
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
            const data = response.data?.data ?? response.data?.Data;
            const requiresTwoFactor = data?.requiresTwoFactor ?? data?.RequiresTwoFactor;

            if (success && requiresTwoFactor) {
                setTwoFactor({
                    required: true,
                    token: data?.twoFactorToken ?? data?.TwoFactorToken ?? "",
                    code: "",
                });
                setFeedback({ type: "success", message: response.data?.message || response.data?.Message || "Codigo enviado para seu email." });
                return;
            }

            const token = typeof data === "string" ? data : "";

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

    async function handleTwoFactorSubmit(event) {
        event.preventDefault();
        setFeedback({ type: "", message: "" });

        if (!twoFactor.code.trim()) {
            setFeedback({ type: "error", message: "Informe o codigo enviado para seu email." });
            return;
        }

        try {
            setLoading(true);
            const response = await axios.post(`${API_BASE_URL}/api/auth/User/VerifyTwoFactor`, {
                email: formData.email,
                code: twoFactor.code,
                twoFactorToken: twoFactor.token,
            });

            const success = response.data?.success ?? response.data?.Success ?? true;
            const token = response.data?.data ?? response.data?.Data;
            if (!success || !token) {
                setFeedback({ type: "error", message: response.data?.message || response.data?.Message || "Codigo invalido." });
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
                <h1>{twoFactor.required ? "Verificacao" : "Entrar"}</h1>
                <p className="auth-desc">
                    {twoFactor.required
                        ? "Digite o codigo enviado para seu email para concluir o acesso."
                        : "Acesse sua conta para acompanhar seu desempenho competitivo."}
                </p>

                <form className="auth-form" onSubmit={twoFactor.required ? handleTwoFactorSubmit : handleSubmit}>
                    {twoFactor.required ? (
                        <label>
                            Codigo de email
                            <input
                                type="text"
                                name="code"
                                value={twoFactor.code}
                                onChange={handleTwoFactorChange}
                                inputMode="numeric"
                                maxLength={6}
                                placeholder="000000"
                            />
                        </label>
                    ) : (
                        <>
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
                        </>
                    )}

                    {feedback.message && (
                        <div className={`auth-feedback ${feedback.type}`}>{feedback.message}</div>
                    )}

                    <button className="btn-primary auth-submit" type="submit" disabled={loading}>
                        {twoFactor.required ? (loading ? "Verificando..." : "Confirmar codigo") : (loading ? "Entrando..." : "Entrar")}
                    </button>
                </form>

                <p className="auth-switch">
                    {twoFactor.required ? (
                        <button className="auth-link-button" type="button" onClick={() => setTwoFactor({ required: false, token: "", code: "" })}>
                            Voltar ao login
                        </button>
                    ) : (
                        <>
                            <Link to="/forgot-password">Esqueci minha senha</Link>
                            <span className="auth-switch-separator"> | </span>
                            Ainda nao tem conta? <Link to="/register">Criar conta</Link>
                        </>
                    )}
                </p>
            </section>
        </main>
    );
}
