"use client";

import { useEffect, useMemo, useState } from "react";
import type { ReactNode } from "react";
import { bookingApi, customerApi, pricingApi, sessionApi, venueApi } from "@/lib/api/endpoints";
import { getCurrentVietnamHourOfDay, getVietnamDateInputValue, vietnamDateRangeToUtcIso } from "@/lib/dateTime";
import { calculateDurationMinutes, formatSlotDateTime, generateBookingSlots, slotToUtcIso } from "@/lib/timeSlots";
import { dateTime, label, bookingStatus, sessionStatus, money } from "@/lib/status";
import { Badge, ConfirmDialog, DataTable, Modal, PageHeader, StateBlock, useList, useLoad } from "@/components/ui";
import { useToast } from "@/components/toast";
import { OvernightToggle } from "@/components/OvernightToggle";
import type { Booking, CustomerDto, PricingPlan, PricingPlanRule, Session, VenueTable } from "@/types";

const BOOKING_CONFIRMED = 2;
const SESSION_OPEN = 1;
function unwrap<T>(value: T[] | { items?: T[] } | undefined): T[] {
  if (!value) return [];
  return Array.isArray(value) ? value : value.items || [];
}

function durationMinutes(startedAtUtc?: string) {
  if (!startedAtUtc) return 0;
  return Math.max(0, Math.floor((Date.now() - new Date(startedAtUtc).getTime()) / 60000));
}

