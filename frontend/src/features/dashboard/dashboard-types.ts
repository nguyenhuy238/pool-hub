import type React from "react";
import type { MetricTone } from "@/components/dashboard/MetricCard";
import type { DashboardSummary, RoleName } from "@/types";

export type DashboardMetric = {
  key: string;
  label: string;
  value: React.ReactNode;
  hint?: string;
  tone?: MetricTone;
};

export type DashboardAction = {
  label: string;
  href: string;
  primary?: boolean;
};

export type DashboardContext = {
  roles: RoleName[];
  permissions: string[];
  summary: DashboardSummary | null;
};

export type DashboardConfig = {
  title: string;
  description: string;
  metrics: DashboardMetric[];
  actions: DashboardAction[];
};
