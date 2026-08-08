"use client";

import { useQuery } from "@tanstack/react-query";
import { useRouter } from "next/navigation";

import type { DashboardSummary, DashboardTrends } from "@/lib/api/dashboard";

import { DashboardOverview } from "./dashboard-overview";

async function request<T>(path: string, onUnauthorized: () => void): Promise<T> {
  const response = await fetch(path, { cache: "no-store" });
  if (response.status === 401) {
    onUnauthorized();
    throw new Error("Session expired");
  }
  if (!response.ok) throw new Error("Dashboard request failed");
  return response.json() as Promise<T>;
}

export function DashboardData() {
  const router = useRouter();
  const onUnauthorized = () => router.replace("/login");
  const summary = useQuery({
    queryKey: ["dashboard", "summary"],
    queryFn: () => request<DashboardSummary>("/api/backend/dashboard/summary", onUnauthorized),
  });
  const trends = useQuery({
    queryKey: ["dashboard", "trends", 14],
    queryFn: () => request<DashboardTrends>("/api/backend/dashboard/trends?days=14", onUnauthorized),
  });

  if (summary.isPending || trends.isPending) {
    return <div className="dashboard-loading" aria-label="Loading dashboard"><span /><span /><span /></div>;
  }
  if (summary.isError || trends.isError) {
    return (
      <div className="dashboard-error" role="alert">
        <strong>Dashboard data is unavailable.</strong>
        <span>Check that the API and PostgreSQL are healthy, then try again.</span>
        <button className="button button-ghost" onClick={() => { void summary.refetch(); void trends.refetch(); }}>
          Try again
        </button>
      </div>
    );
  }

  return <DashboardOverview summary={summary.data} trends={trends.data} />;
}
