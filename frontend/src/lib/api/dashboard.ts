export type DashboardSummary = {
  generatedAtUtc: string;
  totalTickets: number;
  openTickets: number;
  inProgressTickets: number;
  resolvedTickets: number;
  closedTickets: number;
  unassignedTickets: number;
  highPriorityActiveTickets: number;
  activeDepartments: number;
  activeUsers: number;
};

export type DashboardTrendPoint = {
  date: string;
  ticketsCreated: number;
  commentsAdded: number;
};

export type DashboardTrends = {
  from: string;
  to: string;
  points: DashboardTrendPoint[];
};
