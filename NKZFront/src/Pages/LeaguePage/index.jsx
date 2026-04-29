import { useEffect, useMemo, useState } from "react";
import axios from "axios";
import { useNavigate, useParams } from "react-router-dom";
import {
    BsBarChartFill,
    BsCalendarEvent,
    BsClockHistory,
    BsDiagram3Fill,
    BsImage,
    BsShieldLockFill,
    BsShieldFillCheck,
    BsTrophyFill,
    BsTrash3Fill,
} from "react-icons/bs";
import { getAuthHeaders, getCurrentUser } from "../../utils/auth";
import { getPlayerImageUrl, resolveAssetUrl } from "../../utils/images";
import "./style.css";

const API_BASE_URL = import.meta.env.VITE_API_URL || "";
const TAB_ITEMS = [
    { id: "standings", label: "Classificacao", icon: BsTrophyFill },
    { id: "calendar", label: "Calendario", icon: BsCalendarEvent },
    { id: "stats", label: "Estatisticas", icon: BsBarChartFill },
    { id: "history", label: "Historico", icon: BsClockHistory },
    { id: "playoff", label: "Playoff", icon: BsDiagram3Fill },
];
const ROUND_ORDER = ["U-R16", "U-QF", "U-SF", "U-F", "L-R1", "L-R2", "L-R3", "L-R4", "L-R5", "L-F", "GF"];

function unwrapApiData(responseData) {
    return responseData?.data ?? responseData?.Data ?? responseData ?? null;
}

function normalizePlayer(player) {
    const history = player.matchHistory ?? player.MatchHistory ?? [];
    return {
        id: player.id ?? player.Id,
        userId: player.userId ?? player.UserId,
        isCaptain: player.isCaptain ?? player.IsCaptain ?? false,
        nick: player.summonerName ?? player.SummonerName ?? "Invocador",
        role: player.mainRole ?? player.MainRole ?? player.role ?? player.Role ?? "Flex",
        imageUrl: getPlayerImageUrl(player),
        history: Array.isArray(history) ? history : [],
    };
}

function normalizeTeam(team) {
    const players = team.players ?? team.Players ?? [];
    return {
        id: team.id ?? team.Id,
        name: team.name ?? team.Name ?? "Time",
        tag: team.tag ?? team.Tag ?? "TIME",
        ownerId: team.ownerId ?? team.OwnerId,
        imageUrl: resolveAssetUrl(team.profileImageUrl ?? team.ProfileImageUrl ?? ""),
        players: Array.isArray(players) ? players.map(normalizePlayer) : [],
    };
}

function normalizeMatch(match) {
    return {
        id: match.id ?? match.Id,
        leagueId: match.leagueId ?? match.LeagueId,
        bracket: match.bracket ?? match.Bracket ?? "Upper",
        roundKey: match.roundKey ?? match.RoundKey ?? "",
        roundName: match.roundName ?? match.RoundName ?? "Rodada",
        weekNumber: match.weekNumber ?? match.WeekNumber ?? 1,
        matchNumber: match.matchNumber ?? match.MatchNumber ?? 0,
        bestOf: match.bestOf ?? match.BestOf ?? 1,
        teamAId: match.teamAId ?? match.TeamAId,
        teamBId: match.teamBId ?? match.TeamBId,
        winnerTeamId: match.winnerTeamId ?? match.WinnerTeamId,
        loserTeamId: match.loserTeamId ?? match.LoserTeamId,
        teamAScore: match.teamAScore ?? match.TeamAScore ?? 0,
        teamBScore: match.teamBScore ?? match.TeamBScore ?? 0,
        scheduledAt: match.scheduledAt ?? match.ScheduledAt,
        completedAt: match.completedAt ?? match.CompletedAt,
        status: match.status ?? match.Status ?? "Pending",
        reports: (match.reports ?? match.Reports ?? []).map((report) => ({
            id: report.id ?? report.Id,
            teamId: report.teamId ?? report.TeamId,
            reportedWinnerTeamId: report.reportedWinnerTeamId ?? report.ReportedWinnerTeamId,
            proofImageUrl: resolveAssetUrl(report.proofImageUrl ?? report.ProofImageUrl ?? ""),
            updatedAt: report.updatedAt ?? report.UpdatedAt,
        })),
    };
}

