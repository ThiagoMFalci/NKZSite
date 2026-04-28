import { useMemo, useState } from "react";
import { ELO_OPTIONS } from "../../utils/elo";

export default function EloSelector({ label = "Elo medio", selectedElos, onToggle }) {
    const [open, setOpen] = useState(false);
    const displayValue = useMemo(() => {
        if (!selectedElos.length) return "Todos";
        if (selectedElos.length === 1) return selectedElos[0];
        return `${selectedElos.length} elos`;
    }, [selectedElos]);

    function clearSelection() {
        selectedElos.forEach((elo) => onToggle(elo));
    }

    return (
        <div className="elo-dropdown">
            <span className="elo-dropdown-label">{label}</span>
            <button
                className={`elo-dropdown-trigger ${open ? "open" : ""}`}
                type="button"
                onClick={() => setOpen((current) => !current)}
                aria-expanded={open}
            >
                <span>{displayValue}</span>
                <span className="elo-dropdown-chevron">v</span>
            </button>

            {open && (
                <div className="elo-dropdown-menu">
                    <button
                        className={`elo-dropdown-option ${!selectedElos.length ? "active" : ""}`}
                        type="button"
                        onClick={clearSelection}
                    >
                        Padrao
                    </button>

                    {ELO_OPTIONS.map((option) => {
                        const active = selectedElos.includes(option);
                        return (
                            <button
                                key={option}
                                className={`elo-dropdown-option ${active ? "active" : ""}`}
                                type="button"
                                onClick={() => onToggle(option)}
                            >
                                {option}
                            </button>
                        );
                    })}
                </div>
            )}
        </div>
    );
}
