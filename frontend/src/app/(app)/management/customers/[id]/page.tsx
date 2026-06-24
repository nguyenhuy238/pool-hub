"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { customerApi } from "@/lib/api/endpoints";
import { DataTable, PageHeader, StateBlock } from "@/components/ui";
import { useToast } from "@/components/toast";
import { bookingStatus, dateTime, money, sessionStatus } from "@/lib/status";
import type { CustomerBookingHistory, CustomerDto, CustomerInvoiceHistory, CustomerSessionHistory } from "@/types";

export default function CustomerDetailPage({ params }: { params: { id: string } }) {
  const router = useRouter();
  const toast = useToast();
  const customerId = parseInt(params.id, 10);

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<Error | null>(null);
  const [customer, setCustomer] = useState<CustomerDto | null>(null);
  const [bookings, setBookings] = useState<CustomerBookingHistory[]>([]);
  const [sessions, setSessions] = useState<CustomerSessionHistory[]>([]);
  const [invoices, setInvoices] = useState<CustomerInvoiceHistory[]>([]);

  useEffect(() => {
    if (isNaN(customerId)) {
      setError(new Error("Invalid Customer ID"));
      setLoading(false);
      return;
    }

    Promise.all([
      customerApi.detail(customerId),
      customerApi.bookingHistory(customerId),
      customerApi.sessionHistory(customerId),
      customerApi.invoiceHistory(customerId)
    ])
      .then(([customerResult, bookingResult, sessionResult, invoiceResult]) => {
        setCustomer(customerResult);
        setBookings(bookingResult.items ?? []);
        setSessions(sessionResult.items ?? []);
        setInvoices(invoiceResult.items ?? []);
        setLoading(false);
      })
      .catch(err => {
        setError(err);
        setLoading(false);
      });
  }, [customerId]);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!customer) return;

    setSaving(true);
    try {
      await customerApi.update(customer.customerId, {
        fullName: customer.fullName,
        email: customer.email,
        note: customer.note,
        status: customer.status
      });
      toast("Đã cập nhật thông tin khách hàng", "success");
      router.push("/management/customers");
    } catch (err: any) {
      toast(err.message || "Cập nhật thất bại", "error");
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <PageHeader
        title="Chi tiết khách hàng"
        description="Xem và cập nhật thông tin khách hàng."
        action={
          <button className="ghost-btn" onClick={() => router.back()}>Trở lại</button>
        }
      />

      <StateBlock loading={loading} error={error?.message} empty={!loading && !customer} />

      {!loading && customer && (
        <>
        <form className="card" onSubmit={handleSubmit} style={{ maxWidth: 800, margin: '0 auto' }}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px', marginBottom: '24px' }}>
            <div className="state-card" style={{ padding: '16px', background: '#f8fbfa', border: 'none' }}>
              <div className="metric">
                <span>Số điện thoại (Không thể đổi)</span>
                <strong style={{ fontSize: '20px' }}>{customer.phoneNumber || "N/A"}</strong>
              </div>
            </div>
            <div className="state-card" style={{ padding: '16px', background: '#f8fbfa', border: 'none' }}>
              <div className="metric">
                <span>Tổng số lần đặt bàn</span>
                <strong style={{ fontSize: '20px' }}>{customer.totalBookings}</strong>
              </div>
            </div>
          </div>

          <div className="form-grid" style={{ gridTemplateColumns: '1fr 1fr' }}>
            <div style={{ display: 'grid', gap: '6px' }}>
              <label>Họ và tên *</label>
              <input
                required
                value={customer.fullName}
                onChange={e => setCustomer({ ...customer, fullName: e.target.value })}
                placeholder="Nhập họ và tên"
              />
            </div>

            <div style={{ display: 'grid', gap: '6px' }}>
              <label>Email</label>
              <input
                type="email"
                value={customer.email || ""}
                onChange={e => setCustomer({ ...customer, email: e.target.value })}
                placeholder="Nhập email"
              />
            </div>
          </div>

          <div style={{ display: 'grid', gap: '6px', marginBottom: '18px' }}>
            <label>Ghi chú nội bộ</label>
            <textarea
              rows={4}
              value={customer.note || ""}
              onChange={e => setCustomer({ ...customer, note: e.target.value })}
              placeholder="Thêm ghi chú về khách hàng này..."
            />
          </div>

          <div style={{ display: 'grid', gap: '6px', marginBottom: '24px' }}>
            <label>Trạng thái</label>
            <select
              value={customer.status ? "true" : "false"}
              onChange={e => setCustomer({ ...customer, status: e.target.value === "true" })}
            >
              <option value="true">Đang hoạt động (Active)</option>
              <option value="false">Đã khóa (Inactive)</option>
            </select>
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px', borderTop: '1px solid var(--line)', paddingTop: '20px' }}>
            <button type="button" className="ghost-btn" onClick={() => router.back()}>Hủy</button>
            <button type="submit" className="primary-btn" disabled={saving}>
              {saving ? "Đang lưu..." : "Lưu Thay Đổi"}
            </button>
          </div>
        </form>
        <div className="grid-2" style={{ marginTop: 24 }}>
          <section className="card">
            <h3>Lịch sử booking</h3>
            <DataTable rows={bookings as unknown as Record<string, unknown>[]} columns={[
              { key: "bookingCode", label: "Mã" },
              { key: "tableName", label: "Bàn" },
              { key: "startTimeUtc", label: "Bắt đầu", render: (row) => dateTime(String(row.startTimeUtc)) },
              { key: "status", label: "Trạng thái", render: (row) => bookingStatus[Number(row.status)] ?? String(row.status) }
            ]} />
          </section>
          <section className="card">
            <h3>Lịch sử phiên chơi</h3>
            <DataTable rows={sessions as unknown as Record<string, unknown>[]} columns={[
              { key: "sessionCode", label: "Mã" },
              { key: "startedAtUtc", label: "Bắt đầu", render: (row) => dateTime(String(row.startedAtUtc)) },
              { key: "endedAtUtc", label: "Kết thúc", render: (row) => dateTime(String(row.endedAtUtc ?? "")) },
              { key: "status", label: "Trạng thái", render: (row) => sessionStatus[Number(row.status)] ?? String(row.status) }
            ]} />
          </section>
        </div>
        <section className="card" style={{ marginTop: 24 }}>
          <h3>Lịch sử hóa đơn</h3>
          <DataTable rows={invoices as unknown as Record<string, unknown>[]} columns={[
            { key: "invoiceCode", label: "Mã hóa đơn" },
            { key: "grandTotalAmount", label: "Tổng tiền", render: (row) => money(Number(row.grandTotalAmount)) },
            { key: "paidAmount", label: "Đã trả", render: (row) => money(Number(row.paidAmount)) },
            { key: "issuedAtUtc", label: "Ngày xuất", render: (row) => dateTime(String(row.issuedAtUtc ?? "")) }
          ]} />
        </section>
        </>
      )}
    </>
  );
}
