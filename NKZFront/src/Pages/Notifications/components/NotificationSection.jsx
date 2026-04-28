import NotificationCard from "./NotificationCard";

export default function NotificationSection({ title, description, notifications, onRespond, responding }) {
    return (
        <section className="notifications-section">
            <div className="notifications-section-header">
                <div>
                    <p className="notifications-eyebrow">Notificacoes</p>
                    <h2>{title}</h2>
                </div>
                <span>{notifications.length}</span>
            </div>

            <p className="notifications-section-desc">{description}</p>

            <div className="notifications-list">
                {notifications.length ? (
                    notifications.map((notification) => (
                        <NotificationCard
                            key={notification.id}
                            notification={notification}
                            onRespond={onRespond}
                            responding={responding}
                        />
                    ))
                ) : (
                    <div className="notifications-empty">Nada por aqui ainda.</div>
                )}
            </div>
        </section>
    );
}
