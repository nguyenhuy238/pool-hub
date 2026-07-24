"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import type { ReactNode } from "react";
import { useRouter } from "next/navigation";
import { bookingApi, customerApi, pricingApi, sessionApi, venueApi } from "@/lib/api/endpoints";
import { ApiError } from "@/lib/api/client";
import { getCurrentVietnamHourOfDay, getVietnamDateInputValue, utcTimestampMs, vietnamDateRangeToUtcIso } from "@/lib/dateTime";
import { calculateDurationMinutes, formatSlotDateTime, generateBookingSlots, slotToUtcIso } from "@/lib/timeSlots";
import { dateTime, label, bookingStatus, sessionStatus, money } from "@/lib/status";
import { formatElapsedDuration } from "@/lib/sessionDuration";
import { getSessionActiveAssignments, getSessionReleasedAssignments, normalizeActiveSessions } from "@/lib/activeSessions";
import { connectOperationHub } from "@/lib/realtime/operationHub";
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

export default function SessionsPage() {
  const router = useRouter();
  const toast = useToast();
  const [starting, setStarting] = useState<Booking | null>(null);
  const [noShowing, setNoShowing] = useState<Booking | null>(null);
  const [ending, setEnding] = useState<Session | null>(null);
  const [endingSummary, setEndingSummary] = useState<any | null>(null);
  const [summary, setSummary] = useState<any | null>(null);
  const [walkInOpen, setWalkInOpen] = useState(false);
  const [previewingSessionId, setPreviewingSessionId] = useState<number | null>(null);
  const [closingSession, setClosingSession] = useState(false);
  const [, setDurationTick] = useState(0);

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
  const reloadRef = useRef(reload);
  const realtimeReloadTimerRef = useRef<number | null>(null);

  useEffect(() => {
    reloadRef.current = reload;
  }, [reload]);

  const scheduleRealtimeReload = useCallback(() => {
    if (realtimeReloadTimerRef.current !== null) return;
    realtimeReloadTimerRef.current = window.setTimeout(() => {
      realtimeReloadTimerRef.current = null;
      void reloadRef.current();
    }, 250);
  }, []);

  useEffect(() => {
    const timer = window.setInterval(() => setDurationTick((value) => value + 1), 1000);
    return () => window.clearInterval(timer);
  }, []);

  useEffect(() => {
    const cleanup = connectOperationHub({
      onSessionUpdated: scheduleRealtimeReload,
      onOrderUpdated: scheduleRealtimeReload,
      onBookingUpdated: scheduleRealtimeReload,
      onTableStatusChanged: scheduleRealtimeReload,
      onStatusChange: () => undefined
    });

    return () => {
      cleanup();
      if (realtimeReloadTimerRef.current !== null) {
        window.clearTimeout(realtimeReloadTimerRef.current);
        realtimeReloadTimerRef.current = null;
      }
    };
  }, [scheduleRealtimeReload]);

  useEffect(() => {
    const timer = window.setInterval(() => {
      if (!document.hidden) {
        reload();
      }
    }, 15000);
    return () => window.clearInterval(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const dueBookings = useMemo(() => {
    const now = Date.now();
    return unwrap<Booking>(data?.bookings as any).filter((booking) =>
      Number(booking.status) === BOOKING_CONFIRMED &&
      Boolean(booking.tableId) &&
      !booking.hasSession &&
      utcTimestampMs(booking.startTimeUtc) <= now &&
      utcTimestampMs(booking.endTimeUtc) > now
    );
  }, [data]);

  const activeSessions = normalizeActiveSessions(useList<Session>(data?.sessions)).filter((session) => Number(session.status) === SESSION_OPEN);
  const tables = useList<VenueTable>(data?.tables);
  const customers = useList<CustomerDto>(data?.customers).filter((customer) => customer.status);
  const activeTableIds = new Set(activeSessions.flatMap((session) =>
    getSessionActiveAssignments(session).map((assignment) => Number(assignment.tableId))
  ));
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
      const session = await bookingApi.startSession(starting.bookingId);
      toast("Bắt đầu phiên thành công.", "success");
      setStarting(null);
      router.push(`/operation/sessions/${session.sessionId}`);
    } catch (err: any) {
      const message = err?.status === 409
        ? "Booking đã có phiên hoặc bàn đang được sử dụng."
        : err?.message || "Không thể bắt đầu phiên.";
      toast(message, "error");
    }
  }

  async function markNoShow() {
    if (!noShowing) return;
    try {
      await bookingApi.noShow(noShowing.bookingId, "Khách không đến vào giờ đã đặt");
      toast("Đã đánh dấu khách không đến.", "success");
      setNoShowing(null);
      reload();
    } catch (err: any) {
      toast(err?.message || "Không thể đánh dấu khách không đến.", "error");
    }
  }

  async function endSelected() {
    if (!ending) return;
    const activeAssignmentIds = getSessionActiveAssignments(ending)
      .map((assignment) => Number(assignment.assignmentId))
      .filter((assignmentId) => assignmentId > 0);
    if (activeAssignmentIds.length === 0) {
      toast("Không còn bàn đang hoạt động để kết thúc.", "error");
      return;
    }

    setClosingSession(true);
    try {
      const result = await sessionApi.releaseTables(ending.sessionId, {
        assignmentIds: activeAssignmentIds,
        note: "Kết thúc tất cả bàn đang hoạt động"
      });
      const invoiceId = result?.invoiceId;
      toast("Kết thúc tất cả bàn và tạo hóa đơn thành công.", "success");
      setEnding(null);
      setEndingSummary(null);
      reload();
      if (invoiceId) {
        router.push(`/operation/invoices?invoiceId=${invoiceId}`);
      } else if (result?.wasSessionAutoClosed) {
        toast("Phiên đã đóng nhưng chưa nhận được mã hóa đơn.", "error");
      } else {
        router.push("/operation/invoices");
      }
    } catch (err: any) {
      toast(err?.message || "Không thể kết thúc phiên.", "error");
    } finally {
      setClosingSession(false);
    }
  }

  async function openEndConfirm(session: Session) {
    setEnding(session);
    setEndingSummary(null);
    setPreviewingSessionId(session.sessionId);
    try {
      setEndingSummary(await sessionApi.summary(session.sessionId));
    } catch (err: any) {
      setEnding(null);
      toast(err?.message || "Không tải được tạm tính.", "error");
    } finally {
      setPreviewingSessionId(null);
    }
  }

  return (
    <>
      <PageHeader title="Quản lý phiên chơi" />

      <section style={{ padding: "0 24px 24px" }}>
        <div className="panel">
          <div className="panel-head">
            <div>
              <h3>Booking đến giờ</h3>
            </div>
          </div>
          <StateBlock loading={loading} error={error} empty={!loading && dueBookings.length === 0} />
          <DataTable
            rows={dueBookings as unknown as Record<string, unknown>[]}
            columns={[
              { key: "bookingCode", label: "Mã booking" },
              { key: "customerName", label: "Khách hàng", render: (row) => String(row.customerName || row.phoneNumber || row.customerId || "-") },
              { key: "tableId", label: "Bàn / loại bàn", render: (row) => row.tableId ? `Bàn #${row.tableId}` : `Loại #${row.tableTypeId || "-"}` },
              { key: "startTimeUtc", label: "Bắt đầu", render: (row) => dateTime(String(row.startTimeUtc)) },
              { key: "endTimeUtc", label: "Kết thúc", render: (row) => dateTime(String(row.endTimeUtc)) },
              { key: "status", label: "Trạng thái", render: (row) => <Badge tone="green">{label(bookingStatus, Number(row.status))}</Badge> }
            ]}
            actions={(row) => (
              <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
                <button className="primary-btn" onClick={() => setStarting(row as unknown as Booking)}>Bắt đầu phiên</button>
                <button className="danger-btn" onClick={() => setNoShowing(row as unknown as Booking)}>Khách không đến</button>
              </div>
            )}
          />
        </div>
      </section>

      <section style={{ padding: "0 24px 24px" }}>
        <div className="panel">
          <div className="panel-head">
            <div>
              <h3>Phiên đang hoạt động</h3>
            </div>
            <button className="primary-btn" type="button" onClick={() => setWalkInOpen(true)}>Mở phiên khách vãng lai</button>
          </div>
          <StateBlock loading={loading} error={error} empty={!loading && activeSessions.length === 0} />
          <DataTable
            rows={activeSessions as unknown as Record<string, unknown>[]}
            columns={[
              { key: "sessionCode", label: "Mã phiên" },
              { key: "customerId", label: "Khách hàng", render: (row) => String(row.customerName || customerName(row.customerId)) },
              { key: "activeAssignments", label: "Bàn đang hoạt động", render: (row) => <ActiveTablesCell session={row as unknown as Session} /> },
              { key: "releasedTableCount", label: "Bàn đã trả", render: (row) => <ReleasedTablesCell session={row as unknown as Session} /> },
              { key: "startedAtUtc", label: "Bắt đầu", render: (row) => dateTime(String(row.startedAtUtc)) },
              { key: "duration", label: "Thời lượng", render: (row) => `Đã chơi: ${formatElapsedDuration(String(row.startedAtUtc), Number(row.status) === SESSION_OPEN ? undefined : String(row.endedAtUtc || ""))}` },
              { key: "status", label: "Trạng thái", render: (row) => <Badge tone="green">{label(sessionStatus, Number(row.status))}</Badge> }
            ]}
            actions={(row) => (
              <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
                <button className="secondary-btn" type="button" onClick={() => router.push(`/operation/sessions/${Number(row.sessionId)}`)}>Chi tiết</button>
                <button className="ghost-btn" onClick={async () => {
                  try {
                    setPreviewingSessionId(Number(row.sessionId));
                    setSummary(await sessionApi.summary(Number(row.sessionId)));
                  } catch (err: any) {
                    toast(err?.message || "Không tải được tạm tính.", "error");
                  } finally {
                    setPreviewingSessionId(null);
                  }
                }} disabled={previewingSessionId === Number(row.sessionId)}>Tạm tính</button>
                {Number(row.status) === SESSION_OPEN && (
                  <button className="danger-btn" onClick={() => openEndConfirm(row as unknown as Session)} disabled={previewingSessionId === Number(row.sessionId)}>
                    {previewingSessionId === Number(row.sessionId) ? "Đang tải..." : "Kết thúc tất cả"}
                  </button>
                )}
              </div>
            )}
          />
        </div>
      </section>

      {starting ? <ConfirmDialog title="Bắt đầu phiên" message={`Bắt đầu phiên cho booking ${starting.bookingCode || starting.bookingId}?`} confirmLabel="Bắt đầu" onCancel={() => setStarting(null)} onConfirm={startSelected} /> : null}
      {noShowing ? <ConfirmDialog title="Khách không đến" message={`Đánh dấu booking ${noShowing.bookingCode || noShowing.bookingId} là khách không đến?`} confirmLabel="Khách không đến" danger onCancel={() => setNoShowing(null)} onConfirm={markNoShow} /> : null}
      {ending ? <EndSessionModal session={ending} summary={endingSummary} busy={closingSession} onCancel={() => { if (!closingSession) { setEnding(null); setEndingSummary(null); } }} onAddOrder={() => router.push(`/operation/orders?sessionId=${ending.sessionId}&returnTo=${encodeURIComponent("/operation/sessions")}`)} onConfirm={endSelected} /> : null}
      {walkInOpen ? <WalkInSessionModal tables={availableTables} customers={customers} onClose={() => setWalkInOpen(false)} onStarted={async (session) => { setWalkInOpen(false); router.push(`/operation/sessions/${session.sessionId}`); }} /> : null}
      {summary ? <SummaryModal summary={summary} onClose={() => setSummary(null)} /> : null}
    </>
  );
}

function WalkInSessionModal({ tables, customers, onClose, onStarted }: {
  tables: VenueTable[];
  customers: CustomerDto[];
  onClose: () => void;
  onStarted: (session: Session) => Promise<void>;
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
  const tableLabel = selectedTable?.tableCode || selectedTable?.tableName || "đã chọn";
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
    return utcTimestampMs(selectedPeriod.startTimeUtc) <= now && utcTimestampMs(selectedPeriod.endTimeUtc) > now;
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
      const start = utcTimestampMs(booking.startTimeUtc);
      const end = utcTimestampMs(booking.endTimeUtc);
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
      const session = await sessionApi.start({
        tableId: Number(tableId),
        ...(customerId ? { customerId: Number(customerId) } : {})
      });
      toast("Mở phiên thành công", "success");
      await onStarted(session);
    } catch (err) {
      toast(getStartSessionErrorMessage(err, tableLabel), "error");
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

function ActiveTablesCell({ session }: { session: Session }) {
  const assignments = getSessionActiveAssignments(session);
  if (!assignments.length) {
    return <span title="Phiên đang mở nhưng không có bàn đang hoạt động" style={{ color: "#b45309", fontWeight: 700 }}>Không còn bàn hoạt động</span>;
  }

  const names = assignments.map((assignment) => assignment.tableName || assignment.tableCode || `Bàn #${assignment.tableId}`);
  const visible = names.slice(0, 2);
  const hiddenCount = Math.max(0, names.length - visible.length);

  return (
    <span title={names.join("\n")} style={{ display: "inline-flex", gap: 6, flexWrap: "wrap", maxWidth: 260 }}>
      {visible.map((name) => <Badge key={name} tone="green">{name}</Badge>)}
      {hiddenCount > 0 ? <Badge>+{hiddenCount}</Badge> : null}
    </span>
  );
}

function ReleasedTablesCell({ session }: { session: Session }) {
  const assignments = getSessionReleasedAssignments(session);
  const count = session.releasedTableCount ?? assignments.length;
  if (count === 0) return <span>0</span>;

  const detail = assignments.map((assignment) => {
    const name = assignment.tableName || assignment.tableCode || `Bàn #${assignment.tableId}`;
    return `${name} - ${dateTime(assignment.startedAtUtc)} đến ${dateTime(assignment.endedAtUtc)} - ${money(Number(assignment.amount || 0))}`;
  });

  return <span title={detail.join("\n")}>{count} bàn</span>;
}

function getStartSessionErrorMessage(error: unknown, tableLabel: string) {
  if (error instanceof ApiError && error.status === 409) {
    const details = error.errors.filter((item) => item && item !== error.message).join("; ");
    const message = error.message || `Bàn ${tableLabel} chưa được cấu hình bảng giá cho thời điểm hiện tại. Vui lòng kiểm tra Pricing Plan.`;
    return details ? `${message} (${details})` : message;
  }

  return error instanceof Error ? error.message : "Không thể mở phiên.";
}

function EndSessionModal({ session, summary, busy, onCancel, onAddOrder, onConfirm }: {
  session: Session;
  summary: any;
  busy: boolean;
  onCancel: () => void;
  onAddOrder: () => void;
  onConfirm: () => Promise<void>;
}) {
  const data = summary?.data || summary;
  const duration = data?.currentDurationMinutes ?? data?.durationMinutes ?? session.durationMinutes ?? 0;
  const timeAmount = data?.timeSubtotalAmount ?? 0;
  const orderAmount = data?.productSubtotalAmount ?? data?.orderSubtotalAmount ?? 0;
  const discountAmount = data?.discountAmount ?? 0;
  const total = data?.grandTotalAmount ?? Math.max(0, Number(timeAmount) + Number(orderAmount) - Number(discountAmount));

  return (
    <Modal title="Kết thúc toàn bộ phiên chơi" onClose={onCancel} size="small">
      <div style={{ display: "grid", gap: 12 }}>
        <p className="modal-message" style={{ margin: 0 }}>
          Bạn có chắc muốn kết thúc phiên {session.sessionCode || session.sessionId}?
        </p>
        {!summary ? <div className="state-card loading-state"><span className="spinner" />Đang tải tạm tính...</div> : (
          <div style={{ display: "grid", gap: 10 }}>
            <SummaryRow label="Thời lượng" value={`${duration} phút`} />
            <PricingDetails assignments={data?.assignments || data?.Assignments || []} />
            <SummaryRow label="Tiền giờ" value={money(Number(timeAmount))} />
            <SummaryRow label="Tiền đơn hàng" value={money(Number(orderAmount))} />
            <SummaryRow label="Giảm giá" value={`-${money(Number(discountAmount))}`} />
            <SummaryRow label="Tổng tiền" value={money(Number(total))} />
          </div>
        )}
        <div className="modal-actions">
          <button className="ghost-btn" type="button" onClick={onAddOrder} disabled={busy || Number(session.status) !== SESSION_OPEN}>Quay lại thêm đơn hàng</button>
          <button className="danger-btn" type="button" onClick={onConfirm} disabled={busy || !summary}>
            {busy ? "Đang xử lý..." : "Kết thúc tất cả & tạo hóa đơn"}
          </button>
        </div>
      </div>
    </Modal>
  );
}

function PricingDetails({ assignments }: { assignments: any[] }) {
  if (!assignments.length) return null;

  return (
    <div style={{ display: "grid", gap: 8 }}>
      {assignments.map((assignment) => {
        const actual = assignment.actualDurationMinutes ?? assignment.durationMinutes ?? 0;
        const billable = assignment.billableDurationMinutes ?? assignment.billableMinutes ?? actual;
        const minimum = assignment.minimumMinutes ?? 0;
        const block = assignment.billingBlockMinutes ?? 0;
        const rate = assignment.hourlyRate ?? assignment.hourlyRateSnapshot ?? 0;
        const reason = billable > actual
          ? minimum > actual
            ? `Áp dụng thời gian tối thiểu ${minimum} phút`
            : `Làm tròn theo block ${block} phút`
          : null;

        return (
          <div key={assignment.sessionTableAssignmentId || assignment.assignmentId || `${assignment.tableId}-${assignment.startedAtUtc}`} style={{ border: "1px solid #e2e8f0", borderRadius: 10, padding: "10px 12px", background: "#fff" }}>
            <div style={{ display: "flex", justifyContent: "space-between", gap: 12, fontSize: 13 }}>
              <strong>{assignment.tableName || assignment.tableCode || "Bàn"}</strong>
              <span>{money(Number(rate))}/giờ</span>
            </div>
            <div style={{ display: "grid", gap: 3, marginTop: 6, color: "#475569", fontSize: 13 }}>
              <span>Thực tế: {actual} phút · Tính tiền: {billable} phút</span>
              <span>Minimum: {minimum} phút · Block: {block} phút</span>
              {assignment.pricingPlanName ? <span>Bảng giá: {assignment.pricingPlanName}</span> : null}
              {reason ? <span style={{ color: "#b45309", fontWeight: 700 }}>{reason}</span> : null}
            </div>
          </div>
        );
      })}
    </div>
  );
}

function SummaryModal({ summary, onClose }: { summary: any; onClose: () => void }) {
  const data = summary?.data || summary;
  const timeAmount = data?.timeSubtotalAmount ?? data?.TimeSubtotalAmount ?? 0;
  const orderAmount = data?.orderSubtotalAmount ?? data?.productSubtotalAmount ?? data?.ProductSubtotalAmount ?? 0;
  const discountAmount = data?.discountAmount ?? data?.DiscountAmount ?? 0;
  const total = data?.grandTotalAmount ?? data?.subtotalAmount ?? data?.SubtotalAmount ?? Math.max(0, timeAmount + orderAmount - discountAmount);
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
            <h3 style={{ margin: 0, fontSize: 20, fontWeight: 700, color: "#0f172a" }}>Tạm tính / Kết quả đóng phiên</h3>
            <p style={{ margin: "6px 0 0", color: "#475569", fontSize: 14 }}>{data?.sessionCode || data?.SessionCode || `Phiên #${data?.sessionId || data?.SessionId || ""}`}</p>
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
            Đóng
          </button>
        </div>
        <div style={{ display: "grid", gap: 12 }}>
          <SummaryRow label="Mã phiên" value={data?.sessionCode || data?.SessionCode || data?.sessionId || data?.SessionId || "-"} />
          <SummaryRow label="Thời lượng" value={`${duration ?? "-"} phút`} />
          <PricingDetails assignments={data?.assignments || data?.Assignments || []} />
          <SummaryRow label="Tiền giờ" value={money(Number(timeAmount))} />
          <SummaryRow label="Tiền sản phẩm/đơn hàng" value={money(Number(orderAmount))} />
          <SummaryRow label="Giam gia" value={`-${money(Number(discountAmount))}`} />
          <SummaryRow label="Hóa đơn" value={data?.invoiceCode || data?.InvoiceCode || (data?.invoiceGenerated ? "Đã tạo" : "Chưa tạo")} />
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 16, borderTop: "1px solid #e2e8f0", marginTop: 4, paddingTop: 16 }}>
            <span style={{ color: "#0f172a", fontWeight: 700 }}>Tổng tiền</span>
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
