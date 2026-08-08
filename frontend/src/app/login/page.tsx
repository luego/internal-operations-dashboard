import Link from "next/link";

import { LoginForm } from "./login-form";

export const metadata = { title: "Sign in" };

export default function LoginPage() {
  return (
    <main className="auth-shell">
      <div className="auth-glow" aria-hidden="true" />
      <Link className="brand auth-brand" href="/">
        <span className="brand-mark">O</span>
        <span>OpsDesk</span>
      </Link>
      <section className="login-card" aria-labelledby="login-title">
        <div className="login-heading">
          <span className="login-kicker">Operations workspace</span>
          <h1 id="login-title">Welcome back</h1>
          <p>Sign in to manage your team’s operational work.</p>
        </div>
        <LoginForm />
      </section>
      <p className="auth-footer">Authorized team members only</p>
    </main>
  );
}
