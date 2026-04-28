import EloSelector from "../../../Components/EloSelector";

export default function RankingFilter({
    search,
    selectedElos,
    eloSort,
    onSearchChange,
    onEloToggle,
    onEloSortChange,
    searchLabel = "Buscar jogador",
    searchPlaceholder = "Nome do invocador",
}) {
    return (
        <section className="ranking-filter filter-shell">
            <label className="filter-control filter-control-search">
                {searchLabel}
                <input
                    type="search"
                    value={search}
                    onChange={(event) => onSearchChange(event.target.value)}
                    placeholder={searchPlaceholder}
                />
            </label>

            <EloSelector label="Elo" selectedElos={selectedElos} onToggle={onEloToggle} />

            <label className="filter-control">
                Ordem de elo
                <select value={eloSort} onChange={(event) => onEloSortChange(event.target.value)}>
                    <option value="none">Pontuacao</option>
                    <option value="asc">Crescente</option>
                    <option value="desc">Decrescente</option>
                </select>
            </label>
        </section>
    );
}
