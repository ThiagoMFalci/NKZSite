import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import axios from "axios";
import { BsDiscord, BsPersonPlusFill } from "react-icons/bs";
import "./style.css";

const API_BASE_URL = import.meta.env.VITE_API_URL || "";
const DISCORD_INVITE_URL = import.meta.env.VITE_DISCORD_INVITE_URL || "";

function getApiMessage(error) {
    return (
        error?.response?.data?.message ||
        error?.response?.data?.Message ||
        error?.message ||
        "Erro ao criar conta"
    );
}

export default function Register() {
    const navigate = useNavigate();
    const [formData, setFormData] = useState({ email: "", password: "", confirmPassword: "", discordUsername: "", code: "" });
    const [awaitingEmail, setAwaitingEmail] = useState(false);
    const [awaitingDiscord, setAwaitingDiscord] = useState(false);
    const [loading, setLoading] = useState(false);
    const [feedback, setFeedback] = useState({ type: "", message: "" });

    function handleChange(event) {
        const { name, value } = event.target;
        setFormData((current) => ({ ...current, [name]: value }));
    }

    async function handleSubmit(event) {
        event.preventDefault();
        setFeedback({ type: "", message: "" });

        if (!formData.email || !formData.password || !formData.confirmPassword || !formData.discordUsername) {
            setFeedback({ type: "error", message: "Preencha todos os campos." });
            return;
        }

        if (formData.password !== formData.confirmPassword) {
            setFeedback({ type: "error", message: "As senhas precisam ser iguais." });
            return;
        }

        try {
            setLoading(true);
            const response = await axios.post(`${API_BASE_URL}/api/auth/User/CreateUsers`, {
                email: formData.email,
                passwordHash: formData.password,
                passwordSalt: formData.confirmPassword,
                role: "User",
                discordUsername: formData.discordUsername,
            });

            const success = response.data?.success ?? response.data?.Success ?? true;

            if (!success) {
                setFeedback({
                    type: "error",
                    message: response.data?.message || response.data?.Message || "Nao foi possivel criar a conta.",
                });
                return;
            }

            setAwaitingEmail(true);
            setFeedback({
                type: "success",
                message: response.data?.message || response.data?.Message || "Conta criada. Confira seu email e seu privado no Discord.",
            });
        } catch (error) {
            setFeedback({ type: "error", message: getApiMessage(error) });
        } finally {
            setLoading(false);
        }
    }

    async function handleVerifyEmail(event) {
        event.preventDefault();
        setFeedback({ type: "", message: "" });

        if (!formData.emailCode?.trim()) {
            setFeedback({ type: "error", message: "Informe o codigo enviado para seu email." });
            return;
        }

        try {
            setLoading(true);
            await axios.post(`${API_BASE_URL}/api/auth/User/VerifyEmail`, {
                email: formData.email,
                code: formData.emailCode,
            });
            setAwaitingEmail(false);
            setAwaitingDiscord(true);
            setFeedback({ type: "success", message: "Email confirmado. Agora confirme o codigo do Discord." });
        } catch (error) {
            setFeedback({ type: "error", message: getApiMessage(error) });
        } finally {
            setLoading(false);
        }
    }

    async function handleVerifyDiscord(event) {
        event.preventDefault();
        setFeedback({ type: "", message: "" });

        if (!formData.code.trim()) {
            setFeedback({ type: "error", message: "Informe o codigo recebido no Discord." });
            return;
        }

        try {
            setLoading(true);
            await axios.post(`${API_BASE_URL}/api/auth/User/VerifyDiscord`, {
                email: formData.email,
                code: formData.code,
            });
            setFeedback({ type: "success", message: "Conta verificada. Entre com email e senha para receber o codigo de acesso." });
            setTimeout(() => navigate("/login"), 900);
        } catch (error) {
            setFeedback({ type: "error", message: getApiMessage(error) });
        } finally {
            setLoading(false);
        }
    }

    async function handleResendCode() {
        try {
            setLoading(true);
            setFeedback({ type: "", message: "" });
            await axios.post(`${API_BASE_URL}/api/auth/User/ResendDiscordVerification`, JSON.stringify(formData.email), {
                headers: { "Content-Type": "application/json" },
            });
            setFeedback({ type: "success", message: "Novo codigo enviado no privado do Discord." });
        } catch (error) {
            setFeedback({ type: "error", message: getApiMessage(error) });
        } finally {
            setLoading(false);
        }
    }

    async function handleResendEmailCode() {
        try {
            setLoading(true);
            setFeedback({ type: "", message: "" });
            await axios.post(`${API_BASE_URL}/api/auth/User/ResendEmailVerification`, JSON.stringify(formData.email), {
                headers: { "Content-Type": "application/json" },
            });
            setFeedback({ type: "success", message: "Novo codigo enviado para seu email." });
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
                <div className="auth-icon"><BsPersonPlusFill /></div>
                <p className="auth-eyebrow">Nova conta</p>
                <h1>Criar conta</h1>
                <p className="auth-desc">Cadastre-se para vincular seu perfil e entrar na arena NKZ.</p>

                <form className="auth-form" onSubmit={awaitingEmail ? handleVerifyEmail : awaitingDiscord ? handleVerifyDiscord : handleSubmit}>
                    {awaitingEmail ? (
                        <>
                            <div className="discord-verify-panel email-verify-panel">
                                <BsPersonPlusFill />
                                <div>
                                    <strong>Confirme seu email</strong>
                                    <span>Enviamos um codigo para {formData.email}. Depois disso voce confirma o Discord.</span>
                                </div>
                            </div>

                            <label>
                                Codigo do email
                                <input
                                    type="text"
                                    name="emailCode"
                                    value={formData.emailCode || ""}
                                    onChange={handleChange}
                                    inputMode="numeric"
                                    maxLength={6}
                                    placeholder="000000"
                                />
                            </label>
                        </>
                    ) : awaitingDiscord ? (
                        <>
                            <div className="discord-verify-panel">
                                <BsDiscord />
                                <div>
                                    <strong>Confirme seu Discord</strong>
                                    <span>Enviamos um codigo por mensagem privada. O bot so envia se voce estiver no servidor NKZ.</span>
                                </div>
                            </div>

                            <label>
                                Codigo do Discord
                                <input
                                    type="text"
                                    name="code"
                                    value={formData.code}
                                    onChange={handleChange}
                                    inputMode="numeric"
                                    maxLength={6}
                                    placeholder="000000"
                                />
                            </label>
                        </>
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
                                    autoComplete="new-password"
                                    placeholder="Crie uma senha"
                                />
                            </label>

                            <label>
                                Confirmar senha
                                <input
                                    type="password"
                                    name="confirmPassword"
                                    value={formData.confirmPassword}
                                    onChange={handleChange}
                                    autoComplete="new-password"
                                    placeholder="Repita a senha"
                                />
                            </label>

                            <label>
                                Usuario do Discord
                                <input
                                    type="text"
                                    name="discordUsername"
                                    value={formData.discordUsername}
                                    onChange={handleChange}
                                    autoComplete="username"
                                    placeholder="Ex: shorainopureiya"
                                />
                                <span className="auth-hint">
                                    Use o nome de usuario, sem @. Voce precisa estar no{" "}
                                    <a
                                        className="auth-inline-link"
                                        href={DISCORD_INVITE_URL || undefined}
                                        target="_blank"
                                        rel="noreferrer"
                                    >
                                        servidor NKZ
                                    </a>{" "}
                                    para receber o codigo.
                                </span>
                            </label>
                        </>
                    )}

                    {feedback.message && (
                        <div className={`auth-feedback ${feedback.type}`}>{feedback.message}</div>
                    )}

                    <button className="btn-primary auth-submit" type="submit" disabled={loading}>
                        {awaitingEmail
                            ? (loading ? "Verificando..." : "Confirmar email")
                            : awaitingDiscord
                                ? (loading ? "Verificando..." : "Confirmar Discord")
                                : (loading ? "Criando..." : "Criar conta")}
                    </button>
                    {awaitingEmail && (
                        <button className="auth-link-button" type="button" disabled={loading} onClick={handleResendEmailCode}>
                            Reenviar codigo de email
                        </button>
                    )}
                    {awaitingDiscord && (
                        <button className="auth-link-button" type="button" disabled={loading} onClick={handleResendCode}>
                            Reenviar codigo
                        </button>
                    )}
                </form>

                <p className="auth-switch">
                    Ja tem conta? <Link to="/login">Entrar</Link>
                </p>
            </section>
        </main>
    );
}
