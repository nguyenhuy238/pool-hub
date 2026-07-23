"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { customerPortalApi } from "@/lib/api/endpoints";
import { unwrapList } from "@/lib/api/client";
import { bookingStatus, dateTime, label, money, sessionStatus } from "@/lib/status";
import { Badge, Modal, Pagination, StateBlock } from "@/components/ui";
import { DepositRefundSummaryPanel } from "@/components/refunds/DepositRefundSummaryPanel";
import { useToast } from "@/components/toast";
import type {
  CustomerBookingHistory,
  CustomerInvoiceHistory,
  CustomerPointHistory,
  CustomerPortalProfile,
  CustomerSessionHistory,
  Discount,
  Invoice,
  PagedResult
} from "@/types";

export type CustomerPortalSection = "overview" | "bookings" | "sessions" | "invoices" | "vouchers" | "points";

const pageSize = 10;
const loadPageSize = 100;
const invoicePaymentStatus: Record<number, string> = {
  1: "Chưa thanh toán",
  2: "Đã thanh toán một phần",
  3: "Đã thanh toán"
};

async function loadAllPages<T>(
  fetchPage: (params: { pageNumber: number; pageSize: number }) => Promise<PagedResult<T>>
): Promise<T[]> {
  const first = await fetchPage({ pageNumber: 1, pageSize: loadPageSize });
  const firstRows = unwrapList(first);
  const total = Number(first.totalItems ?? first.totalCount ?? firstRows.length);
  const totalPages = Math.ceil(total / loadPageSize);
  if (totalPages <= 1) return firstRows;
  const remaining = await Promise.all(
    Array.from({ length: totalPages - 1 }, (_, index) =>
      fetchPage({ pageNumber: index + 2, pageSize: loadPageSize }))
  );
  return [first, ...remaining].flatMap((page) => unwrapList(page));
}