export default function SessionsPage() {
  const toast = useToast();
  const [starting, setStarting] = useState<Booking | null>(null);
  const [noShowing, setNoShowing] = useState<Booking | null>(null);
  const [ending, setEnding] = useState<Session | null>(null);
  const [summary, setSummary] = useState<any | null>(null);
  const [walkInOpen, setWalkInOpen] = useState(false);

  const { data, loading, error, reload } = useLoad(async () => {
    const { startUtc, endUtc } = vietnamDateRangeToUtcIso(getVietnamDateInputValue());

    const [bookingsRes, activeSessionsRes, tablesRes, customersRes] = await Promise.all([
      bookingApi.calendar(startUtc, endUtc, { status: BOOKING_CONFIRMED, pageNumber: 1, pageSize: 100 }),
      sessionApi.active(),
      venueApi.tables({ pageSize: 500 }),
      customerApi.list({ pageNumber: 1, pageSize: 500, status: true })
    ]);

    return {
      bookings: bookingsRes,
      sessions: activeSessionsRes,
      tables: tablesRes,
      customers: customersRes
    };
  }, []);

  const dueBookings = useMemo(() => {
    const now = Date.now();
    return unwrap<Booking>(data?.bookings as any).filter((booking) =>
      Number(booking.status) === BOOKING_CONFIRMED &&
      Boolean(booking.tableId) &&
      !booking.hasSession &&
      new Date(booking.startTimeUtc).getTime() <= now &&
      new Date(booking.endTimeUtc).getTime() > now
    );
  }, [data]);

  const activeSessions = useList<Session>(data?.sessions).filter((session) => Number(session.status) === SESSION_OPEN);
  const tables = useList<VenueTable>(data?.tables);
  const customers = useList<CustomerDto>(data?.customers).filter((customer) => customer.status);
  const activeTableIds = new Set(activeSessions.map((session) => Number((session as any).currentTable?.tableId || session.tableId)));
  const availableTables = tables.filter((table) =>
    table.isActive !== false &&
    Number(table.operationalStatus) === 1 &&
    !activeTableIds.has(Number(table.tableId))
  );

  function customerName(customerId: unknown) {
    if (!customerId) return "Khách vãng lai";
    return customers.find((customer) => customer.customerId === Number(customerId))?.fullName || "Khách hàng";
  }

  async function startSelected() {
    if (!starting) return;
    try {
      await bookingApi.startSession(starting.bookingId, starting.tableId);
      toast("Bat dau phien thanh cong.", "success");
      setStarting(null);
      reload();
    } catch (err: any) {
      const message = err?.status === 409
        ? "Booking da co phien hoac ban dang duoc su dung."
        : err?.message || "Khong the bat dau phien.";
      toast(message, "error");
    }
  }

  async function markNoShow() {
    if (!noShowing) return;
    try {
      await bookingApi.noShow(noShowing.bookingId, "Khach khong den vao gio da dat");
      toast("Da danh dau khach khong den.", "success");
      setNoShowing(null);
      reload();
    } catch (err: any) {
      toast(err?.message || "Khong the danh dau khach khong den.", "error");
    }
  }

  async function endSelected() {
    if (!ending) return;
    try {
      const result = await sessionApi.end(ending.sessionId);
      setSummary(result);
      toast("Ket thuc phien thanh cong.", "success");
      setEnding(null);
      reload();
    } catch (err: any) {
      toast(err?.message || "Khong the ket thuc phien.", "error");
    }
  }

  return (
    <>
      <PageHeader title="Quan ly phien choi" description="Bat dau phien tu booking da xac nhan va theo doi cac phien dang hoat dong." />

      <section style={{ padding: "0 24px 24px" }}>
        <div className="panel">
          <div className="panel-head">
            <div>
              <h3>Booking den gio</h3>
              <p>Cac booking da xac nhan va dang trong khung gio choi.</p>
            </div>
          </div>
          <StateBlock loading={loading} error={error} empty={!loading && dueBookings.length === 0} />
          <DataTable
            rows={dueBookings as unknown as Record<string, unknown>[]}
            columns={[
              { key: "bookingCode", label: "Ma booking" },
              { key: "customerName", label: "Khach hang", render: (row) => String(row.customerName || row.phoneNumber || row.customerId || "-") },
              { key: "tableId", label: "Ban / loai ban", render: (row) => row.tableId ? `Ban #${row.tableId}` : `Loai #${row.tableTypeId || "-"}` },
              { key: "startTimeUtc", label: "Bat dau", render: (row) => dateTime(String(row.startTimeUtc)) },
              { key: "endTimeUtc", label: "Ket thuc", render: (row) => dateTime(String(row.endTimeUtc)) },
              { key: "status", label: "Trang thai", render: (row) => <Badge tone="green">{label(bookingStatus, Number(row.status))}</Badge> }
            ]}
            actions={(row) => (
              <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
                <button className="primary-btn" onClick={() => setStarting(row as unknown as Booking)}>Bat dau phien</button>
                <button className="danger-btn" onClick={() => setNoShowing(row as unknown as Booking)}>Khach khong den</button>
              </div>
            )}
          />
        </div>
      </section>

      <section style={{ padding: "0 24px 24px" }}>
        <div className="panel">
          <div className="panel-head">
            <div>
              <h3>Phien dang hoat dong</h3>
              <p>Cac phien dang mo, lay tu route chuan /api/sessions/active.</p>
            </div>
            <button className="primary-btn" type="button" onClick={() => setWalkInOpen(true)}>Mở phiên khách vãng lai</button>
          </div>
          <StateBlock loading={loading} error={error} empty={!loading && activeSessions.length === 0} />
          <DataTable
            rows={activeSessions as unknown as Record<string, unknown>[]}
            columns={[
              { key: "sessionCode", label: "Ma session" },
              { key: "customerId", label: "Khach hang", render: (row) => customerName(row.customerId) },
              { key: "currentTable", label: "Ban hien tai", render: (row) => {
                const table = row.currentTable as any;
                return table ? `${table.tableName || table.tableCode || table.tableId}` : String(row.tableName || row.tableId || "-");
              } },
              { key: "startedAtUtc", label: "Bat dau", render: (row) => dateTime(String(row.startedAtUtc)) },
              { key: "duration", label: "Thoi luong", render: (row) => `${durationMinutes(String(row.startedAtUtc))} phut` },
              { key: "status", label: "Trang thai", render: (row) => <Badge tone="green">{label(sessionStatus, Number(row.status))}</Badge> }
            ]}
            actions={(row) => (
              <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
                <button className="ghost-btn" onClick={async () => {
                  try {
                    setSummary(await sessionApi.summary(Number(row.sessionId)));
                  } catch (err: any) {
                    toast(err?.message || "Khong tai duoc tam tinh.", "error");
                  }
                }}>Tam tinh</button>
                {Number(row.status) === SESSION_OPEN && (
                  <button className="danger-btn" onClick={() => setEnding(row as unknown as Session)}>Ket thuc</button>
                )}
              </div>
            )}
          />
        </div>
      </section>

      {starting ? <ConfirmDialog title="Bat dau phien" message={`Bat dau phien cho booking ${starting.bookingCode || starting.bookingId}?`} confirmLabel="Bat dau" onCancel={() => setStarting(null)} onConfirm={startSelected} /> : null}
      {noShowing ? <ConfirmDialog title="Khach khong den" message={`Danh dau booking ${noShowing.bookingCode || noShowing.bookingId} la khach khong den?`} confirmLabel="Khach khong den" danger onCancel={() => setNoShowing(null)} onConfirm={markNoShow} /> : null}
      {ending ? <ConfirmDialog title="Ket thuc phien choi" message={`Ban co chac muon ket thuc phien ${ending.sessionCode || ending.sessionId}?`} confirmLabel="Ket thuc" danger onCancel={() => setEnding(null)} onConfirm={endSelected} /> : null}
      {walkInOpen ? <WalkInSessionModal tables={availableTables} customers={customers} onClose={() => setWalkInOpen(false)} onStarted={async () => { setWalkInOpen(false); await reload(); }} /> : null}
      {summary ? <SummaryModal summary={summary} onClose={() => setSummary(null)} /> : null}
    </>
  );
}

