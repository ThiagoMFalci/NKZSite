import { BsPeopleFill, BsStars } from "react-icons/bs";
import { GiRank3 } from "react-icons/gi";

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

            <span className={`team-status-badge ${team.status?.key || "recruiting"}`}>
                {team.status?.label || "Recrutando"}
            </span>

            <div className="team-card-stats">
                <span><BsPeopleFill /> {team.playerCount}/5 jogadores</span>
                <span><GiRank3 /> {team.averageElo}</span>
                <span><BsStars /> {team.points} pts</span>
            </div>
        </article>
    );
}
