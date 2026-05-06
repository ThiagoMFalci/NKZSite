import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import axios from "axios";
import { BsShieldLockFill } from "react-icons/bs";
import "./style.css";

const API_BASE_URL = import.meta.env.VITE_API_URL || "";

function getApiMessage(error) {
    return (
        error?.response?.data?.message ||
        error?.response?.data?.Message ||
        error?.message ||
        "Erro ao recuperar senha"
    );
}

export default function ForgotPassword() {
    const navigate = useNavigate();
    const [step, setStep] = useState("request");
    const [formData, setFormData] = useState({ email: "", code: "", newPassword: "", confirmPassword: "" });
    const [loading, setLoading] = useState(false);
    const [feedback, setFeedback] = useState({ type: "", message: "" });

    function handleChange(event) {
        const { name, value } = event.target;
        setFormData((current) => ({ ...current, [name]: value }));
    }

    async function requestCode(event) {
        event.preventDefault();
        setFeedback({ type: "", message: "" });
        if (!formData.email) {
            setFeedback({ type: "error", message: "Informe seu email." });
            return;
        }

        try {
            setLoading(true);
            const response = await axios.post(`${API_BASE_URL}/api/auth/User/ForgotPassword`, { email: formData.email });
            setStep("reset");
            setFeedback({ type: "success", message: response.data?.message || response.data?.Message || "Enviamos um codigo para seu email." });
        } catch (error) {
            setFeedback({ type: "error", message: getApiMessage(error) });
        } finally {
            setLoading(false);
        }
    }

    async function resetPassword(event) {
        event.preventDefault();
        setFeedback({ type: "", message: "" });
        if (!formData.code || !formData.newPassword || !formData.confirmPassword) {
            setFeedback({ type: "error", message: "Preencha codigo e nova senha." });
            return;
        }
        if (formData.newPassword !== formData.confirmPassword) {
            setFeedback({ type: "error", message: "As senhas precisam ser iguais." });
            return;
        }

        try {
            setLoading(true);
            await axios.post(`${API_BASE_URL}/api/auth/User/ResetPassword`, {
                email: formData.email,
                code: formData.code,
                newPassword: formData.newPassword,
            });
            setFeedback({ type: "success", message: "Senha atualizada. Voce ja pode entrar." });
            setTimeout(() => navigate("/login"), 900);
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
                <p className="auth-eyebrow">Recuperacao</p>
                <h1>Esqueci senha</h1>
                <p className="auth-desc">
                    {step === "request" ? "Receba um codigo no email para trocar sua senha." : "Digite o codigo recebido e escolha uma nova senha."}
                </p>

                <form className="auth-form" onSubmit={step === "request" ? requestCode : resetPassword}>
                    <label>
                        Email
                        <input
                            type="email"
                            name="email"
                            value={formData.email}
                            onChange={handleChange}
                            autoComplete="email"
                            disabled={step === "reset"}
                            placeholder="voce@email.com"
                        />
                    </label>

                    {step === "reset" && (
                        <>
                            <label>
                                Codigo
                                <input type="text" name="code" value={formData.code} onChange={handleChange} inputMode="numeric" maxLength={6} placeholder="000000" />
                            </label>
                            <label>
                                Nova senha
                                <input type="password" name="newPassword" value={formData.newPassword} onChange={handleChange} autoComplete="new-password" placeholder="Nova senha" />
                            </label>
                            <label>
                                Confirmar nova senha
                                <input type="password" name="confirmPassword" value={formData.confirmPassword} onChange={handleChange} autoComplete="new-password" placeholder="Repita a senha" />
                            </label>
                        </>
                    )}

                    {feedback.message && <div className={`auth-feedback ${feedback.type}`}>{feedback.message}</div>}

                    <button className="btn-primary auth-submit" type="submit" disabled={loading}>
                        {step === "request" ? (loading ? "Enviando..." : "Enviar codigo") : (loading ? "Salvando..." : "Trocar senha")}
                    </button>
                </form>

                <p className="auth-switch">
                    Lembrou a senha? <Link to="/login">Entrar</Link>
                </p>
            </section>
        </main>
    );
}
