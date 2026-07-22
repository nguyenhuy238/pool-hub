"use client";

import { useState } from "react";
import { DataTable, PageHeader, StateBlock, useLoad } from "@/components/ui";
import { reportsApi } from "@/lib/api/endpoints";
import { money } from "@/lib/status";
import { useToast } from "@/components/toast";
import { formatDateLocal, getCurrentVietnamMonthRange, getVietnamDateInputValue } from "@/lib/dateTime";

export default function ReportsPage() {
  const toast = useToast();
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

  const revenue = useLoad(() => reportsApi.revenue(range), [range]);
  const tables = useLoad(() => reportsApi.tableUsage(range), [range]);
  const products = useLoad(() => reportsApi.products(range), [range]);
  const bookings = useLoad(() => reportsApi.bookings(range), [range]);
  const customers = useLoad(() => reportsApi.customers(range), [range]);
  const paymentMethods = useLoad(() => reportsApi.paymentMethods(range), [range]);
  const inventory = useLoad(() => reportsApi.inventory(range), [range]);

  const exportToCsv = (filename: string, rows: (string | number)[][]) => {
    const content = "\uFEFF" + rows.map(r => r.map(cell => `"${String(cell ?? "").replace(/"/g, '""')}"`).join(",")).join("\n");
    const blob = new Blob([content], { type: "text/csv;charset=utf-8;" });
    const link = document.createElement("a");
    link.href = URL.createObjectURL(blob);
    link.download = `${filename}_${new Date().toISOString().split("T")[0]}.csv`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  const exportAllToExcel = () => {
    const rows: (string | number)[][] = [];
    rows.push(["=== BÁO CÁO TỔNG HỢP POOLHUB ==="]);
    rows.push([`Ngày xuất: ${formatDateLocal(new Date().toISOString())}`]);
    if (range.FromDate || range.ToDate) {
      rows.push([`Giai đoạn: ${range.FromDate || '...'} đến ${range.ToDate || '...'}`]);
    }
    rows.push([]);

    if (revenue.data?.length) {
      rows.push(["1. BÁO CÁO DOANH THU"]);
      rows.push(["Ngày", "Doanh thu (VNĐ)", "Số lượng hóa đơn"]);
      revenue.data.forEach((r: any) => rows.push([formatDateLocal(r.date), r.revenue, r.invoiceCount]));
      rows.push([]);
    }

    if (tables.data?.length) {
      rows.push(["2. TẦN SUẤT SỬ DỤNG BÀN"]);
      rows.push(["Tên bàn", "Số phiên chơi", "Tổng thời gian (phút)"]);
      tables.data.forEach((r: any) => rows.push([r.tableName, r.sessionCount, r.totalMinutes]));
      rows.push([]);
    }

    if (products.data?.length) {
      rows.push(["3. TOP SẢN PHẨM BÁN CHẠY"]);
      rows.push(["Tên sản phẩm", "Số lượng đã bán", "Doanh thu mang lại (VNĐ)"]);
      products.data.forEach((r: any) => rows.push([r.productName, r.quantity, r.revenue]));
      rows.push([]);
    }

    if (bookings.data?.length) {
      rows.push(["4. THỐNG KÊ ĐẶT BÀN"]);
      rows.push(["Trạng thái", "Số lượng"]);
      const map: any = { 1: "Chờ xác nhận", 2: "Đã xác nhận", 3: "Đã hoàn thành", 4: "Đã hủy" };
      bookings.data.forEach((r: any) => rows.push([map[r.status] || r.status, r.count]));
      rows.push([]);
    }

    if (customers.data?.length) {
      rows.push(["5. KHÁCH HÀNG"]);
      rows.push(["Khách hàng", "Lượt đặt bàn", "Phiên chơi", "Doanh thu (VNĐ)"]);
      customers.data.forEach((r: any) => rows.push([r.customerName, r.bookingCount, r.sessionCount, r.revenue]));
      rows.push([]);
    }

    if (paymentMethods.data?.length) {
      rows.push(["6. PHƯƠNG THỨC THANH TOÁN"]);
      rows.push(["Phương thức", "Giao dịch", "Tổng tiền (VNĐ)"]);
      paymentMethods.data.forEach((r: any) => rows.push([r.paymentMethodName, r.paymentCount, r.amount]));
      rows.push([]);
    }

    if (inventory.data?.length) {
      rows.push(["7. TỒN KHO"]);
      rows.push(["Sản phẩm", "Tồn hiện tại", "Biến động", "Giá trị (VNĐ)"]);
      inventory.data.forEach((r: any) => rows.push([r.productName, r.currentStock, r.netMovement, r.inventoryValue]));
      rows.push([]);
    }

    exportToCsv("Bao_cao_tong_hop_PoolHub", rows);
    toast("Đã xuất file Excel tổng hợp thành công!", "success");
  };

  return (
    <>
      <PageHeader 
        title="Báo cáo chi tiết" 
        description="Phân tích doanh thu, hiệu suất bàn, sản phẩm, đặt bàn, khách hàng và tồn kho."
        action={
          <button className="primary-btn" onClick={exportAllToExcel} style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
            <span>📊</span> Xuất toàn bộ ra Excel
          </button>
        }
      />
      
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
        <button className="ghost-btn" onClick={() => setRange({ FromDate: "", ToDate: "" })}>Xóa bộ lọc</button>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px', borderBottom: '1px solid var(--line)', paddingBottom: '12px' }}>
            <h3 style={{ margin: 0 }}>Báo cáo Doanh thu</h3>
            {revenue.data && revenue.data.length > 0 && (
              <button className="ghost-btn compact" onClick={() => {
                const rows = [["Ngày", "Doanh thu (VNĐ)", "Số lượng Hóa đơn"], ...revenue.data!.map((r: any) => [formatDateLocal(r.date), r.revenue, r.invoiceCount])];
                exportToCsv("Bao_cao_Doanh_thu", rows);
                toast("Xuất Excel doanh thu thành công!", "success");
              }}>📊 Xuất Excel</button>
            )}
          </div>
          <StateBlock loading={revenue.loading} error={revenue.error} empty={!revenue.loading && !revenue.data?.length} />
          {revenue.data && revenue.data.length > 0 && (
            <DataTable 
              rows={revenue.data} 
              columns={[
                { key: "date", label: "Ngày", render: (row: any) => formatDateLocal(row.date) }, 
                { key: "revenue", label: "Doanh thu", render: (row: any) => <strong style={{ color: '#187344' }}>{money(row.revenue)}</strong> }, 
                { key: "invoiceCount", label: "Số lượng Hóa đơn" }
              ]} 
            />
          )}
        </div>

        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px', borderBottom: '1px solid var(--line)', paddingBottom: '12px' }}>
            <h3 style={{ margin: 0 }}>Tần suất sử dụng Bàn</h3>
            {tables.data && tables.data.length > 0 && (
              <button className="ghost-btn compact" onClick={() => {
                const rows = [["Tên Bàn", "Số phiên chơi", "Tổng thời gian (phút)"], ...tables.data!.map((r: any) => [r.tableName, r.sessionCount, r.totalMinutes])];
                exportToCsv("Bao_cao_Su_dung_Ban", rows);
                toast("Xuất Excel tần suất sử dụng bàn thành công!", "success");
              }}>📊 Xuất Excel</button>
            )}
          </div>
          <StateBlock loading={tables.loading} error={tables.error} empty={!tables.loading && !tables.data?.length} />
          {tables.data && tables.data.length > 0 && (
            <DataTable 
              rows={tables.data} 
              columns={[
                { key: "tableName", label: "Tên Bàn", render: (row: any) => <strong>{row.tableName}</strong> }, 
                { key: "sessionCount", label: "Số phiên chơi" },
                { key: "totalMinutes", label: "Tổng thời gian (phút)" }
              ]} 
            />
          )}
        </div>

        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px', borderBottom: '1px solid var(--line)', paddingBottom: '12px' }}>
            <h3 style={{ margin: 0 }}>Top Sản phẩm Bán chạy</h3>
            {products.data && products.data.length > 0 && (
              <button className="ghost-btn compact" onClick={() => {
                const rows = [["Tên Sản phẩm", "Số lượng đã bán", "Doanh thu mang lại (VNĐ)"], ...products.data!.map((r: any) => [r.productName, r.quantity, r.revenue])];
                exportToCsv("Bao_cao_Top_San_pham", rows);
                toast("Xuất Excel top sản phẩm thành công!", "success");
              }}>📊 Xuất Excel</button>
            )}
          </div>
          <StateBlock loading={products.loading} error={products.error} empty={!products.loading && !products.data?.length} />
          {products.data && products.data.length > 0 && (
            <DataTable 
              rows={products.data} 
              columns={[
                { key: "productName", label: "Tên Sản phẩm", render: (row: any) => <strong>{row.productName}</strong> }, 
                { key: "quantity", label: "Số lượng đã bán" }, 
                { key: "revenue", label: "Doanh thu mang lại", render: (row: any) => <span style={{ color: 'var(--brand)', fontWeight: 600 }}>{money(row.revenue)}</span> }
              ]} 
            />
          )}
        </div>

        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px', borderBottom: '1px solid var(--line)', paddingBottom: '12px' }}>
            <h3 style={{ margin: 0 }}>Thống kê trạng thái đặt bàn</h3>
            {bookings.data && bookings.data.length > 0 && (
              <button className="ghost-btn compact" onClick={() => {
                const statusMap: any = { 1: "Chờ xác nhận", 2: "Đã xác nhận", 3: "Đã hoàn thành", 4: "Đã hủy" };
                const rows = [["Trạng thái đặt bàn", "Số lượng"], ...bookings.data!.map((r: any) => [statusMap[r.status] || r.status, r.count])];
                exportToCsv("Bao_cao_Dat_ban", rows);
                toast("Xuất Excel thống kê đặt bàn thành công!", "success");
              }}>📊 Xuất Excel</button>
            )}
          </div>
          <StateBlock loading={bookings.loading} error={bookings.error} empty={!bookings.loading && !bookings.data?.length} />
          {bookings.data && bookings.data.length > 0 && (
            <DataTable 
              rows={bookings.data} 
              columns={[
                { key: "status", label: "Trạng thái đặt bàn", render: (row: any) => {
                  const statusMap: any = { 1: "Chờ xác nhận", 2: "Đã xác nhận", 3: "Đã hoàn thành", 4: "Đã hủy" };
                  const badgeMap: any = { 1: "yellow", 2: "green", 3: "blue", 4: "red" };
                  return <span className={`badge ${badgeMap[row.status] || 'neutral'}`}>{statusMap[row.status] || row.status}</span>;
                }}, 
                { key: "count", label: "Số lượng" }
              ]} 
            />
          )}
        </div>

        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px', borderBottom: '1px solid var(--line)', paddingBottom: '12px' }}>
            <h3 style={{ margin: 0 }}>Khách hàng</h3>
            {customers.data && customers.data.length > 0 && (
              <button className="ghost-btn compact" onClick={() => {
                const rows = [["Khách hàng", "Lượt đặt bàn", "Phiên chơi", "Doanh thu (VNĐ)"], ...customers.data!.map((r: any) => [r.customerName, r.bookingCount, r.sessionCount, r.revenue])];
                exportToCsv("Bao_cao_Khach_hang", rows);
                toast("Xuất Excel khách hàng thành công!", "success");
              }}>📊 Xuất Excel</button>
            )}
          </div>
          <StateBlock loading={customers.loading} error={customers.error} empty={!customers.loading && !customers.data?.length} />
          {customers.data?.length ? <DataTable rows={customers.data} columns={[
            { key: "customerName", label: "Khách hàng" },
            { key: "bookingCount", label: "Lượt đặt bàn" },
            { key: "sessionCount", label: "Phiên chơi" },
            { key: "revenue", label: "Doanh thu", render: (row: any) => money(row.revenue) }
          ]} /> : null}
        </div>

        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px', borderBottom: '1px solid var(--line)', paddingBottom: '12px' }}>
            <h3 style={{ margin: 0 }}>Phương thức thanh toán</h3>
            {paymentMethods.data && paymentMethods.data.length > 0 && (
              <button className="ghost-btn compact" onClick={() => {
                const rows = [["Phương thức", "Giao dịch", "Tổng tiền (VNĐ)"], ...paymentMethods.data!.map((r: any) => [r.paymentMethodName, r.paymentCount, r.amount])];
                exportToCsv("Bao_cao_Phuong_thuc_thanh_toan", rows);
                toast("Xuất Excel phương thức thanh toán thành công!", "success");
              }}>📊 Xuất Excel</button>
            )}
          </div>
          <StateBlock loading={paymentMethods.loading} error={paymentMethods.error} empty={!paymentMethods.loading && !paymentMethods.data?.length} />
          {paymentMethods.data?.length ? <DataTable rows={paymentMethods.data} columns={[
            { key: "paymentMethodName", label: "Phương thức" },
            { key: "paymentCount", label: "Giao dịch" },
            { key: "amount", label: "Tổng tiền", render: (row: any) => money(row.amount) }
          ]} /> : null}
        </div>

        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px', borderBottom: '1px solid var(--line)', paddingBottom: '12px' }}>
            <h3 style={{ margin: 0 }}>Tồn kho</h3>
            {inventory.data && inventory.data.length > 0 && (
              <button className="ghost-btn compact" onClick={() => {
                const rows = [["Sản phẩm", "Tồn hiện tại", "Biến động", "Giá trị (VNĐ)"], ...inventory.data!.map((r: any) => [r.productName, r.currentStock, r.netMovement, r.inventoryValue])];
                exportToCsv("Bao_cao_Ton_kho", rows);
                toast("Xuất Excel tồn kho thành công!", "success");
              }}>📊 Xuất Excel</button>
            )}
          </div>
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
