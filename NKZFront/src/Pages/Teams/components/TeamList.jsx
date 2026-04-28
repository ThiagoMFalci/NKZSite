import TeamCard from "./TeamCard";

export default function TeamList({ teams, selectedTeamId, onSelect }) {
    if (!teams.length) {
        return (
            <section className="teams-state">
                Nenhum time encontrado com os filtros atuais.
            </section>
        );
    }

    return (
        <section className="teams-grid">
            {teams.map((team) => (
                <TeamCard
                    key={team.id}
                    team={team}
                    selected={team.id === selectedTeamId}
                    onSelect={onSelect}
                />
            ))}
        </section>
    );
}