function WalkInSessionModal({ tables, customers, onClose, onStarted }: {
  tables: VenueTable[];
  customers: CustomerDto[];
  onClose: () => void;
  onStarted: () => Promise<void>;
}) {
  const toast = useToast();
  const [tableId, setTableId] = useState("");
  const [customerId, setCustomerId] = useState("");
  const [playDate, setPlayDate] = useState(() => getVietnamDateInputValue());
  const [overnightEnabled, setOvernightEnabled] = useState(false);
  const timeSlots = useMemo(() => generateBookingSlots({ startDate: playDate, overnightEnabled }), [overnightEnabled, playDate]);
  const [selectedSlots, setSelectedSlots] = useState<number[]>(() => {
    const index = Math.floor(getCurrentVietnamHourOfDay() * 2);
    return index >= 0 && index < 48 ? [index] : [];
  });
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [checking, setChecking] = useState(false);
  const [conflict, setConflict] = useState(false);
  const [saving, setSaving] = useState(false);
  const [plans, setPlans] = useState<PricingPlan[]>([]);
  const [rules, setRules] = useState<PricingPlanRule[]>([]);

  useEffect(() => {
    Promise.all([pricingApi.plans(), pricingApi.rules({ pageSize: 500 })]).then(([planResult, ruleResult]) => {
      setPlans(unwrap<PricingPlan>(planResult));
      setRules(unwrap<PricingPlanRule>(ruleResult));
    }).catch(() => undefined);
  }, []);

  const selectedTable = tables.find((table) => table.tableId === Number(tableId));
  const nightRules = rules.filter((rule) => {
    if (selectedTable && rule.tableTypeId !== selectedTable.tableTypeId) return false;
    const planName = plans.find((plan) => plan.pricingPlanId === rule.pricingPlanId)?.name.toLowerCase() || "";
    const start = rule.startTime?.slice(0, 5) || "";
    const end = rule.endTime?.slice(0, 5) || "";
    return planName.includes("đêm") || planName.includes("night") || planName.includes("overnight") || Boolean(start && end && (start > end || start >= "22:00" || end <= "06:00"));
  });

  const selectedPeriod = useMemo(() => {
    if (selectedSlots.length !== 2 || selectedSlots[1] <= selectedSlots[0]) return null;
    return {
      startTimeUtc: slotToUtcIso(timeSlots[selectedSlots[0]]),
      endTimeUtc: slotToUtcIso(timeSlots[selectedSlots[1]])
    };
  }, [selectedSlots, timeSlots]);

  const isImmediatePeriod = useMemo(() => {
    if (!selectedPeriod || playDate !== getVietnamDateInputValue()) return false;
    const now = Date.now();
    return new Date(selectedPeriod.startTimeUtc).getTime() <= now && new Date(selectedPeriod.endTimeUtc).getTime() > now;
  }, [playDate, selectedPeriod]);

  useEffect(() => {
    if (!tableId || !playDate) {
      setBookings([]);
      return;
    }
    let cancelled = false;
    const { startUtc, endUtc: sameDayEndUtc } = vietnamDateRangeToUtcIso(playDate);
    const endUtc = overnightEnabled ? slotToUtcIso(timeSlots[timeSlots.length - 1]) : sameDayEndUtc;
    bookingApi.calendar(startUtc, endUtc, { tableId: Number(tableId), pageNumber: 1, pageSize: 100 })
      .then((result) => {
        if (!cancelled) setBookings(unwrap<Booking>(result as Booking[] | { items?: Booking[] }).filter((booking) => [1, 2].includes(Number(booking.status))));
      })
      .catch(() => { if (!cancelled) setBookings([]); });
    return () => { cancelled = true; };
  }, [overnightEnabled, playDate, tableId, timeSlots]);

  useEffect(() => {
    if (!tableId || !selectedPeriod || !isImmediatePeriod) {
      setConflict(false);
      setChecking(false);
      return;
    }
    let cancelled = false;
    setChecking(true);
    bookingApi.availability(Number(tableId), selectedPeriod.startTimeUtc, selectedPeriod.endTimeUtc)
      .then((available) => { if (!cancelled) setConflict(!available.some((table) => Number(table.tableId) === Number(tableId))); })
      .catch(() => { if (!cancelled) setConflict(true); })
      .finally(() => { if (!cancelled) setChecking(false); });
    return () => { cancelled = true; };
  }, [isImmediatePeriod, selectedPeriod, tableId]);

  function isPastPoint(index: number) {
    const slot = timeSlots[index];
    return !slot || new Date(slotToUtcIso(slot)).getTime() < Date.now() - 30 * 60 * 1000;
  }

  function isBookedPoint(index: number) {
    const slot = timeSlots[index];
    if (!slot) return false;
    const slotStart = new Date(slotToUtcIso(slot)).getTime();
    const slotEnd = slotStart + 30 * 60 * 1000;
    return bookings.some((booking) => {
      const start = new Date(booking.startTimeUtc).getTime();
      const end = new Date(booking.endTimeUtc).getTime();
      return slotStart < end && start < slotEnd;
    });
  }

  function selectSlot(index: number) {
    if (isPastPoint(index)) return;
    if (selectedSlots.length !== 1) {
      if (isBookedPoint(index) || index === timeSlots.length - 1) return;
      setSelectedSlots([index]);
      return;
    }
    const start = selectedSlots[0];
    if (index <= start) {
      toast("Giờ kết thúc phải sau giờ bắt đầu. Nếu muốn đặt qua đêm, hãy bật Đặt qua đêm.", "error");
      return;
    }
    setSelectedSlots([start, index]);
  }

  async function start() {
    if (!tableId || !selectedPeriod) {
      toast("Vui lòng chọn bàn và đầy đủ giờ bắt đầu, kết thúc.", "error");
      return;
    }
    if (!isImmediatePeriod) {
      toast("Khung giờ này là tương lai. Hãy dùng Tạo lịch đặt bàn.", "error");
      return;
    }

    setSaving(true);
    try {
      const available = await bookingApi.availability(Number(tableId), selectedPeriod.startTimeUtc, selectedPeriod.endTimeUtc);
      if (!available.some((table) => Number(table.tableId) === Number(tableId))) {
        setConflict(true);
        toast("Bàn này đã có booking hoặc phiên chơi trong khung giờ đã chọn.", "error");
        return;
      }
      await sessionApi.start({
        tableId: Number(tableId),
        ...(customerId ? { customerId: Number(customerId) } : {})
      });
      toast("Mở phiên thành công", "success");
      await onStarted();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể mở phiên.", "error");
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal title="Mở phiên khách vãng lai" onClose={onClose} size="large">
      <div style={{ display: "grid", gap: 16 }}>
        <label style={{ display: "grid", gap: 6 }}>
          <span style={{ fontWeight: 700 }}>Bàn *</span>
          <select value={tableId} onChange={(event) => setTableId(event.target.value)}>
            <option value="">Chọn bàn trống</option>
            {tables.map((table) => <option key={table.tableId} value={table.tableId}>{table.tableName || table.tableCode}</option>)}
          </select>
        </label>
        <label style={{ display: "grid", gap: 6 }}>
          <span style={{ fontWeight: 700 }}>Khách hàng có sẵn <small style={{ color: "var(--muted)", fontWeight: 500 }}>(không bắt buộc)</small></span>
          <select value={customerId} onChange={(event) => setCustomerId(event.target.value)}>
            <option value="">Không chọn - Khách vãng lai</option>
            {customers.map((customer) => <option key={customer.customerId} value={customer.customerId}>{customer.fullName}{customer.phoneNumber ? ` - ${customer.phoneNumber}` : ""}</option>)}
          </select>
        </label>
        <label style={{ display: "grid", gap: 6, maxWidth: 240 }}>
          <span style={{ fontWeight: 700 }}>Ngày chơi *</span>
          <input type="date" min={getVietnamDateInputValue()} value={playDate} onChange={(event) => { setPlayDate(event.target.value); setSelectedSlots([]); }} />
        </label>
        <OvernightToggle checked={overnightEnabled} onChange={(checked) => { setOvernightEnabled(checked); setSelectedSlots((current) => current.length ? [current[0]] : []); }} />
        {playDate > getVietnamDateInputValue() ? <div className="inline-alert error">Khung giờ này là tương lai. Hãy dùng Tạo lịch đặt bàn.</div> : null}
        <div>
          <strong>Khung giờ dự kiến *</strong>
          <p style={{ margin: "5px 0 10px", color: "var(--muted)", fontSize: 13 }}>Mốc thứ hai là giờ kết thúc dự kiến; thời gian tính tiền vẫn theo phiên thực tế.</p>
          <div style={{ display: "flex", gap: 12, flexWrap: "wrap", marginBottom: 10, fontSize: 13 }}>
            <span>□ Trống</span><span style={{ color: "#1d4ed8" }}>■ Đang chọn</span><span style={{ color: "#dc2626" }}>■ Đã đặt / Đang bận</span><span style={{ color: "#64748b" }}>■ Đã qua</span>
          </div>
          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(72px, 1fr))", gap: 6 }}>
            {timeSlots.map((slot, index) => {
              const selected = selectedSlots.includes(index) || (selectedSlots.length === 2 && index > selectedSlots[0] && index < selectedSlots[1]);
              const past = isPastPoint(index);
              const booked = isBookedPoint(index);
              return <button key={slot.localDateTime} type="button" onClick={() => selectSlot(index)} disabled={past}
                style={{ minHeight: 38, border: `1px solid ${selected ? "#2563eb" : booked ? "#fecaca" : "#cbd5e1"}`, borderRadius: 6, background: selected ? "#dbeafe" : booked ? "#fee2e2" : past ? "#f1f5f9" : "#fff", color: past ? "#94a3b8" : booked ? "#b91c1c" : "#0f172a", fontWeight: selected ? 700 : 500, cursor: past ? "not-allowed" : "pointer" }}>{slot.displayLabel}</button>;
            })}
          </div>
          {overnightEnabled ? <p style={{ margin: "8px 0 0", color: "var(--muted)", fontSize: 13 }}>(+1) nghĩa là ngày hôm sau.</p> : null}
          {timeSlots[selectedSlots[1]]?.dayOffset === 1 ? <div className="inline-note">Bạn đang chọn ca qua đêm.<br />Thời gian dự kiến: {formatSlotDateTime(timeSlots[selectedSlots[0]])} → {formatSlotDateTime(timeSlots[selectedSlots[1]])}.<br />Thời lượng: {(calculateDurationMinutes(timeSlots[selectedSlots[0]], timeSlots[selectedSlots[1]]) / 60).toLocaleString("vi-VN")} giờ.</div> : null}
          {overnightEnabled ? <div className="inline-note"><strong>Gói đêm áp dụng</strong><br />{nightRules.length ? nightRules.map((rule) => `${plans.find((plan) => plan.pricingPlanId === rule.pricingPlanId)?.name || "Bảng giá"}: ${rule.startTime?.slice(0, 5)}–${rule.endTime?.slice(0, 5)} (${rule.hourlyRate.toLocaleString("vi-VN")} đ/giờ)`).join("; ") : "Chưa có gói đêm được cấu hình. Giá tạm tính theo bảng giá hiện tại."}</div> : null}
        </div>
        {selectedPeriod && !isImmediatePeriod ? <div className="inline-alert error">Khung giờ phải chứa thời điểm hiện tại. Nếu đặt cho tương lai, hãy dùng Tạo lịch đặt bàn.</div> : null}
        {checking ? <div className="inline-note">Đang kiểm tra lịch trống...</div> : null}
        {conflict ? <div className="inline-alert error">Bàn này đã có booking hoặc phiên chơi trong khung giờ đã chọn.</div> : null}
        <div className="modal-actions">
          <button className="ghost-btn" type="button" onClick={onClose} disabled={saving}>Hủy</button>
          <button className="primary-btn" type="button" onClick={start} disabled={saving || checking || conflict || !tableId || !selectedPeriod || !isImmediatePeriod}>{saving ? "Đang mở..." : "Mở phiên"}</button>
        </div>
      </div>
    </Modal>
  );
}

