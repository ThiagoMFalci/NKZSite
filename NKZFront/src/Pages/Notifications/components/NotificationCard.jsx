import { BsCheck2, BsX } from "react-icons/bs";

function statusLabel(status) {
    const normalized = String(status || "Pending").toLowerCase();
    if (normalized === "accepted") return "Aceito";
    if (normalized === "declined") return "Recusado";
    if (normalized === "cancelled") return "Cancelado";
    return "Pendente";
}

export default function NotificationCard({ notification, onRespond, responding }) {
    const isPending = String(notification.status).toLowerCase() === "pending";

    return (
        <article className="notification-card">
            <div className="notification-card-main">
                <p className="notifications-eyebrow">{notification.typeLabel}</p>
                <h3>{notification.title}</h3>
                <div className="notification-meta">
                    {notification.details.map((detail) => (
                        <span key={detail}>{detail}</span>
                    ))}
                </div>
            </div>

            <div className="notification-side">
                <span className={`notification-status ${String(notification.status).toLowerCase()}`}>
                    {statusLabel(notification.status)}
                </span>
                <span className="notification-date">{notification.dateLabel}</span>

                {isPending && notification.canRespond && (
                    <div className="notification-actions">
                        <button
                            className="notification-accept"
                            onClick={() => onRespond(notification.id, true)}
                            disabled={responding}
                        >
                            <BsCheck2 /> Aceitar
                        </button>
                        <button
                            className="notification-decline"
                            onClick={() => onRespond(notification.id, false)}
                            disabled={responding}
                        >
                            <BsX /> Recusar
                        </button>
                    </div>
                )}
            </div>
        </article>
    );
}
