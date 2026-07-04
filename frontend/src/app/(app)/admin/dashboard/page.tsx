"use client";

import { useState } from "react";
import { adminDashboardApi, reportsApi } from "@/lib/api/endpoints";
import { money } from "@/lib/status";
import { DataTable, PageHeader, StateBlock, useLoad } from "@/components/ui";
import { getCurrentVietnamMonthRange, getVietnamDateInputValue } from "@/lib/dateTime";
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip as RechartsTooltip, Legend, ResponsiveContainer, PieChart, Pie, Cell, BarChart, Bar, AreaChart, Area } from 'recharts';
export default function AdminDashboardPage() {
  const [range, setRange] = useState({ FromDate: "", ToDate: "" });

  const applyPreset = (preset: string) => {
    if (preset === "today") {
      const d = getVietnamDateInputValue();
      setRange({ FromDate: d, ToDate: d });
    } else if (preset === "this_month") {
      const monthRange = getCurrentVietnamMonthRange();
      setRange({ FromDate: monthRange.fromDate, ToDate: monthRange.toDate });
    } else {
      setRange({ FromDate: "", ToDate: "" });
    }
  };

  const yAxisFormatter = (value: number) => {
    if (value >= 1000000) return (value / 1000000).toFixed(1).replace('.0', '') + 'M';
    if (value >= 1000) return (value / 1000) + 'k';
    return String(value);
  };

  const xAxisFormatter = (v: any) => {
    if (!v) return "";
    const datePart = String(v).split('T')[0];
    const parts = datePart.split('-');
    return parts.length >= 3 ? `${parts[2]}/${parts[1]}` : datePart;
  };

  const summary = useLoad(() => adminDashboardApi.summary(), []);
  const revenue = useLoad(() => adminDashboardApi.revenue(range), [range]);
  const activeSessions = useLoad(() => adminDashboardApi.activeSessions(), []);
  const topProducts = useLoad(() => reportsApi.products(range), [range]);
  const tableUsage = useLoad(() => reportsApi.tableUsage(range), [range]);
  const paymentMethods = useLoad(() => reportsApi.paymentMethods(range), [range]);
  const loading = summary.loading || revenue.loading || activeSessions.loading || topProducts.loading || tableUsage.loading || paymentMethods.loading;
  const error = summary.error || revenue.error || activeSessions.error || topProducts.error || tableUsage.error || paymentMethods.error;

  const cards = summary.data ? [
    ["Tổng số bàn", summary.data.totalTables],
    ["Bàn hoạt động", summary.data.activeTables ?? 0],
    ["Bàn bảo trì", summary.data.maintenanceTables],
    ["Phiên chơi đang hoạt động", summary.data.activeSessions],
    ["Lượt đặt bàn hôm nay", summary.data.todayBookings],
    ["Đặt bàn chờ xác nhận", summary.data.pendingBookings ?? 0],
    ["Đặt bàn đã xác nhận", summary.data.confirmedBookings ?? 0],
    ["Doanh thu hôm nay", money(summary.data.todayRevenue)],
    ["Hóa đơn chưa thanh toán", summary.data.unpaidInvoices ?? 0],
    ["Sản phẩm sắp hết", summary.data.lowStockProducts],
    ["Thông báo chưa đọc", summary.data.unreadNotifications],
    ["Audit hôm nay", summary.data.todayAuditLogs ?? 0]
    ,["Đơn hàng hôm nay", summary.data.ordersToday ?? 0]
    ,["Khách hàng", summary.data.totalCustomers ?? 0]
    ,["Hóa đơn hôm nay", summary.data.invoicesToday ?? 0]
  ] : [];

  return (
    <>
      <PageHeader title="Tổng quan quản trị" description="Tổng hợp các chỉ số vận hành và kinh doanh từ dữ liệu hệ thống." />
      <div className="card list-controls" style={{ marginBottom: '24px', display: 'flex', gap: '16px', alignItems: 'center' }}>
        <label style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
          <span>Tùy chọn:</span>
          <select onChange={e => applyPreset(e.target.value)} defaultValue="">
            <option value="">Tất cả thời gian</option>
            <option value="today">Hôm nay</option>
            <option value="this_month">Tháng này</option>
          </select>
        </label>
        <label style={{ display: 'flex', gap: '8px', alignItems: 'center', marginLeft: '16px' }}>
          <span>Từ ngày:</span>
          <input type="date" value={range.FromDate} onChange={e => setRange({ ...range, FromDate: e.target.value })} />
        </label>
        <label style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
          <span>Đến ngày:</span>
          <input type="date" value={range.ToDate} onChange={e => setRange({ ...range, ToDate: e.target.value })} />
        </label>
        <button className="primary-btn" onClick={() => setRange({ FromDate: "", ToDate: "" })}>Xóa bộ lọc</button>
      </div>
      <StateBlock loading={loading} error={error} empty={!loading && !summary.data} />
      {summary.data ? <div className="kpi-grid">{cards.map(([label, value]) => <div className="card metric" key={String(label)}><span>{label}</span><strong>{value}</strong></div>)}</div> : null}
      <section style={{ display: 'flex', flexDirection: 'column', gap: '24px', marginTop: '32px' }}>
        <div style={{ display: 'flex', gap: '24px', width: '100%' }}>
          <div className="card" style={{ flex: 6, height: 350, display: 'flex', flexDirection: 'column' }}>
          <h3 style={{ marginBottom: '16px', flexShrink: 0 }}>Biểu đồ Doanh Thu</h3>
          <div style={{ flex: 1, minHeight: 0 }}>
            <ResponsiveContainer width="100%" height="100%">
            <AreaChart data={(revenue.data || []) as any}>
              <defs>
                <linearGradient id="colorRevenue" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#8884d8" stopOpacity={0.8}/>
                  <stop offset="95%" stopColor="#8884d8" stopOpacity={0}/>
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" vertical={false} />
              <XAxis dataKey="date" tickFormatter={xAxisFormatter} />
              <YAxis tickFormatter={yAxisFormatter} />
              <RechartsTooltip formatter={(value) => money(Number(value))} labelFormatter={xAxisFormatter} />
              <Legend />
              <Area type="monotone" dataKey="amount" name="Doanh thu" stroke="#8884d8" fillOpacity={1} fill="url(#colorRevenue)" activeDot={{ r: 8 }} />
            </AreaChart>
            </ResponsiveContainer>
          </div>
        </div>
        <div className="card" style={{ flex: 4, height: 350, display: 'flex', flexDirection: 'column' }}>
          <h3 style={{ marginBottom: '16px', flexShrink: 0 }}>Phương thức thanh toán</h3>
          <div style={{ flex: 1, minHeight: 0 }}>
            <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie 
                data={(paymentMethods.data || []).map((item) => ({ name: item.paymentMethodName, value: item.amount }))}
                cx="50%" cy="50%" 
                innerRadius={60}
                outerRadius={90} 
                paddingAngle={5}
                dataKey="value" 
                labelLine={false}
                label={({ cx, cy, midAngle, innerRadius, outerRadius, percent }) => {
                  const angle = midAngle ?? 0;
                  const ratio = percent ?? 0;
                  const radius = innerRadius + (outerRadius - innerRadius) * 0.5;
                  const x = cx + radius * Math.cos(-angle * (Math.PI / 180));
                  const y = cy + radius * Math.sin(-angle * (Math.PI / 180));
                  return (
                    <text x={x} y={y} fill="white" textAnchor="middle" dominantBaseline="central" style={{ fontSize: '13px', fontWeight: 'bold' }}>
                      {`${(ratio * 100).toFixed(0)}%`}
                    </text>
                  );
                }}
              >
                <Cell fill="#6366f1" />
                <Cell fill="#10b981" />
              </Pie>
              <RechartsTooltip />
              <Legend verticalAlign="bottom" height={36} iconType="circle" wrapperStyle={{ paddingTop: '10px' }} />
            </PieChart>
            </ResponsiveContainer>
          </div>
        </div>
        </div>
        <div style={{ display: 'flex', gap: '24px', width: '100%' }}>
        <div className="card" style={{ flex: 1, height: 350, display: 'flex', flexDirection: 'column' }}>
          <h3 style={{ marginBottom: '16px', flexShrink: 0 }}>Top Sản Phẩm Bán Chạy</h3>
          <div style={{ flex: 1, minHeight: 0 }}>
            <ResponsiveContainer width="100%" height="100%">
            <BarChart data={((topProducts.data as any) || []).slice(0, 5)} layout="vertical">
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis type="number" />
              <YAxis dataKey="productName" type="category" width={100} />
              <RechartsTooltip />
              <Legend />
              <Bar dataKey="quantity" name="Số lượng bán" fill="#8b5cf6" radius={[0, 4, 4, 0]} />
            </BarChart>
            </ResponsiveContainer>
          </div>
        </div>
        <div className="card" style={{ flex: 1, height: 350, display: 'flex', flexDirection: 'column' }}>
          <h3 style={{ marginBottom: '16px', flexShrink: 0 }}>Hiệu suất sử dụng Bàn</h3>
          <div style={{ flex: 1, minHeight: 0 }}>
            <ResponsiveContainer width="100%" height="100%">
            <BarChart data={((tableUsage.data as any) || []).slice(0, 5)}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="tableName" />
              <YAxis />
              <RechartsTooltip />
              <Legend />
              <Bar dataKey="totalMinutes" name="Tổng phút sử dụng" fill="#f59e0b" radius={[4, 4, 0, 0]} />
            </BarChart>
            </ResponsiveContainer>
          </div>
        </div>
        </div>
      </section>
    </>
  );
}
