import { useEffect, useMemo, useState } from "react";
import axios from "axios";
import { FaCrown, FaMoneyBillWave, FaShieldAlt, FaTrophy, FaUsers } from "react-icons/fa";
import { getAuthHeaders, getCurrentUser } from "../../utils/auth";
import "./style.css";

const API_BASE_URL = import.meta.env.VITE_API_URL || "";
const tabs = ["overview", "plans", "users", "teams", "leagues", "payments", "subscriptions"];

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

function unwrap(response) {
    return response?.data?.data ?? response?.data?.Data ?? response?.data ?? null;
}

function getValue(item, key, fallback = "") {
    return item?.[key] ?? item?.[key.charAt(0).toUpperCase() + key.slice(1)] ?? fallback;
}

function getId(item) {
    return getValue(item, "id");
}

function money(value) {
    return Number(value || 0).toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

function date(value) {
    if (!value) return "-";
    return new Date(value).toLocaleDateString("pt-BR");
}

function datetimeLocal(value) {
    if (!value) return "";
    const dateValue = new Date(value);
    if (Number.isNaN(dateValue.getTime())) return "";
    return new Date(dateValue.getTime() - dateValue.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
}

function toIsoOrNull(value) {
    return value ? new Date(value).toISOString() : null;
}

function formatValue(key, value) {
    const lowerKey = key.toLowerCase();
    if (lowerKey.includes("amount") || lowerKey.includes("balance") || lowerKey.includes("revenue") || lowerKey.includes("fee") || lowerKey.includes("award")) {
        return money(value);
    }
    if (lowerKey.includes("date") || lowerKey.includes("createdat") || lowerKey.includes("endsat") || lowerKey.includes("startsat")) {
        return date(value);
    }
    return String(value ?? "-");
}

function normalizeUser(item) {
    return {
        email: getValue(item, "email"),
        role: getValue(item, "role", "User"),
        discordUsername: getValue(item, "discordUsername"),
        discordVerified: Boolean(getValue(item, "discordVerified", false)),
        emailVerified: Boolean(getValue(item, "emailVerified", false)),
        walletBalance: Number(getValue(item, "walletBalance", 0)),
    };
}

function normalizeTeam(item) {
    return {
        name: getValue(item, "name"),
        tag: getValue(item, "tag"),
        ownerId: getValue(item, "ownerId") || "",
        isRecruiting: Boolean(getValue(item, "isRecruiting", false)),
        points: Number(getValue(item, "points", 0)),
    };
}

function normalizeLeague(item) {
    return {
        name: getValue(item, "name"),
        modality: getValue(item, "modality", "Ranking"),
        entryFee: Number(getValue(item, "entryFee", 0)),
        award: Number(getValue(item, "award", 0)),
        minimumTeamPoints: Number(getValue(item, "minimumTeamPoints", 0)),
        maximumTeamPoints: Number(getValue(item, "maximumTeamPoints", 999999)),
        maxTeams: Number(getValue(item, "maxTeams", 16)),
        minimumElo: getValue(item, "minimumElo", "UNRANKED"),
        maximumElo: getValue(item, "maximumElo", "CHALLENGER"),
        imageUrl: getValue(item, "imageUrl"),
        startDate: datetimeLocal(getValue(item, "startDate")),
        endDate: datetimeLocal(getValue(item, "endDate")),
        rankingQueueOpenTime: getValue(item, "rankingQueueOpenTime") || "",
        rankingQueueCloseTime: getValue(item, "rankingQueueCloseTime") || "",
    };
}

const editableConfig = {
    users: {
        title: "Usuarios",
        endpoint: "users",
        normalize: normalizeUser,
        fields: [
            { key: "email", label: "Email", type: "email" },
            { key: "role", label: "Perfil", type: "select", options: ["User", "Admin"] },
            { key: "discordUsername", label: "Discord" },
            { key: "walletBalance", label: "Saldo", type: "number", step: "0.01" },
            { key: "discordVerified", label: "Discord verificado", type: "checkbox" },
            { key: "emailVerified", label: "Email verificado", type: "checkbox" },
        ],
        meta: (item) => [
            ["ID", getId(item)],
            ["Player", getValue(item, "playerName", "-")],
            ["Criado em", date(getValue(item, "createdAt"))],
        ],
    },
    teams: {
        title: "Times",
        endpoint: "teams",
        normalize: normalizeTeam,
        fields: [
            { key: "name", label: "Nome" },
            { key: "tag", label: "Tag", maxLength: 5 },
            { key: "ownerId", label: "Owner ID" },
            { key: "points", label: "Pontos", type: "number" },
            { key: "isRecruiting", label: "Recrutando", type: "checkbox" },
        ],
        meta: (item) => [
            ["ID", getId(item)],
            ["Jogadores", getValue(item, "players", 0)],
        ],
    },
    leagues: {
        title: "Ligas",
        endpoint: "leagues",
        normalize: normalizeLeague,
        fields: [
            { key: "name", label: "Nome" },
            { key: "modality", label: "Modalidade", type: "select", options: ["Ranking", "Chaveamento"] },
            { key: "entryFee", label: "Entrada", type: "number", step: "0.01" },
            { key: "award", label: "Premio", type: "number", step: "0.01" },
            { key: "minimumTeamPoints", label: "Pontos min.", type: "number" },
            { key: "maximumTeamPoints", label: "Pontos max.", type: "number" },
            { key: "maxTeams", label: "Max times", type: "number" },
            { key: "minimumElo", label: "Elo min." },
            { key: "maximumElo", label: "Elo max." },
            { key: "imageUrl", label: "Imagem URL" },
            { key: "startDate", label: "Inicio", type: "datetime-local" },
            { key: "endDate", label: "Fim", type: "datetime-local" },
            { key: "rankingQueueOpenTime", label: "Fila abre", type: "time" },
            { key: "rankingQueueCloseTime", label: "Fila fecha", type: "time" },
        ],
        meta: (item) => [
            ["ID", getId(item)],
            ["Times", getValue(item, "teams", 0)],
        ],
    },
};

export default function AdminPage() {
    const currentUser = getCurrentUser();
    const [activeTab, setActiveTab] = useState("overview");
    const [dashboard, setDashboard] = useState(null);
    const [lists, setLists] = useState({});
    const [planForm, setPlanForm] = useState(emptyPlan);
    const [editingPlanId, setEditingPlanId] = useState("");
    const [editForms, setEditForms] = useState({});
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
        } catch (error) {
            setFeedback(error.response?.data?.message ?? error.response?.data?.Message ?? "Nao foi possivel carregar o painel admin.");
        } finally {
            setLoading(false);
        }
    }

    function startEditPlan(plan) {
        setEditingPlanId(getId(plan));
        setPlanForm({
            name: getValue(plan, "name"),
            description: getValue(plan, "description"),
            price: getValue(plan, "price", 0),
            durationMonths: getValue(plan, "durationMonths", 1),
            benefits: getValue(plan, "benefits"),
            isActive: getValue(plan, "isActive", true),
            isFeatured: getValue(plan, "isFeatured", false),
            sortOrder: getValue(plan, "sortOrder", 0),
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

    function getDraft(type, item) {
        const id = getId(item);
        return editForms[type]?.[id] ?? editableConfig[type].normalize(item);
    }

    function setDraftValue(type, id, key, value) {
        setEditForms((current) => ({
            ...current,
            [type]: {
                ...(current[type] ?? {}),
                [id]: {
                    ...(current[type]?.[id] ?? {}),
                    [key]: value,
                },
            },
        }));
    }

    async function saveRecord(type, item) {
        const id = getId(item);
        const config = editableConfig[type];
        const draft = getDraft(type, item);
        const payload = {
            ...draft,
            ownerId: draft.ownerId || null,
            startDate: toIsoOrNull(draft.startDate),
            endDate: toIsoOrNull(draft.endDate),
            walletBalance: Number(draft.walletBalance ?? 0),
            points: Number(draft.points ?? 0),
            entryFee: Number(draft.entryFee ?? 0),
            award: Number(draft.award ?? 0),
            minimumTeamPoints: Number(draft.minimumTeamPoints ?? 0),
            maximumTeamPoints: Number(draft.maximumTeamPoints ?? 0),
            maxTeams: Number(draft.maxTeams ?? 2),
            rankingQueueOpenTime: draft.rankingQueueOpenTime || null,
            rankingQueueCloseTime: draft.rankingQueueCloseTime || null,
        };

        try {
            await axios.put(`${API_BASE_URL}/api/admin/${config.endpoint}/${id}`, payload, { headers: getAuthHeaders() });
            setFeedback(`${config.title.slice(0, -1)} atualizado.`);
            await loadAdmin();
        } catch (error) {
            setFeedback(error.response?.data?.message ?? error.response?.data?.Message ?? "Nao foi possivel salvar as alteracoes.");
        }
    }

    async function deleteRecord(type, item) {
        const id = getId(item);
        const config = editableConfig[type];
        if (!window.confirm(`Tem certeza que deseja excluir este registro de ${config.title.toLowerCase()}?`)) return;

        try {
            await axios.delete(`${API_BASE_URL}/api/admin/${config.endpoint}/${id}`, { headers: getAuthHeaders() });
            setFeedback("Registro excluido.");
            await loadAdmin();
        } catch (error) {
            setFeedback(error.response?.data?.message ?? error.response?.data?.Message ?? "Nao foi possivel excluir o registro.");
        }
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
                                        const id = getId(plan);
                                        return (
                                            <article key={id}>
                                                <div>
                                                    <strong>{getValue(plan, "name")}</strong>
                                                    <span>{money(getValue(plan, "price"))} - {getValue(plan, "durationMonths")} mes(es)</span>
                                                </div>
                                                <em>{getValue(plan, "isActive", true) ? "Ativo" : "Inativo"}</em>
                                                <button type="button" onClick={() => startEditPlan(plan)}>Editar</button>
                                                <button type="button" onClick={() => deletePlan(id)}>Excluir</button>
                                            </article>
                                        );
                                    })}
                                </section>
                            </section>
                        )}

                        {editableConfig[activeTab] && (
                            <EditableAdminSection
                                config={editableConfig[activeTab]}
                                items={lists[activeTab] || []}
                                getDraft={(item) => getDraft(activeTab, item)}
                                onChange={(id, key, value) => setDraftValue(activeTab, id, key, value)}
                                onSave={(item) => saveRecord(activeTab, item)}
                                onDelete={(item) => deleteRecord(activeTab, item)}
                            />
                        )}

                        {activeTab !== "overview" && activeTab !== "plans" && !editableConfig[activeTab] && (
                            <section className="admin-panel admin-table">
                                <h2>{activeTab}</h2>
                                {(lists[activeTab] || []).length ? (lists[activeTab] || []).map((item, index) => (
                                    <article key={getId(item) || index}>
                                        {Object.entries(item).slice(0, 8).map(([key, value]) => (
                                            <span key={key}><small>{key}</small>{formatValue(key, value)}</span>
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

function EditableAdminSection({ config, items, getDraft, onChange, onSave, onDelete }) {
    return (
        <section className="admin-panel admin-edit-list">
            <h2>{config.title}</h2>
            {items.length ? items.map((item) => {
                const id = getId(item);
                const draft = getDraft(item);
                return (
                    <article className="admin-edit-card" key={id}>
                        <div className="admin-edit-meta">
                            {config.meta(item).map(([label, value]) => (
                                <span key={label}><small>{label}</small>{value || "-"}</span>
                            ))}
                        </div>
                        <div className="admin-edit-fields">
                            {config.fields.map((field) => (
                                <label className={field.type === "checkbox" ? "admin-edit-check" : ""} key={field.key}>
                                    <small>{field.label}</small>
                                    {field.type === "select" ? (
                                        <select value={draft[field.key] ?? ""} onChange={(event) => onChange(id, field.key, event.target.value)}>
                                            {field.options.map((option) => <option key={option} value={option}>{option}</option>)}
                                        </select>
                                    ) : field.type === "checkbox" ? (
                                        <input type="checkbox" checked={Boolean(draft[field.key])} onChange={(event) => onChange(id, field.key, event.target.checked)} />
                                    ) : (
                                        <input
                                            type={field.type ?? "text"}
                                            step={field.step}
                                            maxLength={field.maxLength}
                                            value={draft[field.key] ?? ""}
                                            onChange={(event) => onChange(id, field.key, event.target.value)}
                                        />
                                    )}
                                </label>
                            ))}
                        </div>
                        <div className="admin-edit-actions">
                            <button type="button" onClick={() => onSave(item)}>Salvar</button>
                            <button className="admin-danger" type="button" onClick={() => onDelete(item)}>Excluir</button>
                        </div>
                    </article>
                );
            }) : <p>Nenhum registro encontrado.</p>}
        </section>
    );
}