function SummaryModal({ summary, onClose }: { summary: any; onClose: () => void }) {
  const data = summary?.data || summary;
  const timeAmount = data?.timeSubtotalAmount ?? data?.TimeSubtotalAmount ?? 0;
  const orderAmount = data?.orderSubtotalAmount ?? data?.productSubtotalAmount ?? data?.ProductSubtotalAmount ?? 0;
  const total = data?.grandTotalAmount ?? data?.subtotalAmount ?? data?.SubtotalAmount ?? timeAmount + orderAmount;
  const duration = data?.totalDurationMinutes ?? data?.currentDurationMinutes ?? data?.TotalDurationMinutes;

  return (
    <div
      onMouseDown={onClose}
      style={{
        position: "fixed",
        inset: 0,
        zIndex: 1000,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        padding: 24,
        background: "rgba(15, 23, 42, 0.45)"
      }}
    >
      <div
        role="dialog"
        aria-modal="true"
        onMouseDown={(event) => event.stopPropagation()}
        style={{
          width: "min(520px, 100%)",
          borderRadius: 16,
          background: "#ffffff",
          color: "#0f172a",
          boxShadow: "0 24px 80px rgba(15, 23, 42, 0.28)",
          border: "1px solid #e2e8f0",
          padding: 24
        }}
      >
        <div style={{ display: "flex", justifyContent: "space-between", gap: 16, alignItems: "flex-start", marginBottom: 20 }}>
          <div>
            <h3 style={{ margin: 0, fontSize: 20, fontWeight: 700, color: "#0f172a" }}>Tam tinh / Ket qua dong phien</h3>
            <p style={{ margin: "6px 0 0", color: "#475569", fontSize: 14 }}>{data?.sessionCode || data?.SessionCode || `Session #${data?.sessionId || data?.SessionId || ""}`}</p>
          </div>
          <button
            onClick={onClose}
            style={{
              border: "1px solid #cbd5e1",
              borderRadius: 8,
              background: "#f8fafc",
              color: "#0f172a",
              fontWeight: 600,
              padding: "8px 14px",
              cursor: "pointer"
            }}
          >
            Dong
          </button>
        </div>
        <div style={{ display: "grid", gap: 12 }}>
          <SummaryRow label="Ma phien" value={data?.sessionCode || data?.SessionCode || data?.sessionId || data?.SessionId || "-"} />
          <SummaryRow label="Thoi luong" value={`${duration ?? "-"} phut`} />
          <SummaryRow label="Tien gio" value={money(Number(timeAmount))} />
          <SummaryRow label="Tien san pham/order" value={money(Number(orderAmount))} />
          <SummaryRow label="Hoa don" value={data?.invoiceCode || data?.InvoiceCode || (data?.invoiceGenerated ? "Da tao" : "Chua tao")} />
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 16, borderTop: "1px solid #e2e8f0", marginTop: 4, paddingTop: 16 }}>
            <span style={{ color: "#0f172a", fontWeight: 700 }}>Tong tien</span>
            <strong style={{ color: "#047857", fontSize: 22, fontWeight: 800 }}>{money(Number(total))}</strong>
          </div>
        </div>
      </div>
    </div>
  );
}

function SummaryRow({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div style={{ display: "flex", justifyContent: "space-between", gap: 16, alignItems: "center", border: "1px solid #e2e8f0", borderRadius: 10, padding: "12px 14px", background: "#f8fafc" }}>
      <span style={{ color: "#475569", fontSize: 14 }}>{label}</span>
      <strong style={{ color: "#0f172a", textAlign: "right", fontWeight: 700 }}>{value}</strong>
    </div>
  );
}
