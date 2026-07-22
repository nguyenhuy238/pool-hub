"use client";

import { useState } from "react";
import { adminDashboardApi, reportsApi } from "@/lib/api/endpoints";
import { money } from "@/lib/status";
import { PageHeader, StateBlock, useLoad } from "@/components/ui";
import { getCurrentVietnamMonthRange, getVietnamDateInputValue } from "@/lib/dateTime";
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip as RechartsTooltip,
  XAxis,
  YAxis
} from "recharts";

const paymentColors = ["#2563eb", "#059669", "#f59e0b", "#dc2626", "#7c3aed"];

export default function AdminAnalyticsPage() {
  const [range, setRange] = useState({ fromDate: "", toDate: "" });

  const applyPreset = (preset: string) => {
    if (preset === "today") {
      const date = getVietnamDateInputValue();
      setRange({ fromDate: date, toDate: date });
      return;
    }

    if (preset === "this_month") {
      const monthRange = getCurrentVietnamMonthRange();
      setRange({ fromDate: monthRange.fromDate, toDate: monthRange.toDate });
      return;
    }

    setRange({ fromDate: "", toDate: "" });
  };

  const yAxisFormatter = (value: number) => {
    if (value >= 1000000) return `${(value / 1000000).toFixed(1).replace(".0", "")}M`;
    if (value >= 1000) return `${value / 1000}k`;
    return String(value);
  };

  const xAxisFormatter = (value: unknown) => {
    if (!value) return "";
    const datePart = String(value).split("T")[0];
    const parts = datePart.split("-");
    return parts.length >= 3 ? `${parts[2]}/${parts[1]}` : datePart;
  };

  const summary = useLoad(() => adminDashboardApi.summary(), []);
  const revenue = useLoad(() => adminDashboardApi.revenue(range), [range]);
  const activeSessions = useLoad(() => adminDashboardApi.activeSessions(), []);
  const topProducts = useLoad(() => reportsApi.products(range), [range]);
  const tableUsage = useLoad(() => reportsApi.tableUsage(range), [range]);
  const paymentMethods = useLoad(() => reportsApi.paymentMethods(range), [range]);
  const inventory = useLoad(() => reportsApi.inventory(range), [range]);
  const loading = summary.loading || revenue.loading || activeSessions.loading || topProducts.loading || tableUsage.loading || paymentMethods.loading || inventory.loading;
  const error = summary.error || revenue.error || activeSessions.error || topProducts.error || tableUsage.error || paymentMethods.error || inventory.error;

  const cards = summary.data ? [
    ["Doanh thu hôm nay", money(summary.data.todayRevenue)],
    ["Bàn hoạt động", summary.data.activeTables ?? 0],
    ["Bàn bảo trì", summary.data.maintenanceTables],
    ["Phiên chơi đang chạy", summary.data.activeSessions],
    ["Phiên chơi kéo dài", summary.data.longRunningSessions ?? 0],
    ["Lượt đặt bàn hôm nay", summary.data.todayBookings],
    ["Lượt đặt bàn chờ xử lý", summary.data.pendingBookings ?? 0],
    ["Hóa đơn chưa thanh toán", summary.data.unpaidInvoices ?? 0],
    ["Sản phẩm sắp hết", summary.data.lowStockProducts],
    ["Thanh toán thành công", summary.data.successfulPaymentsToday ?? 0],
    ["Thông báo chưa đọc", summary.data.unreadNotifications],
    ["Nhật ký hệ thống hôm nay", summary.data.todayAuditLogs ?? 0]
  ] : [];

  return (
    <>
      <PageHeader title="Phân tích" description="Phân tích doanh thu, thanh toán, tồn kho, đặt bàn và hiệu suất bàn." />
      <div className="card list-controls" style={{ marginBottom: 24, display: "flex", gap: 16, alignItems: "center", flexWrap: "wrap" }}>
        <label>
          <span>Khoảng thời gian</span>
          <select onChange={(event) => applyPreset(event.target.value)} defaultValue="">
            <option value="">7 ngày gần nhất</option>
            <option value="today">Hôm nay</option>
            <option value="this_month">Tháng này</option>
          </select>
        </label>
        <label>
          <span>Từ ngày</span>
          <input type="date" value={range.fromDate} onChange={(event) => setRange({ ...range, fromDate: event.target.value })} />
        </label>
        <label>
          <span>Đến ngày</span>
          <input type="date" value={range.toDate} onChange={(event) => setRange({ ...range, toDate: event.target.value })} />
        </label>
        <button className="secondary-btn" type="button" onClick={() => setRange({ fromDate: "", toDate: "" })}>Xóa bộ lọc</button>
      </div>

      <StateBlock loading={loading} error={error} empty={!loading && !summary.data} />

      {summary.data ? <div className="kpi-grid">{cards.map(([label, value]) => <div className="card metric" key={String(label)}><span>{label}</span><strong>{value}</strong></div>)}</div> : null}

      {!loading && !error ? (
        <section style={{ display: "flex", flexDirection: "column", gap: 24, marginTop: 32 }}>
          <div className="analytics-grid analytics-grid-main">
            <ChartCard title="Doanh thu">
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={revenue.data || []}>
                  <defs>
                    <linearGradient id="colorRevenue" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="#2563eb" stopOpacity={0.75} />
                      <stop offset="95%" stopColor="#2563eb" stopOpacity={0.08} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} />
                  <XAxis dataKey="date" tickFormatter={xAxisFormatter} />
                  <YAxis tickFormatter={yAxisFormatter} />
                  <RechartsTooltip formatter={(value) => money(Number(value))} labelFormatter={xAxisFormatter} />
                  <Legend />
                  <Area type="monotone" dataKey="amount" name="Doanh thu" stroke="#2563eb" fill="url(#colorRevenue)" />
                </AreaChart>
              </ResponsiveContainer>
            </ChartCard>

            <ChartCard title="Phương thức thanh toán">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={(paymentMethods.data || []).map((item) => ({ name: item.paymentMethodName, value: item.amount }))}
                    cx="50%"
                    cy="50%"
                    innerRadius={60}
                    outerRadius={92}
                    dataKey="value"
                    label={({ percent }) => `${((percent ?? 0) * 100).toFixed(0)}%`}
                  >
                    {(paymentMethods.data || []).map((item, index) => <Cell key={item.paymentMethodId} fill={paymentColors[index % paymentColors.length]} />)}
                  </Pie>
                  <RechartsTooltip formatter={(value) => money(Number(value))} />
                  <Legend verticalAlign="bottom" height={36} iconType="circle" />
                </PieChart>
              </ResponsiveContainer>
            </ChartCard>
          </div>

          <div className="analytics-grid analytics-grid-three">
            <ChartCard title="Sản phẩm bán chạy">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={(topProducts.data || []).slice(0, 5)} layout="vertical">
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis type="number" />
                  <YAxis dataKey="productName" type="category" width={110} />
                  <RechartsTooltip />
                  <Bar dataKey="quantity" name="Số lượng" fill="#059669" radius={[0, 4, 4, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </ChartCard>

            <ChartCard title="Hiệu suất bàn">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={(tableUsage.data || []).slice(0, 6)}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="tableName" />
                  <YAxis />
                  <RechartsTooltip />
                  <Bar dataKey="totalMinutes" name="Phút sử dụng" fill="#f59e0b" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </ChartCard>

            <ChartCard title="Tồn kho">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={(inventory.data || []).slice(0, 6)}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="productName" />
                  <YAxis />
                  <RechartsTooltip formatter={(value) => money(Number(value))} />
                  <Bar dataKey="inventoryValue" name="Giá trị tồn" fill="#7c3aed" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </ChartCard>
          </div>
        </section>
      ) : null}
    </>
  );
}

function ChartCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="card" style={{ height: 350, display: "flex", flexDirection: "column" }}>
      <h3 style={{ margin: "0 0 16px", flexShrink: 0 }}>{title}</h3>
      <div style={{ flex: 1, minHeight: 0 }}>{children}</div>
    </div>
  );
}
