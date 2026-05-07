import { useEffect, useState } from "react";
import { BsImage, BsX } from "react-icons/bs";

const ELO_OPTIONS = [
    "UNRANKED",
    "IRON",
    "BRONZE",
    "SILVER",
    "GOLD",
    "PLATINUM",
    "EMERALD",
    "DIAMOND",
    "MASTER",
    "GRANDMASTER",
    "CHALLENGER",
];

export default function CreateLeagueModal({
    open,
    formData,
    loading,
    feedback,
    validationErrors = {},
    onClose,
    onChange,
    onSubmit,
}) {
    const [imagePreview, setImagePreview] = useState("");

    useEffect(() => {
        if (!formData.image) {
            setImagePreview("");
            return undefined;
        }

        const preview = URL.createObjectURL(formData.image);
        setImagePreview(preview);
        return () => URL.revokeObjectURL(preview);
    }, [formData.image]);

    if (!open) return null;

    return (
        <div className="league-modal-backdrop" onClick={onClose}>
            <section className="league-modal league-create-modal" onClick={(event) => event.stopPropagation()}>
                <button className="league-modal-close" onClick={onClose} aria-label="Fechar criacao de liga">
                    <BsX />
                </button>

                <div className="league-modal-header">
                    <div className="league-mark large">+</div>
                    <div>
                        <p className="leagues-eyebrow">Nova liga</p>
                        <h2>Criar liga</h2>
                        <span>Somente administradores podem publicar temporadas competitivas.</span>
                    </div>
                </div>

                <form className="league-create-form" onSubmit={onSubmit}>
                    <label className="league-image-picker">
                        <span>Imagem da liga</span>
                        <div className="league-image-preview">
                            {imagePreview ? (
                                <img src={imagePreview} alt="Preview da liga" />
                            ) : (
                                <strong><BsImage /> Capa da liga</strong>
                            )}
                        </div>
                        <em>Escolher imagem</em>
                        <input
                            type="file"
                            name="image"
                            accept="image/*"
                            onChange={onChange}
                        />
                    </label>

                    <label>
                        Nome da liga
                        <input
                            type="text"
                            name="name"
                            value={formData.name}
                            onChange={onChange}
                            placeholder="Ex: Split NKZ Academy"
                            minLength={4}
                            maxLength={60}
                            required
                            aria-invalid={Boolean(validationErrors.name)}
                        />
                        <small className={validationErrors.name ? "field-error" : "field-hint"}>
                            {validationErrors.name || "Use mais de 3 caracteres, ate 60."}
                        </small>
                    </label>

                    <div className="league-create-grid">
                        <label>
                            Premiacao
                            <input
                                type="number"
                                name="award"
                                min="0"
                                step="0.01"
                                value={formData.award}
                                onChange={onChange}
                                aria-invalid={Boolean(validationErrors.award)}
                            />
                            {validationErrors.award && <small className="field-error">{validationErrors.award}</small>}
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
                                aria-invalid={Boolean(validationErrors.entryFee)}
                            />
                            {validationErrors.entryFee && <small className="field-error">{validationErrors.entryFee}</small>}
                        </label>

                        <label>
                            Modalidade
                            <select name="modality" value={formData.modality} onChange={onChange}>
                                <option value="Chaveamento">Chaveamento</option>
                                <option value="Ranking">Ranking</option>
                            </select>
                        </label>
                    </div>

                    <div className="league-create-grid">
                        <label>
                            Elo minimo
                            <select name="minimumElo" value={formData.minimumElo} onChange={onChange}>
                                {ELO_OPTIONS.map((elo) => <option key={elo} value={elo}>{elo}</option>)}
                            </select>
                        </label>

                        <label>
                            Elo maximo
                            <select name="maximumElo" value={formData.maximumElo} onChange={onChange}>
                                {ELO_OPTIONS.map((elo) => <option key={elo} value={elo}>{elo}</option>)}
                            </select>
                        </label>

                        <label>
                            Vagas
                            <input type="number" value="16" disabled />
                        </label>
                    </div>

                    <div className="league-create-grid">
                        <label>
                            Pontos minimos do time
                            <input
                                type="number"
                                name="minimumTeamPoints"
                                min="0"
                                value={formData.minimumTeamPoints}
                                onChange={onChange}
                                aria-invalid={Boolean(validationErrors.minimumTeamPoints)}
                            />
                            {validationErrors.minimumTeamPoints && <small className="field-error">{validationErrors.minimumTeamPoints}</small>}
                        </label>

                        <label>
                            Pontos maximos do time
                            <input
                                type="number"
                                name="maximumTeamPoints"
                                min="0"
                                value={formData.maximumTeamPoints}
                                onChange={onChange}
                                aria-invalid={Boolean(validationErrors.maximumTeamPoints)}
                            />
                            {validationErrors.maximumTeamPoints && <small className="field-error">{validationErrors.maximumTeamPoints}</small>}
                        </label>

                        <label>
                            Inicio da fila
                            <input
                                type="time"
                                name="rankingQueueOpenTime"
                                value={formData.rankingQueueOpenTime}
                                onChange={onChange}
                                disabled={formData.modality !== "Ranking"}
                                aria-invalid={Boolean(validationErrors.rankingQueueOpenTime)}
                            />
                            {validationErrors.rankingQueueOpenTime && <small className="field-error">{validationErrors.rankingQueueOpenTime}</small>}
                        </label>
                    </div>

                    <div className="league-create-grid">
                        <label>
                            Final da fila
                            <input
                                type="time"
                                name="rankingQueueCloseTime"
                                value={formData.rankingQueueCloseTime}
                                onChange={onChange}
                                disabled={formData.modality !== "Ranking"}
                                aria-invalid={Boolean(validationErrors.rankingQueueCloseTime)}
                            />
                            {validationErrors.rankingQueueCloseTime && <small className="field-error">{validationErrors.rankingQueueCloseTime}</small>}
                        </label>
                    </div>

                    <div className="league-create-grid">
                        <label>
                            Data de inicio
                            <input
                                type="date"
                                name="startDate"
                                value={formData.startDate}
                                onChange={onChange}
                                aria-invalid={Boolean(validationErrors.startDate)}
                            />
                            {validationErrors.startDate && <small className="field-error">{validationErrors.startDate}</small>}
                        </label>

                        <label>
                            Data final
                            <input
                                type="date"
                                name="endDate"
                                value={formData.endDate}
                                onChange={onChange}
                                aria-invalid={Boolean(validationErrors.endDate)}
                            />
                            {validationErrors.endDate && <small className="field-error">{validationErrors.endDate}</small>}
                        </label>
                    </div>

                    {feedback.message && (
                        <div className={`leagues-feedback ${feedback.type}`}>{feedback.message}</div>
                    )}

                    <button className="btn-primary league-join-button" type="submit" disabled={loading}>
                        {loading ? "Criando..." : "Criar liga"}
                    </button>
                </form>
            </section>
        </div>
    );
}