export function CustomerPortalSection({ section }: { section: CustomerPortalSection }) {
  const toast = useToast();
  const [profile, setProfile] = useState<CustomerPortalProfile | null>(null);
  const [data, setData] = useState<unknown>(null);
  const [voucherTemplates, setVoucherTemplates] = useState<Discount[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState("all");
  const [selectedInvoice, setSelectedInvoice] = useState<Invoice | null>(null);
  const [invoiceLoading, setInvoiceLoading] = useState(false);
  const [profileModalOpen, setProfileModalOpen] = useState(false);
  const [voucherModalOpen, setVoucherModalOpen] = useState(false);
  const [exchangingId, setExchangingId] = useState<number | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const currentProfile = await customerPortalApi.profile();
      setProfile(currentProfile);
      if (section === "overview") {
        setData(null);
      } else if (section === "bookings") {
        setData(await loadAllPages(customerPortalApi.bookings));
      } else if (section === "sessions") {
        setData(await loadAllPages(customerPortalApi.sessions));
      } else if (section === "invoices") {
        setData(await loadAllPages(customerPortalApi.invoices));
      } else if (section === "vouchers") {
        const [vouchers, templates] = await Promise.all([
          loadAllPages(customerPortalApi.vouchers),
          customerPortalApi.voucherTemplates()
        ]);
        setData(vouchers);
        setVoucherTemplates(templates);
      } else {
        setData(await loadAllPages(customerPortalApi.points));
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được dữ liệu tài khoản.");
    } finally {
      setLoading(false);
    }
  }, [section]);

  useEffect(() => {
    setPageNumber(1);
    setSearch("");
    setFilter("all");
    void load();
  }, [load]);

  const allRows = unwrapList(data as never);
  const filteredRows = useMemo(
    () => filterRows(section, allRows, search, filter),
    [allRows, filter, search, section]
  );
  const totalPages = Math.max(1, Math.ceil(filteredRows.length / pageSize));
  const rows = filteredRows.slice((pageNumber - 1) * pageSize, pageNumber * pageSize);

  useEffect(() => {
    setPageNumber(1);
  }, [search, filter]);

  const title = section === "overview" ? "Tổng quan tài khoản"
    : section === "bookings" ? "Lịch sử booking"
      : section === "sessions" ? "Lịch sử phiên chơi"
        : section === "invoices" ? "Lịch sử hóa đơn"
          : section === "vouchers" ? "Voucher của tôi"
            : "Lịch sử điểm";

  if (section === "overview") {
    return (
      <section>
        <div className="customer-page-heading">
          <div><span className="customer-kicker">Không gian cá nhân</span><h2>{title}</h2><p>Quản lý thông tin và theo dõi hành trình chơi tại PoolHub.</p></div>
          <button className="primary-btn" type="button" onClick={() => setProfileModalOpen(true)} disabled={!profile}>Chỉnh sửa hồ sơ</button>
        </div>
        <StateBlock loading={loading} error={error} />
        {!loading && !error && profile ? <Overview profile={profile} /> : null}
        {profileModalOpen && profile ? (
          <ProfileModal
            profile={profile}
            onClose={() => setProfileModalOpen(false)}
            onSaved={(updated) => {
              setProfile(updated);
              setProfileModalOpen(false);
              toast("Cập nhật hồ sơ thành công.", "success");
            }}
          />
        ) : null}
      </section>
    );
  }

  return (
    <section>
      <div className="customer-page-heading">
        <div><span className="customer-kicker">Tài khoản cá nhân</span><h2>{title}</h2><p>Dữ liệu chỉ hiển thị cho tài khoản khách hàng đang đăng nhập.</p></div>
        <div className="customer-heading-actions">
          <div className="customer-points-pill">Điểm hiện có <strong>{profile?.loyaltyPoints?.toLocaleString("vi-VN") ?? "—"}</strong></div>
          {section === "vouchers" ? <button className="primary-btn" type="button" onClick={() => setVoucherModalOpen(true)}>Đổi voucher</button> : null}
        </div>
      </div>
      <CustomerTableFilters section={section} search={search} filter={filter} onSearch={setSearch} onFilter={setFilter} />
      <StateBlock loading={loading} error={error} empty={!loading && !error && rows.length === 0} />
      {!loading && !error && rows.length > 0 ? (
        <div className="customer-table-card">
          {section === "bookings" ? <BookingsTable rows={rows as CustomerBookingHistory[]} /> : null}
          {section === "sessions" ? <SessionsTable rows={rows as CustomerSessionHistory[]} /> : null}
          {section === "invoices" ? (
            <InvoicesTable rows={rows as CustomerInvoiceHistory[]} onOpen={async (id) => {
              setInvoiceLoading(true);
              try { setSelectedInvoice(await customerPortalApi.invoiceDetail(id)); }
              catch (err) { setError(err instanceof Error ? err.message : "Không tải được hóa đơn."); }
              finally { setInvoiceLoading(false); }
            }} />
          ) : null}
          {section === "vouchers" ? <VouchersTable rows={rows as Discount[]} /> : null}
          {section === "points" ? <PointsTable rows={rows as CustomerPointHistory[]} /> : null}
        </div>
      ) : null}
      <Pagination pageNumber={pageNumber} totalPages={totalPages} onChange={setPageNumber} />
      {invoiceLoading ? <div className="customer-loading-note">Đang tải chi tiết hóa đơn...</div> : null}
      {selectedInvoice ? <InvoiceModal invoice={selectedInvoice} onClose={() => setSelectedInvoice(null)} /> : null}
      {voucherModalOpen ? (
        <VoucherExchangeModal
          points={profile?.loyaltyPoints ?? 0}
          templates={voucherTemplates}
          exchangingId={exchangingId}
          onClose={() => setVoucherModalOpen(false)}
          onExchange={async (template) => {
            setExchangingId(template.discountId);
            try {
              const voucher = await customerPortalApi.exchangeVoucher(template.discountId);
              toast(`Đổi voucher thành công. Mã của bạn: ${voucher.discountCode}`, "success");
              setVoucherModalOpen(false);
              await load();
            } catch (err) {
              toast(err instanceof Error ? err.message : "Đổi voucher thất bại.", "error");
            } finally {
              setExchangingId(null);
            }
          }}
        />
      ) : null}
    </section>
  );
}

