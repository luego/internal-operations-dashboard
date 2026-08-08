"use client";

import { useRouter } from "next/navigation";
import { type FormEvent, useState } from "react";

const genericError = "We couldn’t sign you in. Check your details and try again.";

export function LoginForm() {
  const router = useRouter();
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pending) return;
    setPending(true);
    setError(null);
    const data = new FormData(event.currentTarget);

    try {
      const response = await fetch("/api/auth/login", {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({
          identifier: data.get("identifier"),
          password: data.get("password"),
        }),
      });
      if (!response.ok) throw new Error("Login rejected");
      router.replace("/dashboard");
      router.refresh();
    } catch {
      setError(genericError);
    } finally {
      setPending(false);
    }
  }

  return (
    <form className="login-form" onSubmit={submit}>
      <div className="field">
        <label htmlFor="identifier">Email or username</label>
        <input id="identifier" name="identifier" autoComplete="username" required disabled={pending} />
      </div>
      <div className="field">
        <div className="field-label-row">
          <label htmlFor="password">Password</label>
          <span>Secure workspace</span>
        </div>
        <input
          id="password"
          name="password"
          type="password"
          autoComplete="current-password"
          required
          disabled={pending}
        />
      </div>
      {error ? <p className="form-error" role="alert">{error}</p> : null}
      <button className="button button-primary login-submit" type="submit" disabled={pending}>
        {pending ? "Signing in…" : "Sign in"}
      </button>
    </form>
  );
}
