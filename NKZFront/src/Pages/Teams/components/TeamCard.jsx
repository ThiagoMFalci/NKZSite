import { BsPeopleFill, BsStars } from "react-icons/bs";
import RankEmblem from "../../../Components/RankEmblem";

const API_BASE_URL = import.meta.env.VITE_API_URL || "";

function resolveImageUrl(url) {
    if (!url) return "";
    if (/^https?:\/\//i.test(url)) return url;
    return `${API_BASE_URL}/${url}`.replace(/([^:]\/)\/+/g, "$1");
}

export default function TeamCard({ team, selected, onSelect }) {
    return (
        <article
            className={`team-card ${selected ? "selected" : ""}`}
            onClick={() => onSelect(team)}
            role="button"
            tabIndex={0}
            onKeyDown={(event) => {
                if (event.key === "Enter" || event.key === " ") onSelect(team);
            }}
        >
            <div className="team-card-cover">
                {team.profileImageUrl && (
                    <img
                        src={resolveImageUrl(team.profileImageUrl)}
                        alt={team.name}
                        onError={(event) => {
                            event.currentTarget.style.display = "none";
                        }}
                    />
                )}
                <span className={`team-status-pill ${team.status?.key || "recruiting"}`}>
                    {team.status?.label || "Recrutando"}
                </span>
                <strong>{team.tag}</strong>
            </div>

            <div className="team-card-body">
                <div className="team-card-top">
                    <div className="team-mark">
                        <span className="team-mark-fallback">{team.initials}</span>
                        {team.profileImageUrl && (
                            <img
                                src={resolveImageUrl(team.profileImageUrl)}
                                alt={team.name}
                                onError={(event) => {
                                    event.currentTarget.style.display = "none";
                                }}
                            />
                        )}
                    </div>
                    <div>
                        <p className="teams-eyebrow">{team.tag}</p>
                        <h2>{team.name}</h2>
                    </div>
                </div>

                <div className="team-card-highlight">
                    <div>
                        <span>Elenco</span>
                        <strong>{team.playerCount}</strong>
                    </div>
                    <div>
                        <span>Elo medio</span>
                        <strong><RankEmblem tier={team.averageElo} label={team.averageElo} className="compact" /> {team.averageElo}</strong>
                    </div>
                </div>

                <div className="team-card-stats">
                    <span><BsPeopleFill /> {team.playerCount} jogadores</span>
                    <span><BsStars /> {team.points} pts</span>
                </div>

                <div className="team-card-accent" />
            </div>
        </article>
    );
}