function filterRows(section: CustomerPortalSection, rows: unknown[], search: string, filter: string): unknown[] {
  const keyword = search.trim().toLocaleLowerCase("vi");
  return rows.filter((item) => {
    const row = item as Record<string, unknown>;
    const haystack = Object.values(row).filter((value) => typeof value === "string" || typeof value === "number").join(" ").toLocaleLowerCase("vi");
    if (keyword && !haystack.includes(keyword)) return false;
    if (filter === "all") return true;
    if (section === "bookings" || section === "sessions") return String(row.status) === filter;
    if (section === "invoices") return String(row.paymentStatus) === filter;
    if (section === "vouchers") return filter === "active" ? row.isActive === true : row.isActive === false;
    if (section === "points") return String(row.transactionType).toUpperCase() === filter;
    return true;
  });
}

function CustomerTableFilters({ section, search, filter, onSearch, onFilter }: {
  section: CustomerPortalSection;
  search: string;
  filter: string;
  onSearch: (value: string) => void;
  onFilter: (value: string) => void;
}) {
  const options = section === "bookings" ? Object.entries(bookingStatus)
    : section === "sessions" ? Object.entries(sessionStatus)
      : section === "invoices" ? Object.entries(invoicePaymentStatus)
        : section === "vouchers" ? [["active", "Có thể dùng"], ["inactive", "Đã dùng / hết hạn"]]
          : [["EARN", "Tích điểm"], ["REDEEM", "Đổi voucher"]];
  return (
    <div className="customer-table-filters">
      <label><span>Tìm kiếm</span><input value={search} onChange={(event) => onSearch(event.target.value)} placeholder="Nhập mã, tên hoặc nội dung..." /></label>
      <label><span>Lọc trạng thái</span><select value={filter} onChange={(event) => onFilter(event.target.value)}><option value="all">Tất cả</option>{options.map(([value, text]) => <option key={value} value={value}>{text}</option>)}</select></label>
    </div>
  );
}

function ProfileModal({ profile, onClose, onSaved }: {
  profile: CustomerPortalProfile;
  onClose: () => void;
  onSaved: (profile: CustomerPortalProfile) => void;
}) {
  const toast = useToast();
  const [fullName, setFullName] = useState(profile.fullName);
  const [phoneNumber, setPhoneNumber] = useState(profile.phoneNumber);
  const [saving, setSaving] = useState(false);
  return (
    <Modal title="Chỉnh sửa hồ sơ" onClose={onClose}>
      <form onSubmit={async (event) => {
        event.preventDefault();
        if (!fullName.trim() || !phoneNumber.trim()) {
          toast("Vui lòng nhập đầy đủ họ tên và số điện thoại.", "error");
          return;
        }
        setSaving(true);
        try { onSaved(await customerPortalApi.updateProfile({ fullName: fullName.trim(), phoneNumber: phoneNumber.trim() })); }
        catch (err) { toast(err instanceof Error ? err.message : "Không thể cập nhật hồ sơ.", "error"); }
        finally { setSaving(false); }
      }}>
        <div className="form-grid">
          <label><span>Họ và tên</span><input value={fullName} onChange={(event) => setFullName(event.target.value)} maxLength={200} required /></label>
          <label><span>Số điện thoại</span><input value={phoneNumber} onChange={(event) => setPhoneNumber(event.target.value)} maxLength={30} required /></label>
          <label className="full-field"><span>Email đăng nhập</span><input value={profile.email ?? ""} readOnly disabled /><small className="field-help">Email đăng nhập không thể thay đổi tại đây.</small></label>
        </div>
        <div className="modal-actions"><button className="ghost-btn" type="button" onClick={onClose} disabled={saving}>Hủy</button><button className="primary-btn" type="submit" disabled={saving}>{saving ? "Đang lưu..." : "Lưu thay đổi"}</button></div>
      </form>
    </Modal>
  );
}

