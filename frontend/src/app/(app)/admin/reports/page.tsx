"use client";

import { useState } from "react";
import { DataTable, PageHeader, StateBlock, useLoad } from "@/components/ui";
import { reportsApi } from "@/lib/api/endpoints";
import { money } from "@/lib/status";

export default function ReportsPage() {
  const [range, setRange] = useState({ FromDate: "", ToDate: "" });
  const applyPreset = (preset: string) => {
    const now = new Date();
    const toYMD = (d: Date) => {
      const offset = d.getTimezoneOffset() * 60000;
      return new Date(d.getTime() - offset).toISOString().split("T")[0];
    };
    
    if (preset === "today") {
      const d = toYMD(now);
      setRange({ FromDate: d, ToDate: d });
    } else if (preset === "this_month") {
      setRange({ FromDate: toYMD(new Date(now.getFullYear(), now.getMonth(), 1)), ToDate: toYMD(new Date(now.getFullYear(), now.getMonth() + 1, 0)) });
    } else {
      setRange({ FromDate: "", ToDate: "" });
    }
  };

  const revenue = useLoad(() => reportsApi.revenue(range), [range]);
  const tables = useLoad(() => reportsApi.tableUsage(range), [range]);
  const products = useLoad(() => reportsApi.products(range), [range]);
  const bookings = useLoad(() => reportsApi.bookings(range), [range]);
  const customers = useLoad(() => reportsApi.customers(range), [range]);
  const paymentMethods = useLoad(() => reportsApi.paymentMethods(range), [range]);
  const inventory = useLoad(() => reportsApi.inventory(range), [range]);

  return (
    <>
      <PageHeader title="Chi tiết Báo cáo" description="Dữ liệu phân tích chuyên sâu về Doanh thu, Bàn, Sản phẩm và Booking." />
      
      <div className="card list-controls" style={{ marginBottom: '24px', display: 'flex', gap: '16px', alignItems: 'center', flexWrap: 'wrap' }}>
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

      <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
        <div className="card">
          <h3 style={{ marginBottom: '16px', borderBottom: '1px solid var(--border-color)', paddingBottom: '12px' }}>Báo cáo Doanh thu</h3>
          <StateBlock loading={revenue.loading} error={revenue.error} empty={!revenue.loading && !revenue.data?.length} />
          {revenue.data && revenue.data.length > 0 && (
            <DataTable 
              rows={revenue.data} 
              columns={[
                { key: "date", label: "Ngày", render: (row: any) => new Date(row.date).toLocaleDateString('vi-VN') }, 
                { key: "revenue", label: "Doanh thu", render: (row: any) => <strong style={{ color: 'var(--success-color)' }}>{money(row.revenue)}</strong> }, 
                { key: "invoiceCount", label: "Số lượng Hóa đơn" }
              ]} 
            />
          )}
        </div>

        <div className="card">
          <h3 style={{ marginBottom: '16px', borderBottom: '1px solid var(--border-color)', paddingBottom: '12px' }}>Tần suất sử dụng Bàn</h3>
          <StateBlock loading={tables.loading} error={tables.error} empty={!tables.loading && !tables.data?.length} />
          {tables.data && tables.data.length > 0 && (
            <DataTable 
              rows={tables.data} 
              columns={[
                { key: "tableName", label: "Tên Bàn", render: (row: any) => <strong>{row.tableName}</strong> }, 
                { key: "sessionCount", label: "Lượt chơi (Sessions)" }, 
                { key: "totalMinutes", label: "Tổng thời gian (phút)" }
              ]} 
            />
          )}
        </div>

        <div className="card">
          <h3 style={{ marginBottom: '16px', borderBottom: '1px solid var(--border-color)', paddingBottom: '12px' }}>Top Sản phẩm Bán chạy</h3>
          <StateBlock loading={products.loading} error={products.error} empty={!products.loading && !products.data?.length} />
          {products.data && products.data.length > 0 && (
            <DataTable 
              rows={products.data} 
              columns={[
                { key: "productName", label: "Tên Sản phẩm", render: (row: any) => <strong>{row.productName}</strong> }, 
                { key: "quantity", label: "Số lượng đã bán" }, 
                { key: "revenue", label: "Doanh thu mang lại", render: (row: any) => <span style={{ color: 'var(--primary-color)', fontWeight: 600 }}>{money(row.revenue)}</span> }
              ]} 
            />
          )}
        </div>

        <div className="card">
          <h3 style={{ marginBottom: '16px', borderBottom: '1px solid var(--border-color)', paddingBottom: '12px' }}>Tỷ lệ chuyển đổi Booking</h3>
          <StateBlock loading={bookings.loading} error={bookings.error} empty={!bookings.loading && !bookings.data?.length} />
          {bookings.data && bookings.data.length > 0 && (
            <DataTable 
              rows={bookings.data} 
              columns={[
                { key: "status", label: "Trạng thái Booking", render: (row: any) => {
                  const statusMap: any = { 1: "Chờ xác nhận", 2: "Đã xác nhận", 3: "Đã hoàn thành", 4: "Đã hủy" };
                  const badgeMap: any = { 1: "warning", 2: "success", 3: "primary", 4: "danger" };
                  return <span className={`badge badge-${badgeMap[row.status] || 'neutral'}`}>{statusMap[row.status] || row.status}</span>;
                }}, 
                { key: "count", label: "Số lượng" }
              ]} 
            />
          )}
        </div>

        <div className="card">
          <h3>Khách hàng</h3>
          <StateBlock loading={customers.loading} error={customers.error} empty={!customers.loading && !customers.data?.length} />
          {customers.data?.length ? <DataTable rows={customers.data} columns={[
            { key: "customerName", label: "Khách hàng" },
            { key: "bookingCount", label: "Booking" },
            { key: "sessionCount", label: "Session" },
            { key: "revenue", label: "Doanh thu", render: (row: any) => money(row.revenue) }
          ]} /> : null}
        </div>

        <div className="card">
          <h3>Phương thức thanh toán</h3>
          <StateBlock loading={paymentMethods.loading} error={paymentMethods.error} empty={!paymentMethods.loading && !paymentMethods.data?.length} />
          {paymentMethods.data?.length ? <DataTable rows={paymentMethods.data} columns={[
            { key: "paymentMethodName", label: "Phương thức" },
            { key: "paymentCount", label: "Giao dịch" },
            { key: "amount", label: "Tổng tiền", render: (row: any) => money(row.amount) }
          ]} /> : null}
        </div>

        <div className="card">
          <h3>Tồn kho</h3>
          <StateBlock loading={inventory.loading} error={inventory.error} empty={!inventory.loading && !inventory.data?.length} />
          {inventory.data?.length ? <DataTable rows={inventory.data} columns={[
            { key: "productName", label: "Sản phẩm" },
            { key: "currentStock", label: "Tồn hiện tại" },
            { key: "netMovement", label: "Biến động" },
            { key: "inventoryValue", label: "Giá trị", render: (row: any) => money(row.inventoryValue) }
          ]} /> : null}
        </div>
      </div>
    </>
  );
}