function normalizeStanding(standing) {
    return {
        id: standing.id ?? standing.Id,
        leagueId: standing.leagueId ?? standing.LeagueId,
        teamId: standing.teamId ?? standing.TeamId,
        wins: standing.wins ?? standing.Wins ?? 0,
        losses: standing.losses ?? standing.Losses ?? 0,
        mapsPlayed: standing.mapsPlayed ?? standing.MapsPlayed ?? 0,
        mapDiff: standing.mapDiff ?? standing.MapDiff ?? 0,
        penalties: standing.penalties ?? standing.Penalties ?? 0,
    };
}

function normalizeLeague(league) {
    const teams = (league.teams ?? league.Teams ?? []).map(normalizeTeam);
    const matches = (league.matches ?? league.Matches ?? []).map(normalizeMatch);
    const standings = (league.standings ?? league.Standings ?? []).map(normalizeStanding);
    return {
        id: league.id ?? league.Id,
        name: league.name ?? league.Name ?? "Liga",
        award: league.award ?? league.Award ?? 0,
        maxTeams: league.maxTeams ?? league.MaxTeams ?? 16,
        minimumElo: league.minimumElo ?? league.MinimumElo ?? "UNRANKED",
        maximumElo: league.maximumElo ?? league.MaximumElo ?? "CHALLENGER",
        startDate: league.startDate ?? league.StartDate,
        endDate: league.endDate ?? league.EndDate,
        modality: league.modality ?? league.Modality ?? "Chaveamento",
        teams,
        matches,
        standings,
    };
}

function formatDate(value) {
    if (!value) return "A definir";
    return new Date(value).toLocaleDateString("pt-BR", { day: "2-digit", month: "short", year: "numeric" });
}

function formatElo(value) {
    const label = String(value || "UNRANKED").toLowerCase();
    return label.charAt(0).toUpperCase() + label.slice(1);
}

function getKdaLabel(entry) {
    return `${entry.kills}/${entry.deaths}/${entry.assists}`;
}

