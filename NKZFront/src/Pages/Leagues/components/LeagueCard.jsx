import { BsCashCoin, BsPeopleFill, BsTrophyFill } from "react-icons/bs";

export default function LeagueCard({ league, onSelect }) {
    return (
        <article className="league-card" onClick={() => onSelect(league)} role="button" tabIndex={0}>
            <div className="league-card-top">
                <div className="league-mark"><BsTrophyFill /></div>
                <div>
                    <p className="leagues-eyebrow">Liga</p>
                    <h2>{league.name}</h2>
                </div>
            </div>

            <div className="league-card-stats">
                <span><BsPeopleFill /> {league.teamCount}/{league.maxTeams} times</span>
                <span><BsTrophyFill /> Premio {league.awardLabel}</span>
                <span><BsCashCoin /> Entrada {league.entryFeeLabel}</span>
                <span><BsTrophyFill /> Elo medio {league.averageElo}</span>
            </div>

            <div className="league-slots">
                <span style={{ width: `${league.occupancy}%` }} />
            </div>
        </article>
    );
}
