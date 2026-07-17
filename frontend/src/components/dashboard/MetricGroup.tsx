import { MetricCard } from "./MetricCard";
import type { DashboardMetric } from "@/features/dashboard/dashboard-types";

export function MetricGroup({ metrics }: { metrics: DashboardMetric[] }) {
  if (!metrics.length) return <div className="state-card">Chưa có chỉ số phù hợp với vai trò hiện tại.</div>;

  return (
    <div className="kpi-grid">
      {metrics.map((metric) => (
        <MetricCard
          key={metric.key}
          label={metric.label}
          value={metric.value}
          hint={metric.hint}
          tone={metric.tone}
        />
      ))}
    </div>
  );
}
