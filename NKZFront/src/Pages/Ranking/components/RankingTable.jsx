import { BsTrophyFill } from "react-icons/bs";
import RankEmblem from "../../../Components/RankEmblem";

const EMPTY_MESSAGE = {
    players: "Nenhum jogador encontrado no ranking.",
    teams: "Nenhum time encontrado no ranking.",
    tournaments: "Ainda nao ha campeonatos vencidos registrados.",
};

export default function RankingTable({ rows, type = "players" }) {
    if (!rows.length) {
        return <section className="ranking-state">{EMPTY_MESSAGE[type]}</section>;
    }

    const content = {
        players: {
            className: "",
            head: ["#", "Invocador", "Elo", "Partidas", "Win rate", "Pontos"],
            row: (player, index) => [
                <span className="rank-position">{index < 3 ? <BsTrophyFill /> : index + 1}</span>,
                <span className="ranking-player-cell">
                    <span className="ranking-player-avatar">
                        <span>{String(player.summonerName || "JG").slice(0, 2).toUpperCase()}</span>
                        {player.profileImageUrl && (
                            <img
                                src={player.profileImageUrl}
                                alt={player.summonerName}
                                onError={(event) => {
                                    event.currentTarget.style.display = "none";
                                }}
                            />
                        )}
                    </span>
                    <span><strong>{player.summonerName}</strong><small>Nivel {player.summonerLevel}</small></span>
                </span>,
                <span className="ranking-elo-cell"><RankEmblem tier={player.tier} label={player.elo} className="compact" /> {player.elo}</span>,
                <span>{player.totalMatches}</span>,
                <span>{player.winRate}%</span>,
                <span className="ranking-points">{player.points}</span>,
            ],
        },
        teams: {
            className: "",
            head: ["#", "Time", "Elo medio", "Jogadores", "Vitorias", "Pontos"],
            row: (team, index) => [
                <span className="rank-position">{index < 3 ? <BsTrophyFill /> : index + 1}</span>,
                <span><strong>{team.name}</strong><small>{team.tag}</small></span>,
                <span className="ranking-elo-cell"><RankEmblem tier={team.tier} label={team.elo} className="compact" /> {team.elo}</span>,
                <span>{team.players}/5</span>,
                <span>{team.wins}</span>,
                <span className="ranking-points">{team.points}</span>,
            ],
        },
        tournaments: {
            className: "tournament-wins",
            head: ["#", "Time", "Tag", "Campeonatos vencidos", "Ultimo titulo"],
            row: (team, index) => [
                <span className="rank-position">{index < 3 ? <BsTrophyFill /> : index + 1}</span>,
                <span><strong>{team.name}</strong></span>,
                <span>{team.tag}</span>,
                <span className="ranking-points">{team.wins}</span>,
                <span>{team.lastTournament || "-"}</span>,
            ],
        },
    }[type];

    return (
        <section className="ranking-table-card">
            <div className={`ranking-table ${content.className}`}>
                <div className="ranking-row ranking-head">
                    {content.head.map((item) => <span key={item}>{item}</span>)}
                </div>

                {rows.map((row, index) => (
                    <article key={row.id} className="ranking-row">
                        {content.row(row, index).map((cell, cellIndex) => (
                            <span key={`${row.id}-${cellIndex}`} className="ranking-cell">{cell}</span>
                        ))}
                    </article>
                ))}
            </div>
        </section>
    );
}
