import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { FiMail, FiLock, FiUser, FiEye, FiEyeOff, FiHash } from "react-icons/fi";
import "./style.css";

/* ─── Limites de caracteres ─────────────────────────── */
const LIMITS = {
    email:    120,
    password: 72,
    gameName: 16,
    tagline:  5,
};

/* ─── Sanitização ───────────────────────────────────── */
function sanitize(value) {
    return value
        .replace(/['"`;\\]/g, "")
        .replace(/--/g, "")
        .replace(/<[^>]*>/g, "")
        .replace(/\b(SELECT|INSERT|UPDATE|DELETE|DROP|UNION|WHERE|OR|AND)\b/gi, "");
}

function sanitizeEmail(value) {
    return value.replace(/[^a-zA-Z0-9@._+\-]/g, "");
}

function sanitizeGameName(value) {
    // Riot permite letras, números e espaços — sem # (é o separador)
    return value.replace(/[^\p{L}\p{N}]/gu, "");
}

function sanitizeTagline(value) {
    // Tagline: só alfanumérico
    return value.replace(/[^a-zA-Z0-9]/g, "");
}

/* ─── Validações ────────────────────────────────────── */
function validateEmail(v) {
    if (!v) return "E-mail é obrigatório";
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v)) return "E-mail inválido";
    return "";
}
function validatePassword(v) {
    if (!v) return "Senha é obrigatória";
    if (v.length < 8) return "Mínimo 8 caracteres";
    return "";
}
function validateConfirm(v, pass) {
    if (!v) return "Confirme sua senha";
    if (v !== pass) return "As senhas não coincidem";
    return "";
}
function validateGameName(v) {
    if (!v) return "Game Name é obrigatório";
    if (v.length < 3) return "Mínimo 3 caracteres";
    return "";
}
function validateTagline(v) {
    if (!v) return "Tagline é obrigatória";
    if (v.length < 3) return "Mínimo 3 caracteres";
    return "";
}

/* ─── Força da senha ────────────────────────────────── */
function getStrength(pass) {
    if (!pass) return 0;
    let score = 0;
    if (pass.length >= 8)        score++;
    if (/[A-Z]/.test(pass))      score++;
    if (/[0-9]/.test(pass))      score++;
    if (/[^A-Za-z0-9]/.test(pass)) score++;
    return score;
}

function StrengthBar({ password }) {
    const score = getStrength(password);
    const label = ["", "Fraca", "Média", "Boa", "Forte"][score] || "";
    const cls   = score <= 1 ? "weak" : score <= 2 ? "medium" : "strong";
    if (!password) return null;
    return (
        <div className="password-strength">
            {[1, 2, 3, 4].map((i) => (
                <div key={i} className={`strength-bar ${i <= score ? cls : ""}`} />
            ))}
            <span className={`strength-label ${cls}`}>{label}</span>
        </div>
    );
}

/* ─── Campo genérico ────────────────────────────────── */
function Field({ label, icon, type = "text", value, onChange, onBlur, error, maxLength, minLength, placeholder, showToggle, showPass, onToggle, hint }) {
    const remaining = maxLength - value.length;
    const nearLimit = remaining <= Math.floor(maxLength * 0.2);

    return (
        <div className="field">
            <div className="field-label">
                <span>{label}</span>
                <span className={`field-counter ${nearLimit ? "warn" : ""}`}>
                    {minLength && value.length < minLength
                        ? `mín. ${minLength}`
                        : `${value.length}/${maxLength}`}
                </span>
            </div>
            <div className="field-wrap">
                <input
                    className={`field-input ${error ? "error" : ""}`}
                    type={showToggle ? (showPass ? "text" : "password") : type}
                    value={value}
                    onChange={onChange}
                    onBlur={onBlur}
                    maxLength={maxLength}
                    placeholder={placeholder}
                    autoComplete="off"
                    spellCheck={false}
                />
                <span className="field-icon">{icon}</span>
                {showToggle && (
                    <button type="button" className="btn-eye" onClick={onToggle} tabIndex={-1}>
                        {showPass ? <FiEyeOff /> : <FiEye />}
                    </button>
                )}
            </div>
            {hint && !error && <div className="field-hint">{hint}</div>}
            {error && <div className="field-error">⚠ {error}</div>}
        </div>
    );
}

