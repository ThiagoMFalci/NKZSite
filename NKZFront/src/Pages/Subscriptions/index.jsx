import { useEffect, useState } from "react";
import axios from "axios";
import { FaBolt, FaCheck, FaCrown, FaReceipt, FaShieldAlt } from "react-icons/fa";
import { getAuthHeaders } from "../../utils/auth";
import "./style.css";

const API_BASE_URL = import.meta.env.VITE_API_URL || "";

function unwrap(response) {
    return response?.data?.data ?? response?.data?.Data ?? response?.data ?? null;
}

function money(value) {
    return Number(value || 0).toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

function formatDate(value) {
    if (!value) return "A definir";
    return new Date(value).toLocaleDateString("pt-BR");
}

export default function SubscriptionsPage() {
    const [plans, setPlans] = useState([]);
    const [subscription, setSubscription] = useState(null);
    const [loading, setLoading] = useState(true);
    const [feedback, setFeedback] = useState("");
    const [checkoutLoading, setCheckoutLoading] = useState("");

    useEffect(() => {
        loadData();
    }, []);

    async function loadData() {
        setLoading(true);
        try {
            const [plansResponse, subscriptionResponse] = await Promise.all([
                axios.get(`${API_BASE_URL}/api/subscriptions/plans`, { headers: getAuthHeaders() }),
                axios.get(`${API_BASE_URL}/api/subscriptions/me`, { headers: getAuthHeaders() }).catch(() => ({ data: null })),
            ]);

            setPlans(unwrap(plansResponse) || []);
            setSubscription(unwrap(subscriptionResponse));
        } finally {
            setLoading(false);
        }
    }

    async function handleSubscribe(planId) {
        setCheckoutLoading(planId);
        setFeedback("");
        try {
            const response = await axios.post(`${API_BASE_URL}/api/subscriptions/checkout`, { planId }, { headers: getAuthHeaders() });
            const checkoutUrl = unwrap(response);
            if (checkoutUrl) {
                window.location.href = checkoutUrl;
                return;
            }

            setFeedback("Assinatura ativada com sucesso.");
            await loadData();
        } catch (error) {
            setFeedback(error.response?.data?.message ?? error.response?.data?.Message ?? "Nao foi possivel iniciar a assinatura.");
        } finally {
            setCheckoutLoading("");
        }
    }

    const activeSubscription = subscription && subscription.status === "Active" && new Date(subscription.endsAt) > new Date();

    return (
        <main className="subscriptions-page">
            <div className="subscriptions-bg" />
            <div className="subscriptions-container">
                <section className="subscriptions-hero">
                    <div>
                        <p className="subscriptions-eyebrow">Assinaturas NKZ</p>
                        <h1>Beneficios para competir melhor</h1>
                        <span>Planos para destacar seu perfil, organizar times e liberar vantagens futuras da arena.</span>
                    </div>
                    <aside className={`subscription-status ${activeSubscription ? "active" : ""}`}>
                        <FaCrown />
                        <span>{activeSubscription ? "Assinatura ativa" : "Plano atual"}</span>
                        <strong>{activeSubscription ? subscription.planName : "Free"}</strong>
                        <small>{activeSubscription ? `Valida ate ${formatDate(subscription.endsAt)}` : "Sem cobranca ativa"}</small>
                    </aside>
                </section>

                {feedback && <section className="subscriptions-feedback">{feedback}</section>}

                <section className="subscriptions-benefits">
                    <article><FaShieldAlt /><strong>Identidade competitiva</strong><span>Perfil mais completo e preparado para recrutamento.</span></article>
                    <article><FaBolt /><strong>Prioridade futura</strong><span>Base pronta para beneficios exclusivos em ligas e torneios.</span></article>
                    <article><FaReceipt /><strong>Pagamento integrado</strong><span>Checkout via Mercado Pago com webhook na API.</span></article>
                </section>

                <section className="plans-grid">
                    {loading ? (
                        <div className="plans-empty">Carregando planos...</div>
                    ) : plans.length ? plans.map((plan) => {
                        const benefits = String(plan.benefits ?? plan.Benefits ?? "")
                            .split(/\r?\n|;/)
                            .map((item) => item.trim())
                            .filter(Boolean);
                        const planId = plan.id ?? plan.Id;
                        const isCurrent = activeSubscription && (subscription.subscriptionPlanId ?? subscription.SubscriptionPlanId) === planId;

                        return (
                            <article key={planId} className={`plan-card ${plan.isFeatured || plan.IsFeatured ? "featured" : ""}`}>
                                {(plan.isFeatured || plan.IsFeatured) && <span className="plan-ribbon">Recomendado</span>}
                                <div className="plan-topline">
                                    <span>{plan.durationMonths ?? plan.DurationMonths} mes(es)</span>
                                    <FaCrown />
                                </div>
                                <h2>{plan.name ?? plan.Name}</h2>
                                <p>{plan.description ?? plan.Description}</p>
                                <strong className="plan-price">{money(plan.price ?? plan.Price)}</strong>
                                <div className="plan-benefits">
                                    {(benefits.length ? benefits : ["Acesso ao ecossistema NKZ", "Perfil competitivo organizado", "Base para vantagens premium"]).map((benefit) => (
                                        <span key={benefit}><FaCheck /> {benefit}</span>
                                    ))}
                                </div>
                                <button type="button" disabled={isCurrent || checkoutLoading === planId} onClick={() => handleSubscribe(planId)}>
                                    {isCurrent ? "Plano ativo" : checkoutLoading === planId ? "Gerando checkout..." : "Assinar plano"}
                                </button>
                            </article>
                        );
                    }) : (
                        <div className="plans-empty">Nenhum plano ativo por enquanto.</div>
                    )}
                </section>
            </div>
        </main>
    );
}