function VoucherExchangeModal({ points, templates, exchangingId, onClose, onExchange }: {
  points: number;
  templates: Discount[];
  exchangingId: number | null;
  onClose: () => void;
  onExchange: (template: Discount) => void;
}) {
  return (
    <Modal title="Đổi điểm lấy voucher" onClose={onClose} size="large">
      <div className="customer-exchange-balance">Điểm hiện có <strong>{points.toLocaleString("vi-VN")} điểm</strong></div>
      {templates.length === 0 ? <StateBlock empty /> : (
        <div className="customer-voucher-grid">
          {templates.map((template) => {
            const required = template.pointsRequired ?? 0;
            const enough = points >= required;
            return (
              <article className={`customer-voucher-option${enough ? "" : " disabled"}`} key={template.discountId}>
                <div><Badge tone="purple">Voucher đổi điểm</Badge><strong className="customer-voucher-cost">{required.toLocaleString("vi-VN")} điểm</strong></div>
                <h3>{template.name}</h3>
                <p>Giảm {template.discountType === "PERCENTAGE" ? `${template.value}%` : money(template.value)}{template.maxAmount ? `, tối đa ${money(template.maxAmount)}` : ""}</p>
                <small>{template.endsAtUtc ? `Hạn dùng: ${dateTime(template.endsAtUtc)}` : "Không giới hạn thời gian"}</small>
                <button className={enough ? "primary-btn" : "ghost-btn"} type="button" disabled={!enough || exchangingId !== null} onClick={() => onExchange(template)}>
                  {exchangingId === template.discountId ? "Đang đổi..." : enough ? "Đổi ngay" : `Thiếu ${(required - points).toLocaleString("vi-VN")} điểm`}
                </button>
              </article>
            );
          })}
        </div>
      )}
    </Modal>
  );
}

function Overview({ profile }: { profile: CustomerPortalProfile }) {
  return (
    <>
      <div className="customer-welcome-card">
        <div><span className="customer-card-label">Xin chào,</span><h3>{profile.fullName}</h3><p>Hồ sơ của bạn đã sẵn sàng. Từ đây bạn có thể xem lại mọi booking, phiên chơi và hóa đơn.</p></div>
        <div className="customer-points-hero"><span>Điểm tích lũy</span><strong>{profile.loyaltyPoints.toLocaleString("vi-VN")}</strong><small>Tổng đã nhận: {profile.totalPointsEarned.toLocaleString("vi-VN")}</small></div>
      </div>
      <div className="customer-stat-grid">
        <Stat label="Họ tên" value={profile.fullName} /><Stat label="Email" value={profile.email || "Chưa cập nhật"} /><Stat label="Số điện thoại" value={profile.phoneNumber || "Chưa cập nhật"} /><Stat label="Ngày tham gia" value={dateTime(profile.createdAtUtc)} />
      </div>
      <div className="customer-quick-grid">
        <QuickLink href="/customer/bookings" title="Booking" text="Xem các lần đặt bàn" /><QuickLink href="/customer/sessions" title="Phiên chơi" text="Theo dõi thời gian chơi" /><QuickLink href="/customer/invoices" title="Hóa đơn" text="Xem chi tiết hóa đơn" /><QuickLink href="/customer/vouchers" title="Voucher" text="Đổi điểm và kiểm tra ưu đãi" />
      </div>
    </>
  );
}

function Stat({ label: name, value }: { label: string; value: string }) { return <div className="customer-stat"><span>{name}</span><strong>{value}</strong></div>; }
function QuickLink({ href, title, text }: { href: string; title: string; text: string }) { return <a className="customer-quick-link" href={href}><strong>{title}</strong><span>{text}</span><b>→</b></a>; }

