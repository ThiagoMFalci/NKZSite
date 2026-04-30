import { useEffect, useMemo, useState } from "react";
import axios from "axios";
import { useLocation } from "react-router-dom";
import { BsCashCoin, BsPlusLg, BsShieldCheck } from "react-icons/bs";
import { getAuthHeaders } from "../../utils/auth";
import "./style.css";

const API_BASE_URL = import.meta.env.VITE_API_URL || "";
const PRESET_AMOUNTS = [10, 25, 50, 100];

function unwrapApiData(responseData) {
    return responseData?.data ?? responseData?.Data ?? responseData ?? null;
}

function money(value) {
    return Number(value || 0).toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

export default function WalletPage() {
    const location = useLocation();
    const [wallet, setWallet] = useState({ balance: 0, transactions: [] });
    const [amount, setAmount] = useState("25");
    const [loading, setLoading] = useState(true);
    const [depositing, setDepositing] = useState(false);
    const [feedback, setFeedback] = useState({ type: "", message: "" });

    const parsedAmount = useMemo(() => Number(String(amount).replace(",", ".")) || 0, [amount]);

    async function loadWallet() {
        try {
            setLoading(true);
            const response = await axios.get(`${API_BASE_URL}/api/auth/User/wallet`, { headers: getAuthHeaders() });
            const data = unwrapApiData(response.data);
            setWallet({
                balance: data?.balance ?? data?.Balance ?? 0,
                transactions: data?.transactions ?? data?.Transactions ?? [],
            });
        } catch (error) {
            setFeedback({ type: "error", message: error?.response?.data?.message || "Nao foi possivel carregar sua carteira." });
        } finally {
            setLoading(false);
        }
    }

    useEffect(() => {
        loadWallet();
    }, []);

    useEffect(() => {
        const params = new URLSearchParams(location.search);
        const paymentId = params.get("payment_id") || params.get("collection_id");
        if (!paymentId) return;

        let cancelled = false;
        async function syncPayment() {
            try {
                await axios.get(`${API_BASE_URL}/api/auth/User/wallet/mercadopago/webhook`, { params: { payment_id: paymentId } });
                if (!cancelled) {
                    setFeedback({ type: "success", message: "Pagamento confirmado. Saldo atualizado." });
                    await loadWallet();
                    window.dispatchEvent(new Event("nkz-wallet-updated"));
                }
            } catch {
                if (!cancelled) setFeedback({ type: "error", message: "Aguardando confirmacao do Mercado Pago." });
            }
        }

        syncPayment();
        return () => {
            cancelled = true;
        };
    }, [location.search]);

    async function handleDeposit(event) {
        event.preventDefault();
        if (parsedAmount <= 0) {
            setFeedback({ type: "error", message: "Informe um valor maior que zero." });
            return;
        }

        try {
            setDepositing(true);
            setFeedback({ type: "", message: "" });
            const response = await axios.post(`${API_BASE_URL}/api/auth/User/wallet/deposit`, { amount: parsedAmount }, { headers: getAuthHeaders() });
            const checkoutUrl = response?.data?.data ?? response?.data?.Data;
            if (checkoutUrl) {
                window.location.href = checkoutUrl;
                return;
            }
            setFeedback({ type: "success", message: "Recarga criada." });
        } catch (error) {
            setFeedback({ type: "error", message: error?.response?.data?.message || "Nao foi possivel criar a recarga." });
        } finally {
            setDepositing(false);
        }
    }

    return (
        <main className="wallet-page">
            <div className="wallet-bg-grid" />
            <div className="wallet-container">
                <section className="wallet-hero">
                    <div>
                        <p className="wallet-eyebrow">Carteira NKZ</p>
                        <h1>Saldo competitivo</h1>
                        <span>Use creditos para entrar em ligas pagas ou receba reembolso quando uma liga for cancelada.</span>
                    </div>
                    <div className="wallet-balance-card">
                        <BsCashCoin />
                        <span>Saldo disponivel</span>
                        <strong>{money(wallet.balance)}</strong>
                    </div>
                </section>

                {feedback.message && <section className={`wallet-feedback ${feedback.type}`}>{feedback.message}</section>}

                <section className="wallet-grid">
                    <form className="wallet-deposit-card" onSubmit={handleDeposit}>
                        <div>
                            <p className="wallet-eyebrow">Recarga</p>
                            <h2>Adicionar saldo</h2>
                        </div>
                        <div className="wallet-presets">
                            {PRESET_AMOUNTS.map((preset) => (
                                <button key={preset} type="button" className={parsedAmount === preset ? "active" : ""} onClick={() => setAmount(String(preset))}>
                                    {money(preset)}
                                </button>
                            ))}
                        </div>
                        <label>
                            Valor personalizado
                            <input type="number" min="1" step="0.01" value={amount} onChange={(event) => setAmount(event.target.value)} />
                        </label>
                        <button type="submit" disabled={depositing}>
                            <BsPlusLg /> {depositing ? "Criando checkout..." : "Recarregar com Mercado Pago"}
                        </button>
                    </form>

                    <section className="wallet-history-card">
                        <div>
                            <p className="wallet-eyebrow">Historico</p>
                            <h2>Movimentacoes</h2>
                        </div>
                        {loading ? (
                            <span className="wallet-muted">Carregando...</span>
                        ) : wallet.transactions.length ? (
                            <div className="wallet-transactions">
                                {wallet.transactions.map((transaction) => (
                                    <article key={transaction.id ?? transaction.Id}>
                                        <BsShieldCheck />
                                        <div>
                                            <strong>{transaction.description ?? transaction.Description}</strong>
                                            <span>{new Date(transaction.createdAt ?? transaction.CreatedAt).toLocaleString("pt-BR")}</span>
                                        </div>
                                        <em className={Number(transaction.amount ?? transaction.Amount) >= 0 ? "credit" : "debit"}>
                                            {money(transaction.amount ?? transaction.Amount)}
                                        </em>
                                    </article>
                                ))}
                            </div>
                        ) : (
                            <span className="wallet-muted">Nenhuma movimentacao ainda.</span>
                        )}
                    </section>
                </section>
            </div>
        </main>
    );
}
