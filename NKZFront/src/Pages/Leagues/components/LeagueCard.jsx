import { BsCalendarEvent, BsCashCoin, BsPeopleFill, BsShieldFillCheck, BsTrophyFill } from "react-icons/bs";
import RankEmblem from "../../../Components/RankEmblem";

const API_BASE_URL = import.meta.env.VITE_API_URL || "";

function resolveImageUrl(url) {
    if (!url) return "";
    if (/^https?:\/\//i.test(url)) return url;
    return `${API_BASE_URL}/${url}`.replace(/([^:]\/)\/+/g, "$1");
}

function formatShortDate(value) {
    if (!value) return "Sem data";
    return new Date(value).toLocaleDateString("pt-BR", { day: "2-digit", month: "short" }).replace(".", "");
}

export default function LeagueCard({ league, onSelect }) {
    const imageUrl = resolveImageUrl(league.imageUrl);
    const isFull = league.teamCount >= league.maxTeams;

    return (
        <article className="league-card" onClick={() => onSelect(league)} role="button" tabIndex={0}>
            <div className="league-card-cover">
                {imageUrl && (
                    <img
                        src={imageUrl}
                        alt={league.name}
                        onError={(event) => {
                            event.currentTarget.style.display = "none";
                        }}
                    />
                )}
                <div className="league-cover-fallback">
                    <BsTrophyFill />
                </div>
                <span className={`league-status-pill ${isFull ? "full" : "open"}`}>
                    {isFull ? "Lotada" : "Inscricoes abertas"}
                </span>
                <span className="league-modality-pill">{league.modality}</span>
            </div>

            <div className="league-card-body">
                <div className="league-card-top">
                    <div className="league-mark">
                        {imageUrl ? (
                            <img src={imageUrl} alt="" />
                        ) : (
                            <BsTrophyFill />
                        )}
                    </div>
                    <div>
                        <p className="leagues-eyebrow">Liga</p>
                        <h2>{league.name}</h2>
                    </div>
                </div>

                <div className="league-card-highlight">
                    <div>
                        <span>Premiacao</span>
                        <strong>{league.awardLabel}</strong>
                    </div>
                    <div>
                        <span>Vagas</span>
                        <strong>{league.teamCount}/{league.maxTeams}</strong>
                    </div>
                </div>

                <div className="league-card-stats">
                    <span><BsPeopleFill /> {league.teamCount}/{league.maxTeams} times</span>
                    <span><BsCashCoin /> Entrada {league.entryFeeLabel}</span>
                    <span><RankEmblem tier={league.averageElo} label={league.averageElo} className="small" /> Media {league.averageElo}</span>
                    <span><BsShieldFillCheck /> {league.minimumElo} ate {league.maximumElo}</span>
                    <span><BsCalendarEvent /> {formatShortDate(league.startDate)} - {formatShortDate(league.endDate)}</span>
                </div>

                <div className="league-slots">
                    <span style={{ width: `${league.occupancy}%` }} />
                </div>
                <div className="league-card-footer">
                    <small>{league.occupancy}% das vagas preenchidas</small>
                    <strong>Ver liga</strong>
                </div>
            </div>
        </article>
    );
}