export default function LeaguePage() {
    const { leagueId } = useParams();
    const navigate = useNavigate();
    const [league, setLeague] = useState(null);
    const [ownedTeam, setOwnedTeam] = useState(null);
    const [activeTab, setActiveTab] = useState("standings");
    const [loading, setLoading] = useState(true);
    const [actionLoading, setActionLoading] = useState("");
    const [proofFiles, setProofFiles] = useState({});
    const [feedback, setFeedback] = useState({ type: "", message: "" });
    const currentUser = getCurrentUser();
    const isAdmin = String(currentUser?.role || "").includes("Admin");

    async function loadLeague() {
        try {
            setLoading(true);
            const response = await axios.get(`${API_BASE_URL}/api/league/${leagueId}`, { headers: getAuthHeaders() });
            setLeague(normalizeLeague(unwrapApiData(response.data)));
        } catch (requestError) {
            setFeedback({
                type: "error",
                message: requestError?.response?.data?.message || "Nao foi possivel carregar a liga.",
            });
        } finally {
            setLoading(false);
        }
    }

    async function loadOwnedTeam() {
        if (!currentUser?.userId) return;
        try {
            const response = await axios.get(`${API_BASE_URL}/api/team/ListTeams`, { headers: getAuthHeaders() });
            const teams = unwrapApiData(response.data);
            const normalizedTeams = (Array.isArray(teams) ? teams : []).map(normalizeTeam);
            const manageableTeam = normalizedTeams.find((team) => (
                team.ownerId === currentUser.userId ||
                team.players.some((player) => player.userId === currentUser.userId && player.isCaptain)
            ));
            setOwnedTeam(manageableTeam || null);
        } catch {
            setOwnedTeam(null);
        }
    }

    useEffect(() => {
        loadLeague();
        loadOwnedTeam();
    }, [leagueId]);

    const teamById = useMemo(() => {
        const map = new Map();
        league?.teams.forEach((team) => map.set(team.id, team));
        return map;
    }, [league]);

    const standings = useMemo(() => {
        if (!league) return [];
        const base = league.standings.length
            ? league.standings
            : league.teams.map((team) => ({ teamId: team.id, wins: 0, losses: 0, mapsPlayed: 0, mapDiff: 0, penalties: 0 }));

        return base
            .map((standing) => ({ ...standing, team: teamById.get(standing.teamId) }))
            .filter((standing) => standing.team)
            .sort((a, b) => b.wins - a.wins || b.mapDiff - a.mapDiff || a.penalties - b.penalties || a.losses - b.losses);
    }, [league, teamById]);

    const matchesByRound = useMemo(() => {
        const grouped = new Map();
        (league?.matches || [])
            .sort((a, b) => ROUND_ORDER.indexOf(a.roundKey) - ROUND_ORDER.indexOf(b.roundKey) || a.matchNumber - b.matchNumber)
            .forEach((match) => {
                if (!grouped.has(match.roundKey)) grouped.set(match.roundKey, []);
                grouped.get(match.roundKey).push(match);
            });
        return Array.from(grouped.entries()).map(([roundKey, matches]) => ({ roundKey, matches, title: matches[0]?.roundName || roundKey }));
    }, [league]);

    const matchesByWeek = useMemo(() => {
        const grouped = new Map();
        (league?.matches || []).forEach((match) => {
            if (!grouped.has(match.weekNumber)) grouped.set(match.weekNumber, []);
            grouped.get(match.weekNumber).push(match);
        });
        return Array.from(grouped.entries())
            .sort(([weekA], [weekB]) => weekA - weekB)
            .map(([week, matches]) => ({
                week,
                date: matches.find((match) => match.scheduledAt)?.scheduledAt,
                matches: matches.sort((a, b) => ROUND_ORDER.indexOf(a.roundKey) - ROUND_ORDER.indexOf(b.roundKey) || a.matchNumber - b.matchNumber),
            }));
    }, [league]);

    const playerStats = useMemo(() => {
        const rows = [];
        league?.teams.forEach((team) => {
            team.players.forEach((player) => {
                const totals = player.history.reduce((acc, match) => ({
                    kills: acc.kills + (match.kills ?? match.Kills ?? 0),
                    deaths: acc.deaths + (match.deaths ?? match.Deaths ?? 0),
                    assists: acc.assists + (match.assists ?? match.Assists ?? 0),
                    games: acc.games + 1,
                }), { kills: 0, deaths: 0, assists: 0, games: 0 });
                if (!totals.games) return;
                rows.push({
                    player,
                    team,
                    ...totals,
                    kda: (totals.kills + totals.assists) / Math.max(1, totals.deaths),
                });
            });
        });
        return rows.sort((a, b) => b.kda - a.kda).slice(0, 12);
    }, [league]);

    const completedMatches = useMemo(() => (
        (league?.matches || [])
            .filter((match) => match.status === "Completed")
            .sort((a, b) => new Date(b.completedAt || 0) - new Date(a.completedAt || 0))
    ), [league]);

    const ownedTeamInLeague = useMemo(() => (
        Boolean(ownedTeam?.id && league?.teams.some((team) => team.id === ownedTeam.id))
    ), [league, ownedTeam]);

    const leagueEntryDisabled = actionLoading === "join" || actionLoading === "leave" || Boolean(league?.matches.length && !ownedTeamInLeague);

    async function handleGeneratePlayoff() {
        try {
            setActionLoading("generate");
            setFeedback({ type: "", message: "" });
            await axios.post(`${API_BASE_URL}/api/league/${league.id}/playoff/generate`, null, { headers: getAuthHeaders() });
            setFeedback({ type: "success", message: "Chaveamento gerado." });
            await loadLeague();
            setActiveTab("playoff");
        } catch (requestError) {
            setFeedback({
                type: "error",
                message: requestError?.response?.data?.message || requestError?.response?.data?.Message || "Nao foi possivel gerar o chaveamento.",
            });
        } finally {
            setActionLoading("");
        }
    }

    async function handleJoinLeague() {
        if (!ownedTeam?.id) {
            setFeedback({ type: "error", message: "Voce precisa ser dono ou capitao de um time para entrar." });
            return;
        }

        try {
            setActionLoading("join");
            setFeedback({ type: "", message: "" });
            await axios.post(`${API_BASE_URL}/api/league/${league.id}/teams/${ownedTeam.id}`, null, { headers: getAuthHeaders() });
            setFeedback({ type: "success", message: "Time inscrito na liga." });
            await loadLeague();
        } catch (requestError) {
            setFeedback({
                type: "error",
                message: requestError?.response?.data?.message || requestError?.response?.data?.Message || "Nao foi possivel entrar na liga.",
            });
        } finally {
            setActionLoading("");
        }
    }

    async function handleLeaveLeague() {
        if (!ownedTeam?.id) return;
        if (!window.confirm("Tem certeza que deseja tirar seu time desta liga?")) return;

        try {
            setActionLoading("leave");
            setFeedback({ type: "", message: "" });
            await axios.delete(`${API_BASE_URL}/api/league/${league.id}/teams/${ownedTeam.id}`, { headers: getAuthHeaders() });
            setFeedback({ type: "success", message: "Time removido da liga." });
            await loadLeague();
        } catch (requestError) {
            setFeedback({
                type: "error",
                message: requestError?.response?.data?.message || requestError?.response?.data?.Message || "Nao foi possivel sair da liga.",
            });
        } finally {
            setActionLoading("");
        }
    }

    async function handleRemoveTeam(team) {
        if (!team?.id) return;
        if (league.matches.length) {
            setFeedback({ type: "error", message: "Nao e possivel expulsar times depois que o chaveamento foi gerado." });
            return;
        }

        if (!window.confirm(`Expulsar ${team.name} desta liga?`)) return;

        try {
            setActionLoading(`remove-${team.id}`);
            setFeedback({ type: "", message: "" });
            await axios.delete(`${API_BASE_URL}/api/league/${league.id}/teams/${team.id}`, { headers: getAuthHeaders() });
            setFeedback({ type: "success", message: `${team.name} foi removido da liga.` });
            await loadLeague();
        } catch (requestError) {
            setFeedback({
                type: "error",
                message: requestError?.response?.data?.message || requestError?.response?.data?.Message || "Nao foi possivel remover o time.",
            });
        } finally {
            setActionLoading("");
        }
    }

    async function handleDeleteLeague() {
        if (!window.confirm(`Excluir a liga ${league.name}? Essa acao nao pode ser desfeita.`)) return;

        try {
            setActionLoading("delete-league");
            await axios.delete(`${API_BASE_URL}/api/league/${league.id}`, { headers: getAuthHeaders() });
            navigate("/leagues");
        } catch (requestError) {
            setFeedback({
                type: "error",
                message: requestError?.response?.data?.message || requestError?.response?.data?.Message || "Nao foi possivel excluir a liga.",
            });
        } finally {
            setActionLoading("");
        }
    }

    async function handleCompleteMatch(match, winnerTeamId) {
        const winner = teamById.get(winnerTeamId);
        if (!window.confirm(`Finalizar partida com vitoria de ${winner?.name || "este time"}?`)) return;

        try {
            setActionLoading(match.id);
            setFeedback({ type: "", message: "" });
            await axios.post(`${API_BASE_URL}/api/league/matches/${match.id}/complete`, {
                winnerTeamId,
                teamAScore: winnerTeamId === match.teamAId ? 1 : 0,
                teamBScore: winnerTeamId === match.teamBId ? 1 : 0,
            }, { headers: getAuthHeaders() });
            await loadLeague();
        } catch (requestError) {
            setFeedback({
                type: "error",
                message: requestError?.response?.data?.message || requestError?.response?.data?.Message || "Nao foi possivel finalizar a partida.",
            });
        } finally {
            setActionLoading("");
        }
    }

    async function handleReportMatch(match, winnerTeamId) {
        const proofImage = proofFiles[match.id];
        if (!proofImage) {
            setFeedback({ type: "error", message: "Anexe um print antes de enviar o resultado." });
            return;
        }

        const winner = teamById.get(winnerTeamId);
        if (!window.confirm(`Enviar resultado com vitoria de ${winner?.name || "este time"}?`)) return;

        try {
            setActionLoading(`report-${match.id}`);
            setFeedback({ type: "", message: "" });
            const formData = new FormData();
            formData.append("reportedWinnerTeamId", winnerTeamId);
            formData.append("proofImage", proofImage);
            const response = await axios.post(`${API_BASE_URL}/api/league/matches/${match.id}/report`, formData, {
                headers: { ...getAuthHeaders(), "Content-Type": "multipart/form-data" },
            });
            setFeedback({
                type: "success",
                message: response.data?.message || response.data?.Message || "Resultado enviado.",
            });
            setProofFiles((current) => ({ ...current, [match.id]: null }));
            await loadLeague();
        } catch (requestError) {
            setFeedback({
                type: "error",
                message: requestError?.response?.data?.message || requestError?.response?.data?.Message || "Nao foi possivel enviar o resultado.",
            });
        } finally {
            setActionLoading("");
        }
    }

    function renderTeamName(teamId) {
        const team = teamById.get(teamId);
        return team ? `${team.tag} ${team.name}` : "A definir";
    }

    function renderMatchCard(match) {
        const teamA = teamById.get(match.teamAId);
        const teamB = teamById.get(match.teamBId);
        const isReady = match.teamAId && match.teamBId && match.status !== "Completed";
        const canReport = isReady && ownedTeam?.id && (ownedTeam.id === match.teamAId || ownedTeam.id === match.teamBId);
        const ownReport = match.reports.find((report) => report.teamId === ownedTeam?.id);
        return (
            <article key={match.id} className={`league-match-card ${match.status === "Completed" ? "done" : ""}`}>
                <div className="league-match-meta">
                    <span>MD{match.bestOf}</span>
                    <strong>{match.roundName} #{match.matchNumber}</strong>
                </div>
                {[teamA, teamB].map((team, index) => {
                    const teamId = index === 0 ? match.teamAId : match.teamBId;
                    const score = index === 0 ? match.teamAScore : match.teamBScore;
                    const won = match.winnerTeamId === teamId;
                    return (
                        <div key={`${match.id}-${index}`} className={`league-match-team ${won ? "winner" : ""}`}>
                            <TeamLogo team={team} />
                            <span>{team ? team.name : "A definir"}</span>
                            <strong>{match.status === "Completed" ? score : "-"}</strong>
                        </div>
                    );
                })}
                {isAdmin && isReady && (
                    <div className="league-match-actions">
                        <button type="button" disabled={actionLoading === match.id} onClick={() => handleCompleteMatch(match, match.teamAId)}>
                            Venceu A
                        </button>
                        <button type="button" disabled={actionLoading === match.id} onClick={() => handleCompleteMatch(match, match.teamBId)}>
                            Venceu B
                        </button>
                    </div>
                )}
                {match.reports.length > 0 && (
                    <div className="league-match-reports">
                        {match.reports.map((report) => (
                            <a key={report.id} href={report.proofImageUrl} target="_blank" rel="noreferrer">
                                <BsImage /> {renderTeamName(report.teamId)} indicou {renderTeamName(report.reportedWinnerTeamId)}
                            </a>
                        ))}
                    </div>
                )}
                {canReport && (
                    <div className="league-report-box">
                        <span>{ownReport ? "Voce ja enviou um resultado. Pode corrigir reenviando." : "Enviar resultado do seu time"}</span>
                        <input
                            type="file"
                            accept="image/*"
                            onChange={(event) => setProofFiles((current) => ({ ...current, [match.id]: event.target.files?.[0] || null }))}
                        />
                        <div className="league-match-actions">
                            <button type="button" disabled={actionLoading === `report-${match.id}`} onClick={() => handleReportMatch(match, match.teamAId)}>
                                Venceu A
                            </button>
                            <button type="button" disabled={actionLoading === `report-${match.id}`} onClick={() => handleReportMatch(match, match.teamBId)}>
                                Venceu B
                            </button>
                        </div>
                    </div>
                )}
            </article>
        );
    }

    if (loading) {
        return <main className="league-page"><section className="league-state">Carregando liga...</section></main>;
    }

    if (!league) {
        return <main className="league-page"><section className="league-state error">{feedback.message || "Liga nao encontrada."}</section></main>;
    }

    return (
        <main className="league-page">
            <div className="league-bg-grid" />
            <div className="league-container">
                <section className="league-hero">
                    <div>
                        <p className="league-eyebrow">Liga</p>
                        <h1>{league.name}</h1>
                        <div className="league-hero-tags">
                            <span>{league.teams.length}/{league.maxTeams} times</span>
                            <span>{league.modality}</span>
                            <span>Elo {formatElo(league.minimumElo)} - {formatElo(league.maximumElo)}</span>
                            <span>Inicio {formatDate(league.startDate)}</span>
                            <span>Final {formatDate(league.endDate)}</span>
                        </div>
                    </div>
                    <div className="league-hero-side">
                        <div className="league-entry-card">
                            <div className="league-prize">
                                <span>Premiacao</span>
                                <strong>{Number(league.award || 0).toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}</strong>
                            </div>
                            <div className="league-requirements">
                                <span>Elo minimo <strong>{formatElo(league.minimumElo)}</strong></span>
                                <span>Elo maximo <strong>{formatElo(league.maximumElo)}</strong></span>
                            </div>
                        </div>
                        <button
                            type="button"
                            className={`league-entry-button ${ownedTeamInLeague ? "leave" : ""}`}
                            disabled={leagueEntryDisabled}
                            onClick={ownedTeamInLeague ? handleLeaveLeague : handleJoinLeague}
                        >
                            {ownedTeamInLeague ? "Sair da liga" : actionLoading === "join" ? "Entrando..." : "Entrar na liga"}
                        </button>
                        {!ownedTeam && <small className="league-entry-hint">Voce precisa ser dono ou capitao de um time.</small>}
                    </div>
                </section>

                {feedback.message && <div className={`league-feedback ${feedback.type}`}>{feedback.message}</div>}

                {isAdmin && (
                    <section className="league-admin-panel">
                        <div className="league-admin-header">
                            <div>
                                <p className="league-eyebrow">Administracao</p>
                                <h2>Controle da liga</h2>
                            </div>
                            <button
                                type="button"
                                className="league-danger-button"
                                disabled={actionLoading === "delete-league"}
                                onClick={handleDeleteLeague}
                            >
                                <BsTrash3Fill /> Excluir liga
                            </button>
                        </div>

                        <div className="league-admin-grid">
                            <div className="league-admin-card">
                                <strong><BsShieldLockFill /> Times inscritos</strong>
                                <span>{league.matches.length ? "O playoff ja foi gerado, entao a lista esta travada." : "Voce pode expulsar times antes do chaveamento."}</span>
                                <div className="league-admin-team-list">
                                    {league.teams.length ? league.teams.map((team) => (
                                        <article key={team.id} className="league-admin-team-row">
                                            <TeamLogo team={team} />
                                            <div>
                                                <strong>{team.name}</strong>
                                                <span>{team.tag} - {team.players.length}/5 jogadores</span>
                                            </div>
                                            <button
                                                type="button"
                                                disabled={Boolean(league.matches.length) || actionLoading === `remove-${team.id}`}
                                                onClick={() => handleRemoveTeam(team)}
                                            >
                                                Expulsar
                                            </button>
                                        </article>
                                    )) : <small>Nenhum time inscrito.</small>}
                                </div>
                            </div>

                            <div className="league-admin-card">
                                <strong><BsDiagram3Fill /> Automacao</strong>
                                <span>Com 16 times, o site gera upper/lower bracket automaticamente. O botao abaixo forca a geracao se for preciso.</span>
                                <button
                                    type="button"
                                    className="league-primary-button"
                                    disabled={actionLoading === "generate" || league.teams.length !== 16 || Boolean(league.matches.length)}
                                    onClick={handleGeneratePlayoff}
                                >
                                    {league.matches.length ? "Chaveamento gerado" : actionLoading === "generate" ? "Gerando..." : "Gerar chaveamento"}
                                </button>
                            </div>
                        </div>
                    </section>
                )}

                {!league.matches.length && (
                    <section className="league-setup-panel">
                        <BsDiagram3Fill />
                        <div>
                            <strong>Playoff ainda nao gerado</strong>
                            <span>{league.teams.length === 16 ? "A liga esta pronta para gerar upper e lower bracket." : `Faltam ${Math.max(0, 16 - league.teams.length)} times para fechar as 16 vagas.`}</span>
                        </div>
                        {isAdmin && league.teams.length === 16 && (
                            <button type="button" className="league-primary-button" disabled={actionLoading === "generate"} onClick={handleGeneratePlayoff}>
                                {actionLoading === "generate" ? "Gerando..." : "Gerar chaveamento"}
                            </button>
                        )}
                    </section>
                )}

                <nav className="league-tabs">
                    {TAB_ITEMS.map((tab) => {
                        const Icon = tab.icon;
                        return (
                            <button key={tab.id} type="button" className={activeTab === tab.id ? "active" : ""} onClick={() => setActiveTab(tab.id)}>
                                <Icon /> {tab.label}
                            </button>
                        );
                    })}
                </nav>

                {activeTab === "standings" && (
                    <section className="league-panel">
                        <PanelTitle icon={BsTrophyFill} title="Classificacao atual" subtitle="Vitorias, derrotas, mapas jogados e penalidades." />
                        <div className="standings-list">
                            {standings.map((standing, index) => (
                                <article key={standing.teamId} className="standing-row">
                                    <strong className="standing-place">{index + 1}</strong>
                                    <TeamLogo team={standing.team} />
                                    <div className="standing-team">
                                        <strong>{standing.team.name}</strong>
                                        <span>{standing.team.tag}</span>
                                    </div>
                                    <StatPill value={standing.wins} label="Vitorias" tone="green" />
                                    <StatPill value={standing.losses} label="Derrotas" tone="red" />
                                    <StatPill value={standing.mapsPlayed} label="Mapas" tone="blue" />
                                    <StatPill value={standing.penalties} label="Penalidades" tone="yellow" />
                                </article>
                            ))}
                        </div>
                    </section>
                )}

                {activeTab === "calendar" && (
                    <section className="league-panel">
                        <PanelTitle icon={BsCalendarEvent} title="Calendario" subtitle="Cada semana acompanha a proxima etapa do chaveamento." />
                        <div className="calendar-list">
                            {matchesByWeek.map((week) => (
                                <article key={week.week} className="calendar-week">
                                    <header>
                                        <strong>Semana {week.week}</strong>
                                        <span>{formatDate(week.date)}</span>
                                    </header>
                                    <div className="calendar-matches">
                                        {week.matches.map((match) => (
                                            <div key={match.id} className="calendar-match">
                                                <span>{match.roundName}</span>
                                                <strong>{renderTeamName(match.teamAId)} vs {renderTeamName(match.teamBId)}</strong>
                                                <em>{match.status === "Completed" ? `Vencedor: ${renderTeamName(match.winnerTeamId)}` : match.status}</em>
                                            </div>
                                        ))}
                                    </div>
                                </article>
                            ))}
                        </div>
                    </section>
                )}

                {activeTab === "stats" && (
                    <section className="league-panel">
                        <PanelTitle icon={BsBarChartFill} title="Estatisticas" subtitle="Melhores jogadores por KDA com base no historico sincronizado." />
                        <div className="player-stats-list">
                            {playerStats.length ? playerStats.map((row, index) => (
                                <article key={row.player.id} className="player-stat-row">
                                    <strong className="standing-place">{index + 1}</strong>
                                    <PlayerAvatar player={row.player} />
                                    <div>
                                        <strong>{row.player.nick}</strong>
                                        <span>{row.team.tag} - {row.player.role}</span>
                                    </div>
                                    <StatPill value={row.kda.toFixed(2)} label="KDA" tone="green" />
                                    <StatPill value={getKdaLabel(row)} label={`${row.games} jogos`} tone="blue" />
                                </article>
                            )) : <EmptyState text="Sem historico real de partidas sincronizado para calcular KDA." />}
                        </div>
                    </section>
                )}

                {activeTab === "history" && (
                    <section className="league-panel">
                        <PanelTitle icon={BsClockHistory} title="Historico" subtitle="Ultimas partidas finalizadas no chaveamento." />
                        <div className="history-list">
                            {completedMatches.length ? completedMatches.map((match) => (
                                <article key={match.id} className="history-row">
                                    <span>{formatDate(match.completedAt)}</span>
                                    <strong>{renderTeamName(match.teamAId)} {match.teamAScore} x {match.teamBScore} {renderTeamName(match.teamBId)}</strong>
                                    <em>Vencedor: {renderTeamName(match.winnerTeamId)}</em>
                                </article>
                            )) : <EmptyState text="Nenhuma partida finalizada ainda." />}
                        </div>
                    </section>
                )}

                {activeTab === "playoff" && (
                    <section className="league-panel playoff-panel">
                        <PanelTitle icon={BsDiagram3Fill} title="Playoff" subtitle="Upper bracket, lower bracket e grande final automaticos." />
                        <BracketSection title="Upper bracket" rounds={matchesByRound.filter((round) => round.roundKey.startsWith("U-"))} renderMatchCard={renderMatchCard} />
                        <BracketSection title="Lower bracket" rounds={matchesByRound.filter((round) => round.roundKey.startsWith("L-"))} renderMatchCard={renderMatchCard} />
                        <BracketSection title="Grande final" rounds={matchesByRound.filter((round) => round.roundKey === "GF")} renderMatchCard={renderMatchCard} />
                    </section>
                )}
            </div>
        </main>
    );
}

