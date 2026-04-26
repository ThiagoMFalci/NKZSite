import EloSelector from "../../../Components/EloSelector";

export default function TeamFilter({ search, selectedElos, eloSort, onSearchChange, onEloToggle, onEloSortChange }) {
    return (
        <section className="teams-filter filter-shell">
            <label className="filter-control filter-control-search">
                Buscar time
                <input
                    type="search"
                    value={search}
                    onChange={(event) => onSearchChange(event.target.value)}
                    placeholder="Nome do time"
                />
            </label>

            <EloSelector selectedElos={selectedElos} onToggle={onEloToggle} />

            <label className="filter-control">
                Ordem de elo
                <select value={eloSort} onChange={(event) => onEloSortChange(event.target.value)}>
                    <option value="none">Padrao</option>
                    <option value="asc">Crescente</option>
                    <option value="desc">Decrescente</option>
                </select>
            </label>
        </section>
    );
}