/* ─── Campo Riot ID (Game Name + Tagline juntos) ────── */
function RiotIdField({ gameName, tagline, onGameNameChange, onTaglineChange, onGameNameBlur, onTaglineBlur, gameNameError, taglineError, gameNameTouched, taglineTouched }) {
    return (
        <div className="field">
            <div className="field-label">
                <span>Riot ID</span>
                <span className="field-counter field-counter-subtle">Game Name # Tagline</span>
            </div>

            <div className="riot-id-wrap">
                {/* Game Name */}
                <div className="riot-id-gamename">
                    <div className="field-wrap">
                        <input
                            className={`field-input ${gameNameTouched && gameNameError ? "error" : ""}`}
                            type="text"
                            value={gameName}
                            onChange={onGameNameChange}
                            onBlur={onGameNameBlur}
                            maxLength={LIMITS.gameName}
                            placeholder="SeuNickLoL"
                            autoComplete="off"
                            spellCheck={false}
                        />
                        <span className="field-icon"><FiUser /></span>
                    </div>
                </div>

                {/* Separador # */}
                <div className="riot-id-sep">#</div>

                {/* Tagline */}
                <div className="riot-id-tagline">
                    <div className="field-wrap">
                        <input
                            className={`field-input field-input-tag ${taglineTouched && taglineError ? "error" : ""}`}
                            type="text"
                            value={tagline}
                            onChange={onTaglineChange}
                            onBlur={onTaglineBlur}
                            maxLength={LIMITS.tagline}
                            placeholder="BR1"
                            autoComplete="off"
                            spellCheck={false}
                        />
                        <span className="field-icon"><FiHash /></span>
                    </div>
                </div>
            </div>

            {/* Erros */}
            {gameNameTouched && gameNameError && (
                <div className="field-error">⚠ Game Name: {gameNameError}</div>
            )}
            {taglineTouched && taglineError && (
                <div className="field-error">⚠ Tagline: {taglineError}</div>
            )}

            <div className="field-hint">
                Ex: <strong>SeuNickLoL#BR1</strong> — Game Name: 3–16 chars, Tagline: 3–5 chars
            </div>
        </div>
    );
}

/* ─── Login Form ────────────────────────────────────── */
function LoginForm({ onSwitch }) {
    const navigate = useNavigate();
    const [form, setForm]     = useState({ email: "", password: "" });
    const [errors, setErrors] = useState({});
    const [showPass, setShowPass] = useState(false);
    const [touched, setTouched]   = useState({});

    function handleChange(field, sanitizeFn) {
        return (e) => {
            if (e.nativeEvent.isComposing) return; // Ignorar eventos de composição (IME)

            const val = sanitizeFn(e.target.value).slice(0, LIMITS[field] || 120);
            setForm((f) => ({ ...f, [field]: val }));
            if (touched[field]) validate(field, val);
        };
    }

    function validate(field, val) {
        const v = val ?? form[field];
        let err = "";
        if (field === "email")    err = validateEmail(v);
        if (field === "password") err = validatePassword(v);
        setErrors((e) => ({ ...e, [field]: err }));
        return !err;
    }

    function handleBlur(field) {
        setTouched((t) => ({ ...t, [field]: true }));
        validate(field);
    }

    function handleSubmit(e) {
        e.preventDefault();
        const fields = ["email", "password"];
        setTouched(Object.fromEntries(fields.map((f) => [f, true])));
        const valid = fields.every((f) => validate(f));
        if (!valid) return;
        console.log("Login:", form);
        navigate("/");
    }

    return (
        <form className="auth-form" onSubmit={handleSubmit} noValidate>
            <Field
                label="E-mail"
                icon={<FiMail />}
                type="email"
                value={form.email}
                onChange={handleChange("email", sanitizeEmail)}
                onBlur={() => handleBlur("email")}
                error={touched.email && errors.email}
                maxLength={LIMITS.email}
                placeholder="seu@email.com"
            />
            <Field
                label="Senha"
                icon={<FiLock />}
                value={form.password}
                onChange={handleChange("password", sanitize)}
                onBlur={() => handleBlur("password")}
                error={touched.password && errors.password}
                maxLength={LIMITS.password}
                placeholder="••••••••"
                showToggle
                showPass={showPass}
                onToggle={() => setShowPass((v) => !v)}
            />

            <button type="submit" className="btn-auth-submit">Entrar</button>

            <p className="auth-footer-text">
                Não tem conta?{" "}
                <button type="button" onClick={onSwitch}>Criar conta grátis</button>
            </p>
        </form>
    );
}