function PanelTitle({ icon: Icon, title, subtitle }) {
    return (
        <header className="league-panel-title">
            <Icon />
            <div>
                <h2>{title}</h2>
                <p>{subtitle}</p>
            </div>
        </header>
    );
}

function TeamLogo({ team }) {
    return (
        <div className="team-logo">
            <span>{(team?.tag || "?").slice(0, 3).toUpperCase()}</span>
            {team?.imageUrl && (
                <img
                    src={team.imageUrl}
                    alt={team.name}
                    onError={(event) => {
                        event.currentTarget.style.display = "none";
                    }}
                />
            )}
        </div>
    );
}

function PlayerAvatar({ player }) {
    return (
        <div className="team-logo player">
            <span>{(player?.nick || "?").slice(0, 2).toUpperCase()}</span>
            {player?.imageUrl && (
                <img
                    src={player.imageUrl}
                    alt={player.nick}
                    onError={(event) => {
                        event.currentTarget.style.display = "none";
                    }}
                />
            )}
        </div>
    );
}

function StatPill({ value, label, tone }) {
    return (
        <div className={`stat-pill ${tone}`}>
            <strong>{value}</strong>
            <span>{label}</span>
        </div>
    );
}

function BracketSection({ title, rounds, renderMatchCard }) {
    if (!rounds.length) return null;
    return (
        <div className="bracket-section">
            <h3>{title}</h3>
            <div className="bracket-scroll">
                {rounds.map((round) => (
                    <div key={round.roundKey} className="bracket-round">
                        <span className="round-label">{round.title}</span>
                        <div className="bracket-round-matches">
                            {round.matches.map(renderMatchCard)}
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
}

function EmptyState({ text }) {
    return <div className="league-empty-state"><BsShieldFillCheck /> {text}</div>;
}
