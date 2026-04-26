import EloSelector from "../../../Components/EloSelector";

export default function TournamentFilter({
    search,
    fee,
    selectedElos,
    eloSort,
    onSearchChange,
    onFeeChange,
    onEloToggle,
    onEloSortChange,
}) {
    return (
        <section className="tournaments-filter filter-shell">
            <label className="filter-control filter-control-search">
                Buscar campeonato
                <input
                    type="search"
                    value={search}
                    onChange={(event) => onSearchChange(event.target.value)}
                    placeholder="Nome do campeonato"
                />
            </label>

            <label className="filter-control">
                Entrada
                <select value={fee} onChange={(event) => onFeeChange(event.target.value)}>
                    <option value="Todos">Todos</option>
                    <option value="Gratis">Gratis</option>
                    <option value="Pago">Pago</option>
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
