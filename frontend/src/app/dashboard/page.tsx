import { BarChart3, Building2, Settings, Ticket, Users } from "lucide-react";
import { cookies } from "next/headers";
import { redirect } from "next/navigation";

import { ACCESS_COOKIE, environment } from "@/lib/auth/server";
import { getAuthenticatedUser } from "@/lib/auth/guard";

import { DashboardData } from "./dashboard-data";
import { LogoutButton } from "./logout-button";

export const metadata = { title: "Dashboard" };

const navigation = [
  { label: "Overview", icon: BarChart3, active: true },
  { label: "Tickets", icon: Ticket },
  { label: "Departments", icon: Building2 },
  { label: "Users", icon: Users },
];

export default async function DashboardPage() {
  const accessToken = (await cookies()).get(ACCESS_COOKIE)?.value;
  let user = null;
  try {
    user = await getAuthenticatedUser(accessToken, environment());
  } catch {
    user = null;
  }
  if (!user) redirect("/login");

  return (
    <main className="workspace-shell">
      <aside className="workspace-sidebar">
        <div className="sidebar-brand brand"><span className="brand-mark">O</span><span>OpsDesk</span></div>
        <nav aria-label="Workspace navigation">
          {navigation.map(({ label, icon: Icon, active }) => (
            <span className={active ? "nav-item nav-item-active" : "nav-item"} key={label}>
              <Icon aria-hidden="true" size={16} />{label}
            </span>
          ))}
        </nav>
        <div className="sidebar-bottom"><span className="nav-item"><Settings aria-hidden="true" size={16} />Settings</span></div>
      </aside>

      <section className="workspace-main">
        <header className="dashboard-header">
          <div><span className="mobile-brand">OpsDesk</span></div>
          <div className="dashboard-user">
            <div><strong>{user.displayName}</strong><span>{user.roles.join(" · ")}</span></div>
            <LogoutButton />
          </div>
        </header>
        <div className="dashboard-content"><DashboardData /></div>
      </section>
    </main>
  );
}
