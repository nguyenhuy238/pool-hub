import { PageHeader } from "@/components/ui";
import { DashboardError } from "./DashboardError";
import { DashboardSkeleton } from "./DashboardSkeleton";
import { MetricGroup } from "./MetricGroup";
import { QuickActions } from "./QuickActions";
import type { DashboardAction, DashboardMetric } from "@/features/dashboard/dashboard-types";

export function DashboardShell({ title, description, metrics, actions, loading, error, updatedAt }: {
  title: string;
  description?: string;
  metrics: DashboardMetric[];
  actions: DashboardAction[];
  loading?: boolean;
  error?: string | null;
  updatedAt?: Date;
}) {
  return (
    <>
      <PageHeader
        title={title}
        description={description}
        action={updatedAt ? <span className="muted-text">Cập nhật {updatedAt.toLocaleTimeString("vi-VN")}</span> : null}
      />
      {loading ? <DashboardSkeleton /> : null}
      {!loading && error ? <DashboardError message={error} /> : null}
      {!loading && !error ? <MetricGroup metrics={metrics} /> : null}
      {!loading && !error ? <QuickActions actions={actions} /> : null}
    </>
  );
}
