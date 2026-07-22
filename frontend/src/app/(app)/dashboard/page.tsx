"use client";

import { useMemo } from "react";
import { DashboardShell } from "@/components/dashboard/DashboardShell";
import { useLoad } from "@/components/ui";
import { useAuth } from "@/components/auth-provider";
import { dashboardData } from "@/features/dashboard/dashboard-data";
import { resolveDashboardConfig } from "@/features/dashboard/dashboard-registry";

export default function DashboardPage() {
  const { user, roles } = useAuth();
  const { data, loading, error } = useLoad(() => dashboardData.summary(), []);
  const config = useMemo(
    () => resolveDashboardConfig({
      roles,
      permissions: user?.permissions ?? [],
      summary: data
    }),
    [data, roles, user?.permissions]
  );

  return (
    <DashboardShell
      title={config.title}
      description={config.description}
      metrics={config.metrics}
      actions={config.actions}
      loading={loading}
      error={error}
      updatedAt={data ? new Date() : undefined}
    />
  );
}
