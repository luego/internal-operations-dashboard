import { AlertTriangle, Building2, CircleDot, Ticket, Users } from "lucide-react";

import type { DashboardSummary, DashboardTrends } from "@/lib/api/dashboard";

const dateFormatter = new Intl.DateTimeFormat("en", { month: "short", day: "numeric" });

export function DashboardOverview({
  summary,
  trends,
}: Readonly<{ summary: DashboardSummary; trends: DashboardTrends }>) {
  const maximumActivity = Math.max(
    1,
    ...trends.points.map((point) => point.ticketsCreated + point.commentsAdded),
  );
  const metrics = [
    { label: "Total tickets", value: summary.totalTickets, detail: `${summary.unassignedTickets} unassigned`, icon: Ticket },
    { label: "Open", value: summary.openTickets, detail: `${summary.inProgressTickets} in progress`, icon: CircleDot },
    { label: "High priority", value: summary.highPriorityActiveTickets, detail: "Active attention", icon: AlertTriangle },
    { label: "Resolved", value: summary.resolvedTickets, detail: `${summary.closedTickets} closed`, icon: Users },
  ];

  return (
    <div className="overview-stack">
      <div className="overview-heading">
        <div>
          <span className="login-kicker">Live workspace</span>
          <h1>Operations overview</h1>
          <p>Current workload, coverage and recent collaboration signals.</p>
        </div>
        <span className="updated-at">
          Updated {new Date(summary.generatedAtUtc).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}
        </span>
      </div>

      <section className="metric-grid" aria-label="Key metrics">
        {metrics.map(({ label, value, detail, icon: Icon }) => (
          <article className="metric-card" key={label}>
            <div className="metric-label"><Icon aria-hidden="true" size={15} />{label}</div>
            <strong>{value}</strong>
            <span>{detail}</span>
          </article>
        ))}
      </section>

      <section className="activity-panel">
        <div className="panel-heading">
          <div>
            <h2>Ticket activity</h2>
            <p>{summary.activeUsers} active users across {summary.activeDepartments} departments</p>
          </div>
          <Building2 aria-hidden="true" size={18} />
        </div>
        <div className="activity-chart" aria-label="Recent ticket activity">
          {trends.points.map((point) => {
            const total = point.ticketsCreated + point.commentsAdded;
            return (
              <div className="activity-column" key={point.date}>
                <div className="bar-track">
                  <span
                    aria-label={`${point.ticketsCreated} tickets created on ${point.date}`}
                    className="activity-bar"
                    style={{ height: `${Math.max(8, (total / maximumActivity) * 100)}%` }}
                    title={`${point.ticketsCreated} tickets · ${point.commentsAdded} comments`}
                  />
                </div>
                <span>{dateFormatter.format(new Date(`${point.date}T00:00:00`))}</span>
              </div>
            );
          })}
        </div>
        <div className="chart-legend"><span><i className="legend-dot" /> Tickets + comments</span></div>
      </section>
    </div>
  );
}
