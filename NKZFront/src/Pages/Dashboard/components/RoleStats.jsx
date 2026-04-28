const roleLabels = {
    TOP: "TOP",
    JUNGLE: "JUNGLE",
    MID: "MID",
    ADC: "ADC",
    SUPPORT: "SUPPORT",
};

export default function RoleStats({ roles }) {
    return (
        <section className="dashboard-card dashboard-list-card">
            <div className="dashboard-card-heading">
                <p className="dashboard-eyebrow">Rotas</p>
                <h2>Desempenho por role</h2>
            </div>

            <div className="role-grid">
                {Object.keys(roleLabels).map((roleKey) => {
                    const role = roles[roleKey] || { matches: 0, wins: 0, losses: 0, winRate: 0 };

                    return (
                        <article key={roleKey} className="role-card">
                            <div className="role-header">
                                <strong>{roleLabels[roleKey]}</strong>
                                <span>{role.matches} jogos</span>
                            </div>
                            <div className="role-progress">
                                <span style={{ width: `${Math.min(role.winRate, 100)}%` }} />
                            </div>
                            <div className="role-footer">
                                <span>{role.wins}V / {role.losses}D</span>
                                <strong>{role.winRate}% WR</strong>
                            </div>
                        </article>
                    );
                })}
            </div>
        </section>
    );
}
