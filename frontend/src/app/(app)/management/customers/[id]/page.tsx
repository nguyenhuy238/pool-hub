"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { customerApi, discountApi } from "@/lib/api/endpoints";
import { Badge, DataTable, Modal, PageHeader, Pagination, StateBlock } from "@/components/ui";
import { useToast } from "@/components/toast";
import { bookingStatus, dateTime, money, sessionStatus } from "@/lib/status";
import type { CustomerBookingHistory, CustomerDto, CustomerInvoiceHistory, CustomerPointHistory, CustomerSessionHistory, Discount } from "@/types";

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
  const [pointHistories, setPointHistories] = useState<CustomerPointHistory[]>([]);
  const [voucherTemplates, setVoucherTemplates] = useState<Discount[]>([]);
  const [exchangedVouchers, setExchangedVouchers] = useState<Discount[]>([]);
  const [voucherModalOpen, setVoucherModalOpen] = useState(false);
  const [exchangingId, setExchangingId] = useState<number | null>(null);

  // Tabs control
  const [activeTab, setActiveTab] = useState<"bookings" | "sessions" | "invoices" | "vouchers" | "points">("bookings");

  // Bookings Filter & Pagination
  const [bookingSearch, setBookingSearch] = useState("");
  const [bookingStatusFilter, setBookingStatusFilter] = useState("");
  const [bookingPage, setBookingPage] = useState(1);
  const [bookingPageSize, setBookingPageSize] = useState(10);

  const filteredBookings = useMemo(() => {
    return bookings.filter(b => {
      if (bookingStatusFilter && String(b.status) !== bookingStatusFilter) return false;
      if (bookingSearch.trim()) {
        const q = bookingSearch.trim().toLowerCase();
        const matchCode = (b.bookingCode || "").toLowerCase().includes(q);
        const matchTable = (b.tableName || "").toLowerCase().includes(q);
        if (!matchCode && !matchTable) return false;
      }
      return true;
    });
  }, [bookings, bookingSearch, bookingStatusFilter]);

  const bookingTotalPages = Math.max(1, Math.ceil(filteredBookings.length / bookingPageSize));
  const paginatedBookings = useMemo(() => {
    const start = (bookingPage - 1) * bookingPageSize;
    return filteredBookings.slice(start, start + bookingPageSize);
  }, [filteredBookings, bookingPage, bookingPageSize]);

  // Sessions Filter & Pagination
  const [sessionSearch, setSessionSearch] = useState("");
  const [sessionStatusFilter, setSessionStatusFilter] = useState("");
  const [sessionPage, setSessionPage] = useState(1);
  const [sessionPageSize, setSessionPageSize] = useState(10);

  const filteredSessions = useMemo(() => {
    return sessions.filter(s => {
      if (sessionStatusFilter && String(s.status) !== sessionStatusFilter) return false;
      if (sessionSearch.trim()) {
        const q = sessionSearch.trim().toLowerCase();
        const matchCode = (s.sessionCode || "").toLowerCase().includes(q);
        if (!matchCode) return false;
      }
      return true;
    });
  }, [sessions, sessionSearch, sessionStatusFilter]);

  const sessionTotalPages = Math.max(1, Math.ceil(filteredSessions.length / sessionPageSize));
  const paginatedSessions = useMemo(() => {
    const start = (sessionPage - 1) * sessionPageSize;
    return filteredSessions.slice(start, start + sessionPageSize);
  }, [filteredSessions, sessionPage, sessionPageSize]);

  // Invoices Filter & Pagination
  const [invoiceSearch, setInvoiceSearch] = useState("");
  const [invoiceStatusFilter, setInvoiceStatusFilter] = useState("");
  const [invoicePage, setInvoicePage] = useState(1);
  const [invoicePageSize, setInvoicePageSize] = useState(10);

  const filteredInvoices = useMemo(() => {
    return invoices.filter(inv => {
      if (invoiceStatusFilter) {
        if (invoiceStatusFilter === "cancelled" && Number((inv as any).status) !== 3) return false;
        if (invoiceStatusFilter === "paid" && Number((inv as any).paymentStatus) !== 3 && Number((inv as any).paidAmount) < Number((inv as any).grandTotalAmount)) return false;
        if (invoiceStatusFilter === "unpaid" && Number((inv as any).paymentStatus) !== 1 && Number((inv as any).paidAmount) !== 0) return false;
        if (invoiceStatusFilter === "partial" && Number((inv as any).paymentStatus) !== 2) return false;
      }
      if (invoiceSearch.trim()) {
        const q = invoiceSearch.trim().toLowerCase();
        const matchCode = ((inv as any).invoiceCode || "").toLowerCase().includes(q);
        if (!matchCode) return false;
      }
      return true;
    });
  }, [invoices, invoiceSearch, invoiceStatusFilter]);

  const invoiceTotalPages = Math.max(1, Math.ceil(filteredInvoices.length / invoicePageSize));
  const paginatedInvoices = useMemo(() => {
    const start = (invoicePage - 1) * invoicePageSize;
    return filteredInvoices.slice(start, start + invoicePageSize);
  }, [filteredInvoices, invoicePage, invoicePageSize]);

  // Exchanged Vouchers Filter & Pagination
  const [voucherSearch, setVoucherSearch] = useState("");
  const [voucherStatusFilter, setVoucherStatusFilter] = useState("");
  const [voucherPage, setVoucherPage] = useState(1);
  const [voucherPageSize, setVoucherPageSize] = useState(10);

  const filteredVouchers = useMemo(() => {
    const now = new Date();
    return exchangedVouchers.filter(v => {
      if (voucherStatusFilter) {
        const isUsed = Number(v.usageCount || 0) >= Number(v.maxUsage || 1);
        const isExpired = v.endsAtUtc && new Date(String(v.endsAtUtc)) < now;
        if (voucherStatusFilter === "active" && (isUsed || !v.isActive || isExpired)) return false;
        if (voucherStatusFilter === "used" && !isUsed) return false;
        if (voucherStatusFilter === "expired" && (!isExpired || isUsed)) return false;
      }
      if (voucherSearch.trim()) {
        const q = voucherSearch.trim().toLowerCase();
        const matchCode = (v.discountCode || "").toLowerCase().includes(q);
        const matchName = (v.name || "").toLowerCase().includes(q);
        if (!matchCode && !matchName) return false;
      }
      return true;
    });
  }, [exchangedVouchers, voucherSearch, voucherStatusFilter]);

  const voucherTotalPages = Math.max(1, Math.ceil(filteredVouchers.length / voucherPageSize));
  const paginatedVouchers = useMemo(() => {
    const start = (voucherPage - 1) * voucherPageSize;
    return filteredVouchers.slice(start, start + voucherPageSize);
  }, [filteredVouchers, voucherPage, voucherPageSize]);

  // Point Histories Filter & Pagination
  const [pointSearch, setPointSearch] = useState("");
  const [pointTypeFilter, setPointTypeFilter] = useState("");
  const [pointPage, setPointPage] = useState(1);
  const [pointPageSize, setPointPageSize] = useState(10);

  const filteredPointHistories = useMemo(() => {
    return pointHistories.filter(p => {
      if (pointTypeFilter && String(p.transactionType) !== pointTypeFilter) return false;
      if (pointSearch.trim()) {
        const q = pointSearch.trim().toLowerCase();
        const matchDesc = (p.description || "").toLowerCase().includes(q);
        if (!matchDesc) return false;
      }
      return true;
    });
  }, [pointHistories, pointSearch, pointTypeFilter]);

  const pointTotalPages = Math.max(1, Math.ceil(filteredPointHistories.length / pointPageSize));
  const paginatedPointHistories = useMemo(() => {
    const start = (pointPage - 1) * pointPageSize;
    return filteredPointHistories.slice(start, start + pointPageSize);
  }, [filteredPointHistories, pointPage, pointPageSize]);

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
      customerApi.invoiceHistory(customerId),
      customerApi.pointHistory(customerId),
      discountApi.list({ IsActive: true, OnlyTemplates: true, PageSize: 100 }),
      discountApi.list({ CustomerId: customerId, PageSize: 100 })
    ])
      .then(([customerResult, bookingResult, sessionResult, invoiceResult, pointResult, discountResult, exchangedResult]) => {
        setCustomer(customerResult);
        setBookings(bookingResult.items ?? []);
        setSessions(sessionResult.items ?? []);
        setInvoices(invoiceResult.items ?? []);
        setPointHistories(pointResult.items ?? []);
        
        const exVouchers = Array.isArray(exchangedResult) ? exchangedResult : (exchangedResult as any)?.items || [];
        setExchangedVouchers(exVouchers);
        
        const now = new Date();
        const discounts = Array.isArray(discountResult) ? discountResult : (discountResult as any)?.items || [];
        setVoucherTemplates(discounts.filter((d: Discount) => {
          if (!d.isVoucher || !d.isActive || !(d.pointsRequired && d.pointsRequired > 0)) return false;
          if (d.endsAtUtc && new Date(d.endsAtUtc) <= now) return false;
          return true;
        }));
        setLoading(false);
      })
      .catch(err => {
        setError(err);
        setLoading(false);
      });
  }, [customerId]);

  const handleExchangeVoucher = async (template: Discount) => {
    if (!customer) return;
    if ((customer.loyaltyPoints ?? 0) < (template.pointsRequired ?? 0)) {
      toast("Khách hàng không đủ điểm tích lũy để đổi gói này.", "error");
      return;
    }
    setExchangingId(template.discountId);
    try {
      const res = await customerApi.exchangeVoucher(customer.customerId, template.discountId);
      toast(`Đổi voucher thành công! Mã voucher: ${(res as any).discountCode || "Mới"} (Hạn dùng theo gói mẫu)`, "success");
      setVoucherModalOpen(false);
      const [updatedCust, updatedPoints, updatedExchanged] = await Promise.all([
        customerApi.detail(customer.customerId),
        customerApi.pointHistory(customer.customerId),
        discountApi.list({ CustomerId: customer.customerId, PageSize: 100 })
      ]);
      setCustomer(updatedCust);
      setPointHistories(updatedPoints.items ?? []);
      setExchangedVouchers(Array.isArray(updatedExchanged) ? updatedExchanged : (updatedExchanged as any)?.items || []);
    } catch (err: any) {
      toast(err.message || "Đổi voucher thất bại", "error");
    } finally {
      setExchangingId(null);
    }
  };

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
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '20px', marginBottom: '24px' }}>
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
            <div className="state-card" style={{ padding: '16px', background: '#edfdf6', border: '1px solid #a7f3d0', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div className="metric">
                <span style={{ color: '#047857' }}>🪙 Điểm tích lũy hiện có</span>
                <strong style={{ fontSize: '24px', color: '#065f46' }}>{customer.loyaltyPoints?.toLocaleString() ?? 0}</strong>
                <span style={{ fontSize: '12px', color: '#059669' }}>Tổng tích lũy: {customer.totalPointsEarned?.toLocaleString() ?? 0}</span>
              </div>
              <button
                type="button"
                className="primary-btn"
                style={{ background: '#059669', color: '#fff', padding: '10px 16px', fontSize: '14px', borderRadius: '8px', border: 'none', cursor: 'pointer', whiteSpace: 'nowrap', boxShadow: '0 2px 6px rgba(5, 150, 105, 0.2)' }}
                onClick={() => setVoucherModalOpen(true)}
              >
                🎁 Đổi Voucher
              </button>
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
        <div style={{ marginTop: '28px' }}>
          <div style={{ 
            display: 'flex', 
            gap: '6px', 
            borderBottom: '2px solid #e2e8f0', 
            paddingBottom: '0px',
            overflowX: 'auto',
            marginBottom: '20px'
          }}>
            {[
              { key: "bookings", label: `📅 Lịch sử booking (${bookings.length})` },
              { key: "sessions", label: `🎱 Lịch sử phiên chơi (${sessions.length})` },
              { key: "invoices", label: `🧾 Lịch sử hóa đơn (${invoices.length})` },
              { key: "vouchers", label: `🎟️ Voucher đã đổi (${exchangedVouchers.length})` },
              { key: "points", label: `🪙 Lịch sử điểm (${pointHistories.length})` },
            ].map((tab) => {
              const active = activeTab === tab.key;
              return (
                <button
                  key={tab.key}
                  type="button"
                  onClick={() => setActiveTab(tab.key as any)}
                  style={{
                    padding: '12px 18px',
                    fontSize: '15px',
                    fontWeight: active ? 700 : 500,
                    color: active ? '#059669' : '#64748b',
                    background: active ? '#ecfdf5' : 'transparent',
                    border: 'none',
                    borderBottom: active ? '3px solid #059669' : '3px solid transparent',
                    borderRadius: '8px 8px 0 0',
                    cursor: 'pointer',
                    transition: 'all 0.15s ease',
                    whiteSpace: 'nowrap'
                  }}
                >
                  {tab.label}
                </button>
              );
            })}
          </div>

          {activeTab === "bookings" && (
            <section className="card">
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px', flexWrap: 'wrap', gap: '12px' }}>
                <h3 style={{ margin: 0 }}>Lịch sử booking ({filteredBookings.length})</h3>
                <div style={{ display: 'flex', gap: '10px', alignItems: 'center', flexWrap: 'wrap' }}>
                  <input
                    type="text"
                    placeholder="Tìm mã booking, tên bàn..."
                    value={bookingSearch}
                    onChange={e => { setBookingSearch(e.target.value); setBookingPage(1); }}
                    style={{ padding: '8px 12px', border: '1px solid #cbd5e1', borderRadius: '6px', fontSize: '14px', minWidth: '220px' }}
                  />
                  <select
                    value={bookingStatusFilter}
                    onChange={e => { setBookingStatusFilter(e.target.value); setBookingPage(1); }}
                    style={{ padding: '8px 12px', border: '1px solid #cbd5e1', borderRadius: '6px', fontSize: '14px' }}
                  >
                    <option value="">Tất cả trạng thái</option>
                    {Object.entries(bookingStatus).map(([k, v]) => (
                      <option key={k} value={k}>{v}</option>
                    ))}
                  </select>
                  <select
                    value={bookingPageSize}
                    onChange={e => { setBookingPageSize(Number(e.target.value)); setBookingPage(1); }}
                    style={{ padding: '8px 12px', border: '1px solid #cbd5e1', borderRadius: '6px', fontSize: '14px' }}
                  >
                    <option value={10}>10 dòng</option>
                    <option value={20}>20 dòng</option>
                    <option value={50}>50 dòng</option>
                  </select>
                </div>
              </div>
              <DataTable rows={paginatedBookings as unknown as Record<string, unknown>[]} columns={[
                { key: "bookingCode", label: "Mã" },
                { key: "tableName", label: "Bàn" },
                { key: "startTimeUtc", label: "Bắt đầu", render: (row) => dateTime(String(row.startTimeUtc)) },
                { key: "status", label: "Trạng thái", render: (row) => bookingStatus[Number(row.status)] ?? String(row.status) }
              ]} />
              <Pagination pageNumber={bookingPage} totalPages={bookingTotalPages} onChange={setBookingPage} />
            </section>
          )}

          {activeTab === "sessions" && (
            <section className="card">
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px', flexWrap: 'wrap', gap: '12px' }}>
                <h3 style={{ margin: 0 }}>Lịch sử phiên chơi ({filteredSessions.length})</h3>
                <div style={{ display: 'flex', gap: '10px', alignItems: 'center', flexWrap: 'wrap' }}>
                  <input
                    type="text"
                    placeholder="Tìm mã phiên..."
                    value={sessionSearch}
                    onChange={e => { setSessionSearch(e.target.value); setSessionPage(1); }}
                    style={{ padding: '8px 12px', border: '1px solid #cbd5e1', borderRadius: '6px', fontSize: '14px', minWidth: '220px' }}
                  />
                  <select
                    value={sessionStatusFilter}
                    onChange={e => { setSessionStatusFilter(e.target.value); setSessionPage(1); }}
                    style={{ padding: '8px 12px', border: '1px solid #cbd5e1', borderRadius: '6px', fontSize: '14px' }}
                  >
                    <option value="">Tất cả trạng thái</option>
                    {Object.entries(sessionStatus).map(([k, v]) => (
                      <option key={k} value={k}>{v}</option>
                    ))}
                  </select>
                  <select
                    value={sessionPageSize}
                    onChange={e => { setSessionPageSize(Number(e.target.value)); setSessionPage(1); }}
                    style={{ padding: '8px 12px', border: '1px solid #cbd5e1', borderRadius: '6px', fontSize: '14px' }}
                  >
                    <option value={10}>10 dòng</option>
                    <option value={20}>20 dòng</option>
                    <option value={50}>50 dòng</option>
                  </select>
                </div>
              </div>
              <DataTable rows={paginatedSessions as unknown as Record<string, unknown>[]} columns={[
                { key: "sessionCode", label: "Mã" },
                { key: "startedAtUtc", label: "Bắt đầu", render: (row) => dateTime(String(row.startedAtUtc)) },
                { key: "endedAtUtc", label: "Kết thúc", render: (row) => dateTime(String(row.endedAtUtc ?? "")) },
                { key: "status", label: "Trạng thái", render: (row) => sessionStatus[Number(row.status)] ?? String(row.status) }
              ]} />
              <Pagination pageNumber={sessionPage} totalPages={sessionTotalPages} onChange={setSessionPage} />
            </section>
          )}

          {activeTab === "invoices" && (
            <section className="card">
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px', flexWrap: 'wrap', gap: '12px' }}>
                <h3 style={{ margin: 0 }}>Lịch sử hóa đơn ({filteredInvoices.length})</h3>
                <div style={{ display: 'flex', gap: '10px', alignItems: 'center', flexWrap: 'wrap' }}>
                  <input
                    type="text"
                    placeholder="Tìm mã hóa đơn..."
                    value={invoiceSearch}
                    onChange={e => { setInvoiceSearch(e.target.value); setInvoicePage(1); }}
                    style={{ padding: '8px 12px', border: '1px solid #cbd5e1', borderRadius: '6px', fontSize: '14px', minWidth: '220px' }}
                  />
                  <select
                    value={invoiceStatusFilter}
                    onChange={e => { setInvoiceStatusFilter(e.target.value); setInvoicePage(1); }}
                    style={{ padding: '8px 12px', border: '1px solid #cbd5e1', borderRadius: '6px', fontSize: '14px' }}
                  >
                    <option value="">Tất cả trạng thái</option>
                    <option value="paid">Đã thanh toán</option>
                    <option value="unpaid">Chưa thanh toán</option>
                    <option value="partial">Thanh toán một phần</option>
                    <option value="cancelled">Đã hủy</option>
                  </select>
                  <select
                    value={invoicePageSize}
                    onChange={e => { setInvoicePageSize(Number(e.target.value)); setInvoicePage(1); }}
                    style={{ padding: '8px 12px', border: '1px solid #cbd5e1', borderRadius: '6px', fontSize: '14px' }}
                  >
                    <option value={10}>10 dòng</option>
                    <option value={20}>20 dòng</option>
                    <option value={50}>50 dòng</option>
                  </select>
                </div>
              </div>
              <DataTable rows={paginatedInvoices as unknown as Record<string, unknown>[]} columns={[
                { key: "invoiceCode", label: "Mã hóa đơn" },
                { key: "grandTotalAmount", label: "Tổng tiền", render: (row) => money(Number(row.grandTotalAmount)) },
                { key: "paidAmount", label: "Đã trả", render: (row) => money(Number(row.paidAmount)) },
                { key: "issuedAtUtc", label: "Ngày xuất", render: (row) => dateTime(String(row.issuedAtUtc ?? "")) }
              ]} />
              <Pagination pageNumber={invoicePage} totalPages={invoiceTotalPages} onChange={setInvoicePage} />
            </section>
          )}

          {activeTab === "vouchers" && (
            <section className="card">
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px', flexWrap: 'wrap', gap: '12px' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '16px', flexWrap: 'wrap' }}>
                  <h3 style={{ margin: 0 }}>🎟️ Danh sách Voucher đã đổi ({filteredVouchers.length})</h3>
                  <button type="button" className="ghost-btn compact" onClick={() => setVoucherModalOpen(true)} style={{ color: '#059669', fontWeight: 600 }}>
                    🎁 Đổi Voucher mới
                  </button>
                </div>
                <div style={{ display: 'flex', gap: '10px', alignItems: 'center', flexWrap: 'wrap' }}>
                  <input
                    type="text"
                    placeholder="Tìm mã hoặc tên voucher..."
                    value={voucherSearch}
                    onChange={e => { setVoucherSearch(e.target.value); setVoucherPage(1); }}
                    style={{ padding: '8px 12px', border: '1px solid #cbd5e1', borderRadius: '6px', fontSize: '14px', minWidth: '220px' }}
                  />
                  <select
                    value={voucherStatusFilter}
                    onChange={e => { setVoucherStatusFilter(e.target.value); setVoucherPage(1); }}
                    style={{ padding: '8px 12px', border: '1px solid #cbd5e1', borderRadius: '6px', fontSize: '14px' }}
                  >
                    <option value="">Tất cả trạng thái</option>
                    <option value="active">Đang có hiệu lực</option>
                    <option value="used">Đã sử dụng</option>
                    <option value="expired">Hết hạn</option>
                  </select>
                  <select
                    value={voucherPageSize}
                    onChange={e => { setVoucherPageSize(Number(e.target.value)); setVoucherPage(1); }}
                    style={{ padding: '8px 12px', border: '1px solid #cbd5e1', borderRadius: '6px', fontSize: '14px' }}
                  >
                    <option value={10}>10 dòng</option>
                    <option value={20}>20 dòng</option>
                    <option value={50}>50 dòng</option>
                  </select>
                </div>
              </div>
              <DataTable rows={paginatedVouchers as unknown as Record<string, unknown>[]} columns={[
                { key: "discountCode", label: "Mã Voucher", render: (row) => <strong style={{ color: '#059669', fontSize: '15px' }}>{String(row.discountCode || "-")}</strong> },
                { key: "name", label: "Tên Voucher", render: (row) => <span>{String(row.name || "-")}</span> },
                { key: "value", label: "Giá trị", render: (row) => row.discountType === "PERCENTAGE" ? `${row.value}%` : `${Number(row.value || 0).toLocaleString()} đ` },
                { key: "startsAtUtc", label: "Ngày đổi", render: (row) => dateTime(String(row.startsAtUtc)) },
                { key: "endsAtUtc", label: "Hạn sử dụng", render: (row) => row.endsAtUtc ? dateTime(String(row.endsAtUtc)) : "Vô thời hạn" },
                { key: "isActive", label: "Trạng thái", render: (row) => {
                  const isUsed = Number(row.usageCount || 0) >= Number(row.maxUsage || 1);
                  const isExpired = row.endsAtUtc && new Date(String(row.endsAtUtc)) < new Date();
                  if (isUsed || !row.isActive) return <Badge tone="red">Đã sử dụng</Badge>;
                  if (isExpired) return <Badge tone="red">Hết hạn</Badge>;
                  return <Badge tone="green">Đang có hiệu lực</Badge>;
                }}
              ]} />
              <Pagination pageNumber={voucherPage} totalPages={voucherTotalPages} onChange={setVoucherPage} />
            </section>
          )}

          {activeTab === "points" && (
            <section className="card">
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px', flexWrap: 'wrap', gap: '12px' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '16px', flexWrap: 'wrap' }}>
                  <h3 style={{ margin: 0 }}>🪙 Lịch sử tích / đổi điểm ({filteredPointHistories.length})</h3>
                  <button type="button" className="ghost-btn compact" onClick={() => setVoucherModalOpen(true)} style={{ color: '#059669', fontWeight: 600 }}>
                    🎁 Đổi Voucher ngay
                  </button>
                </div>
                <div style={{ display: 'flex', gap: '10px', alignItems: 'center', flexWrap: 'wrap' }}>
                  <input
                    type="text"
                    placeholder="Tìm theo diễn giải..."
                    value={pointSearch}
                    onChange={e => { setPointSearch(e.target.value); setPointPage(1); }}
                    style={{ padding: '8px 12px', border: '1px solid #cbd5e1', borderRadius: '6px', fontSize: '14px', minWidth: '220px' }}
                  />
                  <select
                    value={pointTypeFilter}
                    onChange={e => { setPointTypeFilter(e.target.value); setPointPage(1); }}
                    style={{ padding: '8px 12px', border: '1px solid #cbd5e1', borderRadius: '6px', fontSize: '14px' }}
                  >
                    <option value="">Tất cả loại</option>
                    <option value="EARN">Tích điểm (+)</option>
                    <option value="REDEEM">Đổi Voucher (-)</option>
                  </select>
                  <select
                    value={pointPageSize}
                    onChange={e => { setPointPageSize(Number(e.target.value)); setPointPage(1); }}
                    style={{ padding: '8px 12px', border: '1px solid #cbd5e1', borderRadius: '6px', fontSize: '14px' }}
                  >
                    <option value={10}>10 dòng</option>
                    <option value={20}>20 dòng</option>
                    <option value={50}>50 dòng</option>
                  </select>
                </div>
              </div>
              <DataTable rows={paginatedPointHistories as unknown as Record<string, unknown>[]} columns={[
                { key: "createdAtUtc", label: "Thời gian", render: (row) => dateTime(String(row.createdAtUtc)) },
                { key: "transactionType", label: "Loại GD", render: (row) => row.transactionType === 'REDEEM' ? <Badge tone="purple">Đổi Voucher</Badge> : <Badge tone="green">Tích điểm</Badge> },
                { key: "points", label: "Biến động", render: (row) => <strong style={{ color: Number(row.points) > 0 ? '#059669' : '#dc2626', fontSize: '15px' }}>{Number(row.points) > 0 ? `+${Number(row.points).toLocaleString()}` : Number(row.points).toLocaleString()}</strong> },
                { key: "description", label: "Diễn giải", render: (row) => <span>{String(row.description || "-")}</span> }
              ]} />
              <Pagination pageNumber={pointPage} totalPages={pointTotalPages} onChange={setPointPage} />
            </section>
          )}
        </div>

        {voucherModalOpen && (
          <Modal title={`🎁 Đổi điểm lấy Voucher - Khách hàng ${customer.fullName}`} onClose={() => setVoucherModalOpen(false)} size="large">
            <div style={{ marginBottom: '16px', padding: '14px 18px', background: '#f0fdf4', border: '1px solid #bbf7d0', borderRadius: '10px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '10px' }}>
              <div>
                <span style={{ fontSize: '14px', color: '#166534' }}>Điểm tích lũy hiện có của khách:</span>
                <strong style={{ fontSize: '22px', color: '#15803d', marginLeft: '8px' }}>{customer.loyaltyPoints?.toLocaleString() ?? 0} điểm</strong>
              </div>
              <span style={{ fontSize: '13px', color: '#15803d', background: '#dcfce7', padding: '4px 10px', borderRadius: '20px', fontWeight: 500 }}>💡 Chọn gói bên dưới để đổi bằng điểm (Hạn dùng theo gói Voucher mẫu)</span>
            </div>
            
            {voucherTemplates.length === 0 ? (
              <div style={{ padding: '40px', textAlign: 'center', color: 'var(--muted)', background: '#f8faf9', borderRadius: '12px', border: '1px dashed var(--line)' }}>
                <p style={{ fontSize: '16px', margin: '0 0 8px', fontWeight: 500, color: '#444' }}>Hiện chưa có gói Voucher đổi bằng điểm nào đang hoạt động.</p>
                <p style={{ fontSize: '14px', margin: 0 }}>Vui lòng vào menu <strong>Quản lý Mã giảm giá</strong> để tạo gói Voucher (chọn loại khuyến mãi là Gói Voucher)!</p>
              </div>
            ) : (
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: '16px', maxHeight: '450px', overflowY: 'auto', padding: '4px' }}>
                {voucherTemplates.map((template) => {
                  const required = template.pointsRequired ?? 0;
                  const enough = (customer.loyaltyPoints ?? 0) >= required;
                  const isBusy = exchangingId === template.discountId;
                  return (
                    <div key={template.discountId} style={{ border: enough ? '1.5px solid #a7f3d0' : '1px solid var(--line)', borderRadius: '12px', padding: '16px', background: enough ? '#ffffff' : '#f9fafb', display: 'flex', flexDirection: 'column', justifyContent: 'space-between', boxShadow: enough ? '0 4px 12px rgba(5, 150, 105, 0.08)' : 'none' }}>
                      <div>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '8px' }}>
                          <span className="badge purple" style={{ fontSize: '12px', padding: '4px 10px' }}>🎁 Voucher đổi điểm</span>
                          <span style={{ fontSize: '14px', fontWeight: 'bold', color: '#b45309', background: '#fef3c7', padding: '4px 10px', borderRadius: '20px' }}>
                            {required.toLocaleString()} điểm
                          </span>
                        </div>
                        <h4 style={{ margin: '8px 0 6px', fontSize: '17px', color: '#111' }}>{template.name}</h4>
                        <p style={{ margin: '0 0 14px', fontSize: '14px', color: 'var(--muted)' }}>
                          Giảm ngay: <strong style={{ color: '#059669', fontSize: '15px' }}>{template.discountType === 'PERCENTAGE' ? `${template.value}%` : `${template.value.toLocaleString()} đ`}</strong>
                          {template.maxAmount ? ` (Tối đa ${template.maxAmount.toLocaleString()} đ)` : ''}
                        </p>
                      </div>
                      <div style={{ borderTop: '1px dashed var(--line)', paddingTop: '14px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ fontSize: '13px', fontWeight: 500, color: enough ? '#059669' : '#dc2626' }}>
                          {enough ? '✅ Đủ điều kiện đổi' : `❌ Thiếu ${(required - (customer.loyaltyPoints ?? 0)).toLocaleString()} điểm`}
                        </span>
                        <button
                          type="button"
                          className={enough ? "primary-btn" : "ghost-btn"}
                          style={enough ? { background: '#7c3aed', color: '#fff', border: 'none', padding: '8px 16px', borderRadius: '8px', cursor: 'pointer', fontWeight: 600, boxShadow: '0 2px 6px rgba(124, 58, 237, 0.25)' } : { padding: '8px 16px', borderRadius: '8px', cursor: 'not-allowed', color: '#999', background: '#eee', border: 'none' }}
                          disabled={!enough || isBusy || exchangingId !== null}
                          onClick={() => handleExchangeVoucher(template)}
                        >
                          {isBusy ? "Đang đổi..." : "Đổi ngay"}
                        </button>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
            <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '20px', paddingTop: '14px', borderTop: '1px solid var(--line)' }}>
              <button type="button" className="ghost-btn" onClick={() => setVoucherModalOpen(false)}>Đóng</button>
            </div>
          </Modal>
        )}
        </>
      )}
    </>
  );
}
