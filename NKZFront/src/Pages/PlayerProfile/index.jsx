import { useEffect, useState } from "react";
import axios from "axios";
import { useNavigate, useParams } from "react-router-dom";
import { BsArrowLeft, BsController, BsDiscord, BsStars } from "react-icons/bs";
import RankEmblem from "../../Components/RankEmblem";
import { calculateRankPoints, calculateWinRate, normalizeEloLabel } from "../../utils/elo";
import { getAuthHeaders } from "../../utils/auth";
import { getPlayerImageUrl } from "../../utils/images";
import "../Players/style.css";

const API_BASE_URL = import.meta.env.VITE_API_URL || "";

function unwrapApiData(responseData) {
    return responseData?.data ?? responseData?.Data ?? responseData ?? null;
}

function normalizePlayer(player) {
    const tier = player?.soloQueueTier ?? player?.SoloQueueTier ?? "UNRANKED";
    const rank = player?.soloQueueRank ?? player?.SoloQueueRank ?? "";
    const lp = player?.soloQueueLP ?? player?.SoloQueueLP ?? 0;
    const wins = player?.wins ?? player?.Wins ?? 0;
    const losses = player?.losses ?? player?.Losses ?? 0;
    const matches = player?.totalMatches ?? player?.TotalMatches ?? wins + losses;
    const championStats = player?.championStats ?? player?.ChampionStats ?? [];
    const roleStats = player?.roleStats ?? player?.RoleStats ?? [];
    const matchHistory = player?.matchHistory ?? player?.MatchHistory ?? [];

    return {
        id: player?.id ?? player?.Id,
        nick: player?.summonerName ?? player?.SummonerName ?? "Invocador",
        discordUsername: player?.discordUsername ?? player?.DiscordUsername ?? "",
        profileImageUrl: getPlayerImageUrl(player),
        rank: `${normalizeEloLabel(tier)} ${rank}`.trim(),
        tier,
        mainRole: player?.mainRole ?? player?.MainRole ?? "Flex",
        lookingForTeam: player?.lookingForTeam ?? player?.LookingForTeam ?? true,
        tags: String(player?.tags ?? player?.Tags ?? "").split(",").map((tag) => tag.trim()).filter(Boolean),
        points: calculateRankPoints(tier, rank, lp),
        winRate: matches ? calculateWinRate(wins, matches - wins) : calculateWinRate(wins, losses),
        champions: championStats.map((champion) => ({
            name: champion.championName ?? champion.ChampionName ?? champion.name ?? "Campeao",
            matches: champion.matches ?? champion.Matches ?? 0,
            winRate: champion.winRate ?? champion.WinRate ?? 0,
        })),
        roles: roleStats.map((role) => ({
            role: role.role ?? role.Role ?? "Flex",
            matches: role.matches ?? role.Matches ?? 0,
            winRate: role.winRate ?? role.WinRate ?? 0,
        })),
        matches: matchHistory.slice(0, 8).map((match) => ({
            id: match.id ?? match.Id,
            champion: match.championName ?? match.ChampionName ?? "Campeao",
            role: match.role ?? match.Role ?? "Flex",
            result: match.win ?? match.Win ? "Vitoria" : "Derrota",
            kda: `${match.kills ?? match.Kills ?? 0}/${match.deaths ?? match.Deaths ?? 0}/${match.assists ?? match.Assists ?? 0}`,
            date: match.playedAt ?? match.PlayedAt,
        })),
    };
}

export default function PlayerProfilePage() {
    const { playerId } = useParams();
    const navigate = useNavigate();
    const [player, setPlayer] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    useEffect(() => {
        let isMounted = true;

        async function loadPlayer() {
            try {
                setLoading(true);
                setError("");
                const response = await axios.get(`${API_BASE_URL}/api/player/${playerId}`, { headers: getAuthHeaders() });
                if (isMounted) setPlayer(normalizePlayer(unwrapApiData(response.data)));
            } catch (requestError) {
                if (isMounted) setError(requestError?.response?.data?.message || "Erro ao carregar perfil.");
            } finally {
                if (isMounted) setLoading(false);
            }
        }

        loadPlayer();

        return () => {
            isMounted = false;
        };
    }, [playerId]);

    return (
        <main className="players-page">
            <div className="players-bg-grid" />
            <div className="players-container">
                <button className="player-profile-back" onClick={() => navigate("/players")} type="button">
                    <BsArrowLeft /> Jogadores
                </button>
                {loading && <section className="players-state">Carregando perfil...</section>}
                {error && !loading && <section className="players-state players-state-error">{error}</section>}
                {player && !loading && !error && (
                    <>
                        <section className="player-profile-hero">
                            <div className="player-avatar large">
                                <span>{player.nick.slice(0, 2).toUpperCase()}</span>
                                {player.profileImageUrl && (
                                    <img
                                        src={player.profileImageUrl}
                                        alt={player.nick}
                                        onError={(event) => {
                                            event.currentTarget.style.display = "none";
                                        }}
                                    />
                                )}
                            </div>
                            <div>
                                <p className="players-eyebrow">{player.mainRole}</p>
                                <h1>{player.nick}</h1>
                                <div className="player-profile-meta">
                                    <span><RankEmblem tier={player.tier} label={player.rank} className="compact" /> {player.rank}</span>
                                    <span><BsStars /> {player.points} pontos</span>
                                    <span><BsController /> {player.winRate}% win rate</span>
                                    {player.discordUsername && <span><BsDiscord /> {player.discordUsername}</span>}
                                </div>
                                <em className={`player-looking-badge ${player.lookingForTeam ? "active" : ""}`}>
                                    {player.lookingForTeam ? "Procurando time" : "Nao procurando time"}
                                </em>
                            </div>
                        </section>

                        <section className="player-profile-grid">
                            <article className="player-profile-card">
                                <h2>Tags</h2>
                                <div className="player-champion-list">
                                    {(player.tags.length ? player.tags : ["Sem tags"]).map((tag) => <span key={tag}>{tag}</span>)}
                                </div>
                            </article>
                            <article className="player-profile-card">
                                <h2>Campeoes mais usados</h2>
                                {player.champions.length ? player.champions.map((champion) => (
                                    <div key={champion.name} className="profile-row">
                                        <strong>{champion.name}</strong>
                                        <span>{champion.matches} partidas</span>
                                        <em>{champion.winRate}% WR</em>
                                    </div>
                                )) : <p className="players-empty">Sem dados de campeoes.</p>}
                            </article>
                            <article className="player-profile-card">
                                <h2>Desempenho por rota</h2>
                                {player.roles.length ? player.roles.map((role) => (
                                    <div key={role.role} className="profile-row">
                                        <strong>{role.role}</strong>
                                        <span>{role.matches} partidas</span>
                                        <em>{role.winRate}% WR</em>
                                    </div>
                                )) : <p className="players-empty">Sem dados por rota.</p>}
                            </article>
                            <article className="player-profile-card wide">
                                <h2>Historico recente</h2>
                                {player.matches.length ? player.matches.map((match) => (
                                    <div key={match.id || `${match.champion}-${match.date}`} className="profile-row match">
                                        <strong>{match.champion}</strong>
                                        <span>{match.role}</span>
                                        <em>{match.result}</em>
                                        <small>{match.kda}</small>
                                    </div>
                                )) : <p className="players-empty">Sem partidas registradas.</p>}
                            </article>
                        </section>
                    </>
                )}
            </div>
        </main>
    );
}
