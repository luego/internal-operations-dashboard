import Link from "next/link";
import { ArrowRight, BarChart3, ShieldCheck, TicketCheck } from "lucide-react";

const capabilities = [
  { icon: BarChart3, label: "Live operations dashboard" },
  { icon: TicketCheck, label: "Complete ticket lifecycle" },
  { icon: ShieldCheck, label: "Role-based access" },
];

export default function Home() {
  return (
    <main className="landing-shell">
      <div className="landing-glow" aria-hidden="true" />
      <nav className="landing-nav" aria-label="Primary navigation">
        <Link className="brand" href="/">
          <span className="brand-mark">O</span>
          <span>OpsDesk</span>
        </Link>
        <Link className="button button-ghost" href="/login">
          Sign in
        </Link>
      </nav>

      <section className="hero">
        <div className="eyebrow">
          <span className="status-dot" />
          Operations workspace
        </div>
        <h1>Internal operations, under control.</h1>
        <p className="hero-copy">
          One focused workspace for teams, tickets, assignments and the signals
          that keep work moving.
        </p>
        <div className="hero-actions">
          <Link className="button button-primary" href="/login">
            Open workspace <ArrowRight aria-hidden="true" size={16} />
          </Link>
          <a className="button button-ghost" href="#capabilities">
            Explore capabilities
          </a>
        </div>
      </section>

      <section className="capability-grid" id="capabilities" aria-label="Capabilities">
        {capabilities.map(({ icon: Icon, label }) => (
          <article className="capability-card" key={label}>
            <Icon aria-hidden="true" size={18} />
            <span>{label}</span>
          </article>
        ))}
      </section>
    </main>
  );
}
