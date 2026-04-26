import TournamentCard from "./TournamentCard";

export default function TournamentList({ tournaments, onSelect }) {
    if (!tournaments.length) {
        return <section className="tournaments-state">Nenhum campeonato encontrado.</section>;
    }

    return (
        <section className="tournaments-grid">
            {tournaments.map((tournament) => (
                <TournamentCard key={tournament.id} tournament={tournament} onSelect={onSelect} />
            ))}
        </section>
    );
}
