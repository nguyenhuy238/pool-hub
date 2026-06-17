"use client";

import { adminDashboardApi } from "@/lib/api/endpoints";
import { money } from "@/lib/status";
import { DataTable, PageHeader, StateBlock, useLoad } from "@/components/ui";

export default function AdminDashboardPage() {
  const summary = useLoad(() => adminDashboardApi.summary(), []);
  const revenue = useLoad(() => adminDashboardApi.revenue(), []);
  const activeSessions = useLoad(() => adminDashboardApi.activeSessions(), []);
  const lowStock = useLoad(() => adminDashboardApi.lowStockProducts(), []);
  const audits = useLoad(() => adminDashboardApi.recentAuditLogs(), []);
  const loading = summary.loading || revenue.loading || activeSessions.loading || lowStock.loading || audits.loading;
  const error = summary.error || revenue.error || activeSessions.error || lowStock.error || audits.error;

  const cards = summary.data ? [
    ["Tổng số bàn", summary.data.totalTables],
    ["Bàn hoạt động", summary.data.activeTables ?? 0],
    ["Bàn bảo trì", summary.data.maintenanceTables],
    ["Session active", summary.data.activeSessions],
    ["Booking hôm nay", summary.data.todayBookings],
    ["Booking pending", summary.data.pendingBookings ?? 0],
    ["Booking confirmed", summary.data.confirmedBookings ?? 0],
    ["Doanh thu hôm nay", money(summary.data.todayRevenue)],
    ["Hóa đơn chưa thanh toán", summary.data.unpaidInvoices ?? 0],
    ["Sản phẩm sắp hết", summary.data.lowStockProducts],
    ["Thông báo chưa đọc", summary.data.unreadNotifications],
    ["Audit hôm nay", summary.data.todayAuditLogs ?? 0]
  ] : [];

  return (
    <>
      <PageHeader title="Admin Dashboard" description="Tổng quan quản trị lấy từ dữ liệu hiện có trong database." />
      <StateBlock loading={loading} error={error} empty={!loading && !summary.data} />
      {summary.data ? <div className="kpi-grid">{cards.map(([label, value]) => <div className="card metric" key={String(label)}><span>{label}</span><strong>{value}</strong></div>)}</div> : null}
      <section className="grid-2">
        <div className="card">
          <h3>Doanh thu 7 ngày</h3>
          <DataTable rows={(revenue.data || []) as unknown as Record<string, unknown>[]} columns={[{ key: "date", label: "Ngày", render: (row) => String(row.date).slice(0, 10) }, { key: "amount", label: "Doanh thu", render: (row) => money(Number(row.amount)) }]} />
        </div>
        <div className="card">
          <h3>Session đang chạy</h3>
          <DataTable rows={(activeSessions.data || []) as unknown as Record<string, unknown>[]} columns={[{ key: "sessionCode", label: "Mã" }, { key: "startedAtUtc", label: "Bắt đầu", render: (row) => String(row.startedAtUtc).replace("T", " ").slice(0, 16) }, { key: "durationMinutes", label: "Phút" }]} />
        </div>
        <div className="card">
          <h3>Sản phẩm sắp hết</h3>
          <DataTable rows={(lowStock.data || []) as unknown as Record<string, unknown>[]} columns={[{ key: "name", label: "Tên" }, { key: "sku", label: "SKU" }, { key: "stockQuantity", label: "Kho" }]} />
        </div>
        <div className="card">
          <h3>Audit gần đây</h3>
          <DataTable rows={(audits.data || []) as unknown as Record<string, unknown>[]} columns={[{ key: "action", label: "Action" }, { key: "entityName", label: "Entity" }, { key: "createdAtUtc", label: "Thời gian", render: (row) => String(row.createdAtUtc).replace("T", " ").slice(0, 16) }]} />
        </div>
      </section>
    </>
  );
}