/* ─── Register Form ─────────────────────────────────── */
function RegisterForm({ onSwitch }) {
    const navigate = useNavigate();
    const [form, setForm] = useState({
        email: "", password: "", confirm: "", gameName: "", tagline: "",
    });
    const [errors, setErrors]   = useState({});
    const [showPass, setShowPass]       = useState(false);
    const [showConfirm, setShowConfirm] = useState(false);
    const [touched, setTouched] = useState({});

    function handleChange(field, sanitizeFn, limit) {
        return (e) => {
            if (e.nativeEvent.isComposing) return; // Ignorar eventos de composição (IME)

            const val = sanitizeFn(e.target.value).slice(0, limit || 120);
            setForm((f) => ({ ...f, [field]: val }));
            if (touched[field]) validate(field, val);
        };
    }

    function validate(field, val) {
        const v = val ?? form[field];
        let err = "";
        if (field === "email")    err = validateEmail(v);
        if (field === "password") err = validatePassword(v);
        if (field === "confirm")  err = validateConfirm(v, form.password);
        if (field === "gameName") err = validateGameName(v);
        if (field === "tagline")  err = validateTagline(v);
        setErrors((e) => ({ ...e, [field]: err }));
        return !err;
    }

    function handleBlur(field) {
        setTouched((t) => ({ ...t, [field]: true }));
        validate(field);
    }

    function handleSubmit(e) {
        e.preventDefault();
        const fields = ["email", "password", "confirm", "gameName", "tagline"];
        setTouched(Object.fromEntries(fields.map((f) => [f, true])));
        const valid = fields.every((f) => validate(f));
        if (!valid) return;
        console.log("Register:", { ...form, riotId: `${form.gameName}#${form.tagline}` });
        navigate("/");
    }

    return (
        <form className="auth-form" onSubmit={handleSubmit} noValidate>
            <RiotIdField
                gameName={form.gameName}
                tagline={form.tagline}
                onGameNameChange={handleChange("gameName", sanitizeGameName, LIMITS.gameName)}
                onTaglineChange={handleChange("tagline", sanitizeTagline, LIMITS.tagline)}
                onGameNameBlur={() => handleBlur("gameName")}
                onTaglineBlur={() => handleBlur("tagline")}
                gameNameError={errors.gameName}
                taglineError={errors.tagline}
                gameNameTouched={touched.gameName}
                taglineTouched={touched.tagline}
            />

            <Field
                label="E-mail"
                icon={<FiMail />}
                type="email"
                value={form.email}
                onChange={handleChange("email", sanitizeEmail, LIMITS.email)}
                onBlur={() => handleBlur("email")}
                error={touched.email && errors.email}
                maxLength={LIMITS.email}
                placeholder="seu@email.com"
            />

            <Field
                label="Senha"
                icon={<FiLock />}
                value={form.password}
                onChange={handleChange("password", sanitize, LIMITS.password)}
                onBlur={() => handleBlur("password")}
                error={touched.password && errors.password}
                maxLength={LIMITS.password}
                minLength={8}
                placeholder="Mínimo 8 caracteres"
                showToggle
                showPass={showPass}
                onToggle={() => setShowPass((v) => !v)}
            />
            <StrengthBar password={form.password} />

            <Field
                label="Confirmar senha"
                icon={<FiLock />}
                value={form.confirm}
                onChange={handleChange("confirm", sanitize, LIMITS.password)}
                onBlur={() => handleBlur("confirm")}
                error={touched.confirm && errors.confirm}
                maxLength={LIMITS.password}
                placeholder="Repita a senha"
                showToggle
                showPass={showConfirm}
                onToggle={() => setShowConfirm((v) => !v)}
            />

            <button type="submit" className="btn-auth-submit">Criar conta</button>

            <p className="auth-footer-text">
                Já tem conta?{" "}
                <button type="button" onClick={onSwitch}>Fazer login</button>
            </p>
        </form>
    );
}

/* ─── Page ──────────────────────────────────────────── */
export default function Auth() {
    const [tab, setTab] = useState("login");

    return (
        <div className="auth-page">
            <div className="auth-grid" />
            <div className="auth-orb-1" />
            <div className="auth-orb-2" />

            <div className="auth-card">
                <div className="auth-header">
                    <a href="/" className="auth-logo">NKZ<span>Academy</span></a>
                    <div className="auth-tabs">
                        <button
                            className={`auth-tab ${tab === "login" ? "active" : ""}`}
                            onClick={() => setTab("login")}
                        >Login</button>
                        <button
                            className={`auth-tab ${tab === "register" ? "active" : ""}`}
                            onClick={() => setTab("register")}
                        >Criar conta</button>
                    </div>
                </div>

                <div className="auth-body">
                    {tab === "login"
                        ? <LoginForm    onSwitch={() => setTab("register")} />
                        : <RegisterForm onSwitch={() => setTab("login")} />
                    }
                </div>
            </div>
        </div>
    );
}