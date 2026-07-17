import Link from "next/link";
import type { DashboardAction } from "@/features/dashboard/dashboard-types";

export function QuickActions({ actions }: { actions: DashboardAction[] }) {
  if (!actions.length) return null;

  return (
    <section className="card dashboard-actions">
      <h2>Thao tác nhanh</h2>
      <div className="dashboard-action-list">
        {actions.map((action) => (
          <Link key={action.href} className={action.primary ? "primary-btn" : "secondary-btn"} href={action.href}>
            {action.label}
          </Link>
        ))}
      </div>
    </section>
  );
}
