"use client";

import { miscApi } from "@/lib/api/endpoints";
import { money } from "@/lib/status";
import { PageHeader, StateBlock, useLoad } from "@/components/ui";

export default function DashboardPage() {
  const { data, loading, error } = useLoad(() => miscApi.dashboardSummary(), []);

  return (
    <>
      <PageHeader title="Dashboard" description="Tổng quan vận hành lấy từ các API hiện có." />
      <StateBlock loading={loading} error={error} />
      {data ? <div className="kpi-grid">
        {[
          ["Tổng số bàn", data.totalTables],
          ["Bàn đang sử dụng", data.inUseTables],
          ["Bàn trống", data.availableTables],
          ["Booking hôm nay", data.todayBookings],
          ["Session đang chạy", data.activeSessions],
          ["Doanh thu hôm nay", money(data.todayRevenue)],
          ["Sản phẩm sắp hết", data.lowStockProducts],
          ["Thông báo mới", data.unreadNotifications]
        ].map(([labelText, value]) => <div className="card metric" key={labelText}><span>{labelText}</span><strong>{value}</strong></div>)}
      </div> : null}
    </>
  );
}
