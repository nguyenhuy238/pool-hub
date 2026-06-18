"use client";

import { useState } from "react";
import { DataTable, PageHeader, StateBlock, useLoad } from "@/components/ui";
import { reportsApi } from "@/lib/api/endpoints";

export default function ReportsPage() {
  const [range, setRange] = useState({ FromDate: "", ToDate: "" });
  const revenue = useLoad(() => reportsApi.revenue(range), [range]);
  const tables = useLoad(() => reportsApi.tableUsage(range), [range]);
  const products = useLoad(() => reportsApi.products(range), [range]);
  const bookings = useLoad(() => reportsApi.bookings(range), [range]);
  return <>
    <PageHeader title="Reports" description="Báo cáo từ dữ liệu invoice, session, order và booking." />
    <div className="card list-controls">
      <label><span>Từ ngày</span><input type="date" value={range.FromDate} onChange={e => setRange({ ...range, FromDate: e.target.value })} /></label>
      <label><span>Đến ngày</span><input type="date" value={range.ToDate} onChange={e => setRange({ ...range, ToDate: e.target.value })} /></label>
    </div>
    <h2>Doanh thu</h2><StateBlock loading={revenue.loading} error={revenue.error} empty={!revenue.loading && !revenue.data?.length} />
    <DataTable rows={revenue.data || []} columns={[{ key: "date", label: "Ngày" }, { key: "revenue", label: "Doanh thu" }, { key: "invoiceCount", label: "Hóa đơn" }]} />
    <h2>Bàn sử dụng nhiều</h2><StateBlock loading={tables.loading} error={tables.error} empty={!tables.loading && !tables.data?.length} />
    <DataTable rows={tables.data || []} columns={[{ key: "tableName", label: "Bàn" }, { key: "sessionCount", label: "Sessions" }, { key: "totalMinutes", label: "Phút" }]} />
    <h2>Sản phẩm bán chạy</h2><StateBlock loading={products.loading} error={products.error} empty={!products.loading && !products.data?.length} />
    <DataTable rows={products.data || []} columns={[{ key: "productName", label: "Sản phẩm" }, { key: "quantity", label: "Số lượng" }, { key: "revenue", label: "Doanh thu" }]} />
    <h2>Booking conversion</h2><StateBlock loading={bookings.loading} error={bookings.error} empty={!bookings.loading && !bookings.data?.length} />
    <DataTable rows={bookings.data || []} columns={[{ key: "status", label: "Status" }, { key: "count", label: "Số lượng" }]} />
  </>;
}
