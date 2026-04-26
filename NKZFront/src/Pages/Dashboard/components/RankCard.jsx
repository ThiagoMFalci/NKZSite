import { GiRank3 } from "react-icons/gi";

export default function RankCard({ rank }) {
    const tier = rank.tier || "UNRANKED";
    const division = rank.division || "";

    return (
        <section className="dashboard-card dashboard-rank-card">
            <div className="dashboard-card-heading">
                <p className="dashboard-eyebrow">Solo/Duo</p>
                <h2>Rank atual</h2>
            </div>

            <div className="rank-content">
                <div className={`rank-emblem rank-${tier.toLowerCase()}`}>
                    {rank.emblemUrl ? (
                        <img src={rank.emblemUrl} alt={`${tier} ${division}`} />
                    ) : (
                        <GiRank3 />
                    )}
                </div>

                <div>
                    <strong>{tier} {division}</strong>
                    <span>{rank.leaguePoints} LP</span>
                </div>
            </div>
        </section>
    );
}