function BookingsTable({ rows }: { rows: CustomerBookingHistory[] }) {
  return <TableShell headers={["Mã booking", "Bàn", "Bắt đầu", "Kết thúc", "Trạng thái"]}>{rows.map((row) => <tr key={row.bookingId}><td><strong>{row.bookingCode}</strong></td><td>{row.tableName || (row.tableId ? `Bàn #${row.tableId}` : "Chưa chọn")}</td><td>{dateTime(row.startTimeUtc)}</td><td>{dateTime(row.endTimeUtc)}</td><td><Badge tone="blue">{label(bookingStatus, row.status)}</Badge></td></tr>)}</TableShell>;
}
function SessionsTable({ rows }: { rows: CustomerSessionHistory[] }) {
  return <TableShell headers={["Mã phiên", "Bắt đầu", "Kết thúc", "Trạng thái"]}>{rows.map((row) => <tr key={row.sessionId}><td><strong>{row.sessionCode}</strong></td><td>{dateTime(row.startedAtUtc)}</td><td>{row.endedAtUtc ? dateTime(row.endedAtUtc) : "Đang diễn ra"}</td><td><Badge tone={row.status === 1 ? "green" : "blue"}>{label(sessionStatus, row.status)}</Badge></td></tr>)}</TableShell>;
}
function InvoicesTable({ rows, onOpen }: { rows: CustomerInvoiceHistory[]; onOpen: (id: number) => void }) {
  return <TableShell headers={["Mã hóa đơn", "Tổng tiền", "Đã trả", "Trạng thái", "Ngày lập", ""]}>{rows.map((row) => <tr key={row.invoiceId}><td><strong>{row.invoiceCode}</strong></td><td>{money(row.grandTotalAmount)}</td><td>{money(row.paidAmount)}</td><td><Badge tone={row.paymentStatus === 3 ? "green" : row.paymentStatus === 2 ? "blue" : "yellow"}>{invoicePaymentStatus[row.paymentStatus] || "Chờ xử lý"}</Badge></td><td>{row.issuedAtUtc ? dateTime(row.issuedAtUtc) : "—"}</td><td><button className="ghost-btn compact" onClick={() => onOpen(row.invoiceId)}>Xem chi tiết</button></td></tr>)}</TableShell>;
}
function VouchersTable({ rows }: { rows: Discount[] }) {
  return <TableShell headers={["Mã voucher", "Tên ưu đãi", "Giá trị", "Hiệu lực đến", "Trạng thái"]}>{rows.map((row) => <tr key={row.discountId}><td><strong>{row.discountCode}</strong></td><td>{row.name}</td><td>{row.discountType === "PERCENTAGE" ? `${row.value}%` : money(row.value)}</td><td>{row.endsAtUtc ? dateTime(row.endsAtUtc) : "Vô thời hạn"}</td><td><Badge tone={row.isActive ? "green" : "red"}>{row.isActive ? "Có thể dùng" : "Đã dùng / hết hạn"}</Badge></td></tr>)}</TableShell>;
}
function PointsTable({ rows }: { rows: CustomerPointHistory[] }) {
  return <TableShell headers={["Thời gian", "Loại giao dịch", "Mô tả", "Điểm"]}>{rows.map((row) => <tr key={row.customerPointHistoryId}><td>{dateTime(row.createdAtUtc)}</td><td><Badge tone={row.points >= 0 ? "green" : "purple"}>{row.transactionType === "REDEEM" ? "Đổi voucher" : "Tích điểm"}</Badge></td><td>{row.description}</td><td className={row.points >= 0 ? "point-positive" : "point-negative"}>{row.points > 0 ? "+" : ""}{row.points.toLocaleString("vi-VN")}</td></tr>)}</TableShell>;
}
function TableShell({ headers, children }: { headers: string[]; children: React.ReactNode }) { return <div className="customer-table-scroll"><table className="customer-table"><thead><tr>{headers.map((header) => <th key={header}>{header}</th>)}</tr></thead><tbody>{children}</tbody></table></div>; }

function InvoiceModal({ invoice, onClose }: { invoice: Invoice; onClose: () => void }) {
  return <Modal title={invoice.invoiceCode || `Hóa đơn #${invoice.invoiceId}`} onClose={onClose} size="large"><div className="customer-invoice-detail">
    <div className="customer-invoice-meta"><span>Trạng thái</span><Badge tone={invoice.paymentStatus === 3 ? "green" : invoice.paymentStatus === 2 ? "blue" : "yellow"}>{invoicePaymentStatus[invoice.paymentStatus || 1] || "Chờ thanh toán"}</Badge><span>Tổng thanh toán</span><strong>{money(invoice.grandTotalAmount)}</strong></div>
    <div className="customer-table-scroll"><table className="customer-table"><thead><tr><th>Diễn giải</th><th>Số lượng</th><th>Đơn giá</th><th>Thành tiền</th></tr></thead><tbody>{(invoice.lines || []).map((line) => { const isTime = String(line.lineType || "").toUpperCase() === "TIME"; return <tr key={line.invoiceLineId}><td>{line.description}</td><td>{isTime ? `${Math.round(line.quantity * 60).toLocaleString("vi-VN")} phút` : line.quantity.toLocaleString("vi-VN")}</td><td>{isTime ? `${money(line.unitPrice)} / giờ` : money(line.unitPrice)}</td><td>{money(line.lineTotalAmount)}</td></tr>; })}</tbody></table></div>
    <DepositRefundSummaryPanel summary={invoice.depositRefundSummary} variant="bill" />
    <div className="customer-invoice-total"><span>Còn phải trả</span><strong>{money(invoice.remainingAmount ?? 0)}</strong></div>
  </div></Modal>;
}
