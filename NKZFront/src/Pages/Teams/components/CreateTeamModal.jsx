import { useState } from "react";
import { BsX } from "react-icons/bs";

export default function CreateTeamModal({
    open,
    teamName,
    teamTag,
    loading,
    feedback,
    onClose,
    onSubmit,
    onTeamNameChange,
    onTeamTagChange,
    onTeamImageChange,
}) {
    const [imagePreview, setImagePreview] = useState("");

    if (!open) return null;

    function closeModal() {
        if (imagePreview) URL.revokeObjectURL(imagePreview);
        setImagePreview("");
        onTeamImageChange(null);
        onClose();
    }

    return (
        <div className="team-modal-backdrop" onClick={closeModal}>
            <section className="team-modal" onClick={(event) => event.stopPropagation()}>
                <button className="team-modal-close" onClick={closeModal} aria-label="Fechar criacao de time">
                    <BsX />
                </button>

                <div className="team-modal-header">
                    <div className="team-mark large">+</div>
                    <div>
                        <p className="teams-eyebrow">Novo time</p>
                        <h2>Criar equipe</h2>
                        <span>Seu jogador vinculado sera usado como dono do time.</span>
                    </div>
                </div>

                <form className="team-create-form team-panel" onSubmit={onSubmit}>
                    <label>
                        Nome do time
                        <input
                            type="text"
                            value={teamName}
                            onChange={(event) => onTeamNameChange(event.target.value)}
                            placeholder="Ex: NKZ Academy"
                        />
                    </label>

                    <label>
                        Tag do time
                        <input
                            type="text"
                            value={teamTag}
                            minLength={3}
                            maxLength={5}
                            onChange={(event) => onTeamTagChange(event.target.value.replace(/[^a-zA-Z]/g, "").toUpperCase())}
                            placeholder="Ex: NKZ"
                        />
                    </label>

                    <label className="team-image-preview-picker create">
                        <span>Imagem do time</span>
                        <div className="team-image-preview">
                            {imagePreview ? <img src={imagePreview} alt="Preview do time" /> : <strong>{teamTag || "NKZ"}</strong>}
                        </div>
                        <strong>Escolher imagem</strong>
                        <input
                            type="file"
                            accept="image/*"
                            onChange={(event) => {
                                const file = event.target.files?.[0] || null;
                                if (imagePreview) URL.revokeObjectURL(imagePreview);
                                setImagePreview(file ? URL.createObjectURL(file) : "");
                                onTeamImageChange(file);
                            }}
                        />
                    </label>

                    {feedback.message && (
                        <div className={`teams-feedback ${feedback.type}`}>{feedback.message}</div>
                    )}

                    <button className="team-action-button primary team-create-submit" type="submit" disabled={loading}>
                        {loading ? "Criando..." : "Criar time"}
                    </button>
                </form>
            </section>
        </div>
    );
}
