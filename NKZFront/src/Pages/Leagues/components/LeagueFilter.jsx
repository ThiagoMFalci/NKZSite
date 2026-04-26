import EloSelector from "../../../Components/EloSelector";

export default function LeagueFilter({
    search,
    status,
    awardFilter,
    selectedElos,
    eloSort,
    onSearchChange,
    onStatusChange,
    onAwardFilterChange,
    onEloToggle,
    onEloSortChange,
}) {
    return (
        <section className="leagues-filter filter-shell">
            <label className="filter-control filter-control-search">
                Buscar liga
                <input
                    type="search"
                    value={search}
                    onChange={(event) => onSearchChange(event.target.value)}
                    placeholder="Nome da liga"
                />
            </label>

            <label className="filter-control">
                Status
                <select value={status} onChange={(event) => onStatusChange(event.target.value)}>
                    <option value="Todas">Todas</option>
                    <option value="Aberta">Aberta</option>
                    <option value="Lotada">Lotada</option>
                </select>
            </label>

            <label className="filter-control">
                Premiacao
                <select value={awardFilter} onChange={(event) => onAwardFilterChange(event.target.value)}>
                    <option value="Todas">Todas</option>
                    <option value="Sem premio">Sem premio</option>
                    <option value="Ate 100">Ate R$ 100</option>
                    <option value="Acima de 100">Acima de R$ 100</option>
                </select>
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
