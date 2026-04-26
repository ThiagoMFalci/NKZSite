import { BsCashCoin, BsDiagram3Fill, BsPeopleFill, BsTrophyFill } from "react-icons/bs";

export default function TournamentCard({ tournament, onSelect }) {
    return (
        <article className="tournament-card" onClick={() => onSelect(tournament)} role="button" tabIndex={0}>
            <div className="tournament-card-top">
                <div className="tournament-mark"><BsDiagram3Fill /></div>
                <div>
                    <p className="tournaments-eyebrow">Campeonato</p>
                    <h2>{tournament.name}</h2>
                </div>
            </div>

            <div className="tournament-card-stats">
                <span><BsPeopleFill /> {tournament.teamCount}/{tournament.maxTeams} times</span>
                <span><BsTrophyFill /> Premio {tournament.prizeLabel}</span>
                <span><BsCashCoin /> Entrada {tournament.entryFeeLabel}</span>
                <span><BsTrophyFill /> Elo medio {tournament.averageElo}</span>
            </div>
        </article>
    );
}
