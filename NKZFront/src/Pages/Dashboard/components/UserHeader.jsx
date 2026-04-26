import { FaCamera, FaSave, FaUserShield } from "react-icons/fa";

export default function UserHeader({
    user,
    canEdit,
    loading,
    feedback,
    pendingPreview,
    hasPendingImage,
    onImageSelect,
    onImageSave,
}) {
    const imageUrl = pendingPreview || user.profileIconUrl;

    return (
        <section className="dashboard-user-header">
            <div className="dashboard-profile-icon">
                <span className="dashboard-profile-fallback">
                    <FaUserShield />
                </span>
                {imageUrl ? (
                    <img
                        src={imageUrl}
                        alt={user.summonerName}
                        onError={(event) => {
                            event.currentTarget.style.display = "none";
                        }}
                    />
                ) : null}
            </div>

            <div className="dashboard-user-info">
                <div className="dashboard-user-main">
                    <p className="dashboard-eyebrow">Perfil do invocador</p>
                    <h1>{user.summonerName}</h1>
                    <span>Nivel {user.summonerLevel}</span>
                </div>
                <div className="dashboard-user-actions">
                    {canEdit && (
                        <>
                            <label className="dashboard-image-upload">
                                <FaCamera />
                                Trocar imagem
                                <input
                                    type="file"
                                    accept="image/*"
                                    onChange={(event) => {
                                        onImageSelect(event.target.files?.[0] || null);
                                        event.target.value = "";
                                    }}
                                    disabled={loading}
                                />
                            </label>
                            {hasPendingImage && (
                                <button className="dashboard-image-save" type="button" onClick={onImageSave} disabled={loading}>
                                    <FaSave />
                                    {loading ? "Salvando..." : "Salvar imagem"}
                                </button>
                            )}
                        </>
                    )}
                </div>
                {feedback?.message && (
                    <div className={`dashboard-feedback compact ${feedback.type}`}>{feedback.message}</div>
                )}
            </div>
        </section>
    );
}
