import { BsX } from "react-icons/bs";

export default function CreateTournamentModal({
    open,
    formData,
    loading,
    feedback,
    onClose,
    onChange,
    onSubmit,
}) {
    if (!open) return null;

    return (
        <div className="tournament-modal-backdrop" onClick={onClose}>
            <section className="tournament-modal" onClick={(event) => event.stopPropagation()}>
                <button className="tournament-modal-close" onClick={onClose} aria-label="Fechar criacao de campeonato">
                    <BsX />
                </button>

                <div className="tournament-modal-header">
                    <div className="tournament-mark large">+</div>
                    <div>
                        <p className="tournaments-eyebrow">Novo campeonato</p>
                        <h2>Criar campeonato</h2>
                        <span>A API valida permissao de administrador ao salvar.</span>
                    </div>
                </div>

                <form className="tournament-create-form" onSubmit={onSubmit}>
                    <label>
                        Nome
                        <input
                            type="text"
                            name="name"
                            value={formData.name}
                            onChange={onChange}
                            placeholder="Nome do campeonato"
                        />
                    </label>

                    <div className="tournament-create-grid">
                        <label>
                            Max. times
                            <input
                                type="number"
                                name="maxTeams"
                                min="2"
                                value={formData.maxTeams}
                                onChange={onChange}
                            />
                        </label>

                        <label>
                            Premio
                            <input
                                type="number"
                                name="prize"
                                min="0"
                                step="0.01"
                                value={formData.prize}
                                onChange={onChange}
                            />
                        </label>

                        <label>
                            Entrada
                            <input
                                type="number"
                                name="entryFee"
                                min="0"
                                step="0.01"
                                value={formData.entryFee}
                                onChange={onChange}
                            />
                        </label>
                    </div>

                    <label className="tournament-check">
                        <input
                            type="checkbox"
                            name="joinOwnerTeam"
                            checked={formData.joinOwnerTeam}
                            onChange={onChange}
                        />
                        Inscrever meu time ao criar
                    </label>

                    {feedback.message && (
                        <div className={`tournaments-feedback ${feedback.type}`}>{feedback.message}</div>
                    )}

                    <button className="btn-primary tournament-join-button" type="submit" disabled={loading}>
                        {loading ? "Criando..." : "Criar campeonato"}
                    </button>
                </form>
            </section>
        </div>
    );
}
