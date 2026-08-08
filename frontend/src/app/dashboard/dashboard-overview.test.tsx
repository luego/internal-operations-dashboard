import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { DashboardOverview } from "./dashboard-overview";

const summary = {
  generatedAtUtc: "2026-08-08T12:00:00Z",
  totalTickets: 24,
  openTickets: 9,
  inProgressTickets: 6,
  resolvedTickets: 5,
  closedTickets: 4,
  unassignedTickets: 3,
  highPriorityActiveTickets: 2,
  activeDepartments: 4,
  activeUsers: 18,
};

const trends = {
  from: "2026-08-06",
  to: "2026-08-08",
  points: [
    { date: "2026-08-06", ticketsCreated: 2, commentsAdded: 1 },
    { date: "2026-08-07", ticketsCreated: 4, commentsAdded: 3 },
    { date: "2026-08-08", ticketsCreated: 1, commentsAdded: 5 },
  ],
};

describe("DashboardOverview", () => {
  it("renders operational KPIs and trend activity", () => {
    render(<DashboardOverview summary={summary} trends={trends} />);

    expect(screen.getByRole("heading", { name: "Operations overview" })).toBeInTheDocument();
    expect(screen.getByText("24")).toBeInTheDocument();
    expect(screen.getByText("9")).toBeInTheDocument();
    expect(screen.getByText("3 unassigned")).toBeInTheDocument();
    expect(screen.getByText("Ticket activity")).toBeInTheDocument();
    expect(screen.getByText("18 active users across 4 departments")).toBeInTheDocument();
    expect(screen.getAllByLabelText(/tickets created/i)).toHaveLength(3);
  });
});
