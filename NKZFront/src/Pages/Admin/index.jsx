import { useEffect, useMemo, useState } from "react";
import axios from "axios";
import { FaCrown, FaMoneyBillWave, FaShieldAlt, FaTrophy, FaUsers } from "react-icons/fa";
import { getAuthHeaders, getCurrentUser } from "../../utils/auth";
import "./style.css";

const API_BASE_URL = import.meta.env.VITE_API_URL || "";
const tabs = ["overview", "plans", "users", "teams", "leagues", "payments", "subscriptions"];

function unwrap(response) {
    return response?.data?.data ?? response?.data?.Data ?? response?.data ?? null;
}

function money(value) {
    return Number(value || 0).toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

function date(value) {
    if (!value) return "-";
    return new Date(value).toLocaleDateString("pt-BR");
}

const emptyPlan = {
    name: "",
    description: "",
    price: 0,
    durationMonths: 1,
    benefits: "",
    isActive: true,
    isFeatured: false,
    sortOrder: 0,
};

export default function AdminPage() {
    const currentUser = getCurrentUser();
    const [activeTab, setActiveTab] = useState("overview");
    const [dashboard, setDashboard] = useState(null);
    const [lists, setLists] = useState({});
    const [planForm, setPlanForm] = useState(emptyPlan);
    const [editingPlanId, setEditingPlanId] = useState("");
    const [feedback, setFeedback] = useState("");
    const [loading, setLoading] = useState(true);

    const isAdmin = currentUser?.role === "Admin";

    useEffect(() => {
        if (isAdmin) loadAdmin();
    }, [isAdmin]);

    async function loadAdmin() {
        setLoading(true);
        try {
            const [dashboardResponse, plansResponse, usersResponse, teamsResponse, leaguesResponse, paymentsResponse, subscriptionsResponse] = await Promise.all([
                axios.get(`${API_BASE_URL}/api/admin/dashboard`, { headers: getAuthHeaders() }),
                axios.get(`${API_BASE_URL}/api/admin/plans`, { headers: getAuthHeaders() }),
                axios.get(`${API_BASE_URL}/api/admin/users`, { headers: getAuthHeaders() }),
                axios.get(`${API_BASE_URL}/api/admin/teams`, { headers: getAuthHeaders() }),
                axios.get(`${API_BASE_URL}/api/admin/leagues`, { headers: getAuthHeaders() }),
                axios.get(`${API_BASE_URL}/api/admin/payments`, { headers: getAuthHeaders() }),
                axios.get(`${API_BASE_URL}/api/admin/subscriptions`, { headers: getAuthHeaders() }),
            ]);
            setDashboard(unwrap(dashboardResponse));
            setLists({
                plans: unwrap(plansResponse)?.data ?? unwrap(plansResponse)?.Data ?? unwrap(plansResponse) ?? [],
                users: unwrap(usersResponse) ?? [],
                teams: unwrap(teamsResponse) ?? [],
                leagues: unwrap(leaguesResponse) ?? [],
                payments: unwrap(paymentsResponse) ?? [],
                subscriptions: unwrap(subscriptionsResponse) ?? [],
            });
        } finally {
            setLoading(false);
        }
    }

    function startEditPlan(plan) {
        setEditingPlanId(plan.id ?? plan.Id);
        setPlanForm({
            name: plan.name ?? plan.Name ?? "",
            description: plan.description ?? plan.Description ?? "",
            price: plan.price ?? plan.Price ?? 0,
            durationMonths: plan.durationMonths ?? plan.DurationMonths ?? 1,
            benefits: plan.benefits ?? plan.Benefits ?? "",
            isActive: plan.isActive ?? plan.IsActive ?? true,
            isFeatured: plan.isFeatured ?? plan.IsFeatured ?? false,
            sortOrder: plan.sortOrder ?? plan.SortOrder ?? 0,
        });
        setActiveTab("plans");
    }

    async function savePlan(event) {
        event.preventDefault();
        setFeedback("");
        try {
            const payload = {
                ...planForm,
                price: Number(planForm.price),
                durationMonths: Number(planForm.durationMonths),
                sortOrder: Number(planForm.sortOrder),
            };
            if (editingPlanId) {
                await axios.put(`${API_BASE_URL}/api/admin/plans/${editingPlanId}`, payload, { headers: getAuthHeaders() });
                setFeedback("Plano atualizado.");
            } else {
                await axios.post(`${API_BASE_URL}/api/admin/plans`, payload, { headers: getAuthHeaders() });
                setFeedback("Plano criado.");
            }
            setPlanForm(emptyPlan);
            setEditingPlanId("");
            await loadAdmin();
        } catch (error) {
            setFeedback(error.response?.data?.message ?? error.response?.data?.Message ?? "Nao foi possivel salvar o plano.");
        }
    }

    async function deletePlan(planId) {
        if (!window.confirm("Tem certeza que deseja excluir/desativar este plano?")) return;
        await axios.delete(`${API_BASE_URL}/api/admin/plans/${planId}`, { headers: getAuthHeaders() });
        setFeedback("Plano removido ou desativado.");
        await loadAdmin();
    }

    const metrics = useMemo(() => [
        { label: "Usuarios", value: dashboard?.totalUsers ?? dashboard?.TotalUsers ?? 0, icon: <FaUsers /> },
        { label: "Times", value: dashboard?.totalTeams ?? dashboard?.TotalTeams ?? 0, icon: <FaShieldAlt /> },
        { label: "Ligas", value: dashboard?.totalLeagues ?? dashboard?.TotalLeagues ?? 0, icon: <FaTrophy /> },
        { label: "Assinaturas ativas", value: dashboard?.activeSubscriptions ?? dashboard?.ActiveSubscriptions ?? 0, icon: <FaCrown /> },
        { label: "Receita assinaturas", value: money(dashboard?.subscriptionRevenue ?? dashboard?.SubscriptionRevenue), icon: <FaMoneyBillWave /> },
        { label: "Receita carteira", value: money(dashboard?.walletRevenue ?? dashboard?.WalletRevenue), icon: <FaMoneyBillWave /> },
    ], [dashboard]);

    if (!isAdmin) {
        return (
            <main className="admin-page">
                <section className="admin-denied">
                    <p className="admin-eyebrow">Painel Admin</p>
                    <h1>Acesso restrito</h1>
                    <span>Sua conta nao tem permissao administrativa.</span>
                </section>
            </main>
        );
    }

    return (
        <main className="admin-page">
            <div className="admin-bg" />
            <div className="admin-container">
                <section className="admin-hero">
                    <div>
                        <p className="admin-eyebrow">Painel Admin</p>
                        <h1>Controle da arena NKZ</h1>
                        <span>Gerencie planos, usuarios, times, ligas, pagamentos e assinaturas em um so lugar.</span>
                    </div>
                    <button type="button" onClick={loadAdmin}>Atualizar dados</button>
                </section>

                {feedback && <section className="admin-feedback">{feedback}</section>}

                <nav className="admin-tabs">
                    {tabs.map((tab) => (
                        <button key={tab} className={activeTab === tab ? "active" : ""} onClick={() => setActiveTab(tab)} type="button">
                            {tab === "overview" ? "Resumo" : tab === "plans" ? "Planos" : tab === "users" ? "Usuarios" : tab === "teams" ? "Times" : tab === "leagues" ? "Ligas" : tab === "payments" ? "Pagamentos" : "Assinaturas"}
                        </button>
                    ))}
                </nav>

                {loading ? <section className="admin-panel">Carregando...</section> : (
                    <>
                        {activeTab === "overview" && (
                            <section className="admin-grid">
                                {metrics.map((metric) => (
                                    <article className="admin-metric" key={metric.label}>
                                        {metric.icon}
                                        <span>{metric.label}</span>
                                        <strong>{metric.value}</strong>
                                    </article>
                                ))}
                            </section>
                        )}

                        {activeTab === "plans" && (
                            <section className="admin-split">
                                <form className="admin-panel admin-form" onSubmit={savePlan}>
                                    <h2>{editingPlanId ? "Editar plano" : "Novo plano"}</h2>
                                    <label>Nome<input value={planForm.name} onChange={(event) => setPlanForm({ ...planForm, name: event.target.value })} maxLength={80} required /></label>
                                    <label>Descricao<textarea value={planForm.description} onChange={(event) => setPlanForm({ ...planForm, description: event.target.value })} maxLength={500} required /></label>
                                    <div className="admin-form-row">
                                        <label>Preco<input type="number" min="0" step="0.01" value={planForm.price} onChange={(event) => setPlanForm({ ...planForm, price: event.target.value })} /></label>
                                        <label>Meses<input type="number" min="1" max="36" value={planForm.durationMonths} onChange={(event) => setPlanForm({ ...planForm, durationMonths: event.target.value })} /></label>
                                        <label>Ordem<input type="number" value={planForm.sortOrder} onChange={(event) => setPlanForm({ ...planForm, sortOrder: event.target.value })} /></label>
                                    </div>
                                    <label>Beneficios<textarea value={planForm.benefits} onChange={(event) => setPlanForm({ ...planForm, benefits: event.target.value })} placeholder="Um beneficio por linha" maxLength={1000} /></label>
                                    <div className="admin-checks">
                                        <label><input type="checkbox" checked={planForm.isActive} onChange={(event) => setPlanForm({ ...planForm, isActive: event.target.checked })} /> Ativo</label>
                                        <label><input type="checkbox" checked={planForm.isFeatured} onChange={(event) => setPlanForm({ ...planForm, isFeatured: event.target.checked })} /> Destaque</label>
                                    </div>
                                    <button type="submit">{editingPlanId ? "Salvar alteracoes" : "Criar plano"}</button>
                                    {editingPlanId && <button type="button" className="admin-secondary" onClick={() => { setEditingPlanId(""); setPlanForm(emptyPlan); }}>Cancelar edicao</button>}
                                </form>
                                <section className="admin-panel admin-list">
                                    <h2>Planos cadastrados</h2>
                                    {(lists.plans || []).map((plan) => {
                                        const id = plan.id ?? plan.Id;
                                        return (
                                            <article key={id}>
                                                <div>
                                                    <strong>{plan.name ?? plan.Name}</strong>
                                                    <span>{money(plan.price ?? plan.Price)} - {plan.durationMonths ?? plan.DurationMonths} mes(es)</span>
                                                </div>
                                                <em>{(plan.isActive ?? plan.IsActive) ? "Ativo" : "Inativo"}</em>
                                                <button type="button" onClick={() => startEditPlan(plan)}>Editar</button>
                                                <button type="button" onClick={() => deletePlan(id)}>Excluir</button>
                                            </article>
                                        );
                                    })}
                                </section>
                            </section>
                        )}

                        {activeTab !== "overview" && activeTab !== "plans" && (
                            <section className="admin-panel admin-table">
                                <h2>{activeTab}</h2>
                                {(lists[activeTab] || []).length ? (lists[activeTab] || []).map((item, index) => (
                                    <article key={item.id ?? item.Id ?? index}>
                                        {Object.entries(item).slice(0, 8).map(([key, value]) => (
                                            <span key={key}><small>{key}</small>{key.toLowerCase().includes("amount") || key.toLowerCase().includes("balance") || key.toLowerCase().includes("revenue") ? money(value) : key.toLowerCase().includes("date") || key.toLowerCase().includes("createdat") || key.toLowerCase().includes("endsat") ? date(value) : String(value ?? "-")}</span>
                                        ))}
                                    </article>
                                )) : <p>Nenhum registro encontrado.</p>}
                            </section>
                        )}
                    </>
                )}
            </div>
        </main>
    );
}
