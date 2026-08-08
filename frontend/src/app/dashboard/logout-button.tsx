"use client";

import { LogOut } from "lucide-react";
import { useRouter } from "next/navigation";
import { useState } from "react";

export function LogoutButton() {
  const router = useRouter();
  const [pending, setPending] = useState(false);

  async function signOut() {
    setPending(true);
    try {
      await fetch("/api/auth/logout", { method: "POST" });
    } finally {
      router.replace("/login");
      router.refresh();
    }
  }

  return (
    <button className="button button-ghost" type="button" onClick={signOut} disabled={pending}>
      <LogOut aria-hidden="true" size={15} />
      {pending ? "Signing out…" : "Sign out"}
    </button>
  );
}
