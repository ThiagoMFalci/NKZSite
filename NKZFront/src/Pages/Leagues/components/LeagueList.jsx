import LeagueCard from "./LeagueCard";

export default function LeagueList({ leagues, onSelect }) {
    if (!leagues.length) {
        return <section className="leagues-state">Nenhuma liga encontrada.</section>;
    }

    return (
        <section className="leagues-grid">
            {leagues.map((league) => (
                <LeagueCard key={league.id} league={league} onSelect={onSelect} />
            ))}
        </section>
    );
}
