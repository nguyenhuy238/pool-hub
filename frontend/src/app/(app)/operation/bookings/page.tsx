"use client";

import { useEffect, useState } from "react";
import type { CSSProperties, ReactNode } from "react";
import { bookingApi, venueApi } from "@/lib/api/endpoints";
import { getTotalPages } from "@/lib/api/client";
import { formatVietnamTime, utcTimestampMs, utcToVietnamDatetimeLocal, vietnamDatetimeLocalToUtcIso } from "@/lib/dateTime";
import { dateTime, label, bookingStatus, depositStatus, money } from "@/lib/status";
import { Badge, ConfirmDialog, DataTable, ListControls, PageHeader, StateBlock, useList, useLoad, Pagination } from "@/components/ui";
import { useToast } from "@/components/toast";
import { compactBookingTablesLabel, getBookingTables, normalizeTableIds } from "@/lib/bookingTables";
import type { Booking } from "@/types";
import { BookingModal } from "./BookingModal";

const BOOKING_PENDING = 1;
const BOOKING_CONFIRMED = 2;
const BOOKING_CANCELLED = 3;
const BOOKING_COMPLETED = 4;
const BOOKING_NO_SHOW = 5;
const BOOKING_PENDING_DEPOSIT = 6;
const BOOKING_PENDING_APPROVAL = 7;
const BOOKING_EXPIRED = 8;
const BOOKING_IN_PROGRESS = 9;
const DEPOSIT_PENDING_VERIFICATION = 9;
const DEPOSIT_PENDING = 2;
const DEPOSIT_PAID = 3;
const DEPOSIT_APPLIED_TO_INVOICE = 4;
const DEPOSIT_FORFEITED = 7;
const NO_SHOW_GRACE_MINUTES = 15;

type TableOption = { tableId: number; tableName?: string; tableCode?: string; tableTypeId?: number };
type TableTypeOption = { tableTypeId: number; name?: string };

function hasStartedSession(row: Record<string, unknown>) {
  return Boolean(row.sessionId || row.hasSession);
}

function getBookingCustomerDisplay(row: Record<string, unknown>) {
  const customer = row.customer && typeof row.customer === "object"
    ? row.customer as Record<string, unknown>
    : undefined;
  const candidates = [
    row.customerName,
    row.customerFullName,
    customer?.fullName,
    row.walkInCustomerName,
    row.guestName,
    row.contactName
  ];
  const placeholders = new Set(["anonymous", "guest", "khách vãng lai"]);
  const name = candidates
    .map((value) => typeof value === "string" ? value.trim() : "")
    .find((value) => value && !placeholders.has(value.toLocaleLowerCase("vi-VN")));

  return name || "Khách vãng lai";
}

function getDepositFlowLabel(bookingStatusValue: number, depositStatusValue?: number) {
  if (bookingStatusValue === BOOKING_PENDING_DEPOSIT && depositStatusValue === DEPOSIT_PENDING) return "Chờ khách chuyển khoản";
  if (bookingStatusValue === BOOKING_PENDING_DEPOSIT && depositStatusValue === DEPOSIT_PENDING_VERIFICATION) return "Chờ xác minh cọc";
  if (bookingStatusValue === BOOKING_CONFIRMED && depositStatusValue === DEPOSIT_PAID) return "Đã nhận cọc";
  if (bookingStatusValue === BOOKING_EXPIRED) return "Hết hạn thanh toán cọc";
  if (bookingStatusValue === BOOKING_NO_SHOW && depositStatusValue === DEPOSIT_FORFEITED) return "Đã mất cọc";
  if (bookingStatusValue === BOOKING_COMPLETED && depositStatusValue === DEPOSIT_APPLIED_TO_INVOICE) return "Đã trừ vào hóa đơn";
  return depositStatusValue ? label(depositStatus, depositStatusValue) : "-";
}

function getStartSessionState(row: Record<string, unknown>, nowMs: number) {
  const startMs = utcTimestampMs(typeof row.startTimeUtc === "string" ? row.startTimeUtc : null);
  const endMs = utcTimestampMs(typeof row.endTimeUtc === "string" ? row.endTimeUtc : null);

  if (!Number.isFinite(startMs) || !Number.isFinite(endMs)) {
    return { canStart: false, message: "Thời gian booking không hợp lệ." };
  }

  if (nowMs >= endMs) {
    return { canStart: false, message: "Booking đã quá giờ kết thúc." };
  }

  return { canStart: true, message: "" };
}

function getNoShowState(row: Record<string, unknown>, nowMs: number) {
  const startMs = utcTimestampMs(typeof row.startTimeUtc === "string" ? row.startTimeUtc : null);

  if (!Number.isFinite(startMs)) {
    return { canNoShow: false, message: "Thời gian booking không hợp lệ." };
  }

  const noShowMs = startMs + NO_SHOW_GRACE_MINUTES * 60 * 1000;
  if (nowMs < noShowMs) {
    return {
      canNoShow: false,
      message: `Có thể đánh dấu không đến từ ${formatVietnamTime(new Date(noShowMs).toISOString())}.`
    };
  }

  return { canNoShow: true, message: "" };
}

export default function BookingsPage() {
  const toast = useToast();
  const [status, setStatus] = useState("");
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editing, setEditing] = useState<Booking | null>(null);
  const [cancelling, setCancelling] = useState<Booking | null>(null);
  const [startingSession, setStartingSession] = useState<Booking | null>(null);
  const [noShowing, setNoShowing] = useState<Booking | null>(null);
  const [nowMs, setNowMs] = useState(() => Date.now());

  useEffect(() => {
    const timer = window.setInterval(() => setNowMs(Date.now()), 30000);
    return () => window.clearInterval(timer);
  }, []);

  const { data, loading, error, reload } = useLoad(async () => {
    const [bookingsRes, tablesRes, typesRes] = await Promise.all([
      bookingApi.list({ Status: status || undefined, Search: params.search, PageNumber: params.pageNumber, PageSize: params.pageSize }),
      venueApi.tables({ pageSize: 500 }),
      venueApi.tableTypes({ pageSize: 100 })
    ]);

    return {
      bookings: bookingsRes,
      tables: Array.isArray(tablesRes) ? tablesRes : (tablesRes && "items" in tablesRes ? tablesRes.items : []),
      tableTypes: Array.isArray(typesRes) ? typesRes : (typesRes && "items" in typesRes ? typesRes.items : [])
    };
  }, [status, params]);

  const rows = useList<Booking>(data?.bookings);
  const tables = data?.tables || [];
  const tableTypes = data?.tableTypes || [];

  async function action(fn: Promise<unknown>, message: string) {
    await fn.then(() => toast(message, "success")).catch((err) => toast(err.message, "error"));
    reload();
  }

  return (
    <>
      <PageHeader
        title="Quản lý đặt bàn"
        description="Theo dõi và tạo lịch đặt bàn mới cho khách hàng."
        action={
          <div style={{ display: "flex", gap: "12px" }}>
            <a href="/operation/bookings/calendar" className="primary-btn" style={{ background: "#123b63", textDecoration: "none" }}>Xem lịch</a>
            <select value={status} onChange={(e) => setStatus(e.target.value)} style={{ padding: "8px", borderRadius: "6px", border: "1px solid var(--line)" }}>
              <option value="">Tất cả trạng thái</option>
              <option value="1">Chờ xác nhận</option>
              <option value="2">Đã xác nhận</option>
              <option value="3">Đã hủy</option>
              <option value="4">Hoàn thành</option>
              <option value="5">Khách không đến</option>
              <option value="6">Chờ thanh toán cọc</option>
              <option value="7">Chờ quản lý duyệt</option>
              <option value="8">Hết hạn</option>
              <option value="9">Đang sử dụng</option>
            </select>
          </div>
        }
      />
      <ListControls search={params.search} pageNumber={params.pageNumber} pageSize={params.pageSize} onChange={setParams} />

      <div style={{ padding: "0 24px", marginBottom: "16px" }}>
        <button className="primary-btn" onClick={() => setIsModalOpen(true)}>+ Tạo lịch đặt bàn</button>
      </div>

      {isModalOpen && (
        <BookingModal
          onClose={() => setIsModalOpen(false)}
          onSuccess={() => {
            setIsModalOpen(false);
            reload();
          }}
          tables={tables}
        />
      )}

      {editing && (
        <EditBookingModal
          booking={editing}
          tables={tables}
          tableTypes={tableTypes}
          onClose={() => setEditing(null)}
          onSaved={async () => {
            setEditing(null);
            await reload();
          }}
        />
      )}

      <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
      <DataTable
        rows={rows as unknown as Record<string, unknown>[]}
        columns={[
          { key: "bookingCode", label: "Mã Booking" },
          { key: "customerName", label: "Khách hàng", render: (row) => (
            <div>
              <strong style={{ color: "var(--ink)" }}>{getBookingCustomerDisplay(row)}</strong>
              {getBookingCustomerDisplay(row) !== "Khách vãng lai" ? <span style={{ fontSize: 12, color: "var(--muted)", fontWeight: 500 }}> (Khách vãng lai)</span> : null}<br />
              <span style={{ fontSize: "13px", color: "var(--muted)" }}>{String(row.phoneNumber || "-")}</span>
            </div>
          ) },
          { key: "tableId", label: "Bàn / Loại bàn", render: (row) => {
            const bookingTables = getBookingTables(row as Partial<Booking>, tables);
            if (bookingTables.length) {
              return (
                <div title={bookingTables.map((table) => table.tableName || table.tableCode || `Bàn #${table.tableId}`).join("\n")}>
                  <strong>{compactBookingTablesLabel(row as Partial<Booking>, tables)}</strong>
                  <div style={{ color: "var(--muted)", fontSize: 12 }}>{bookingTables.length} bàn</div>
                </div>
              );
            }
            if (row.tableTypeId) {
              const tableType = tableTypes.find((item) => item.tableTypeId === Number(row.tableTypeId));
              return tableType ? <span>Loại: {tableType.name}</span> : `Loại #${row.tableTypeId}`;
            }
            return <span style={{ color: "var(--muted)" }}>Chưa xếp bàn</span>;
          } },
          { key: "startTimeUtc", label: "Bắt đầu", render: (row) => dateTime(String(row.startTimeUtc)) },
          { key: "endTimeUtc", label: "Kết thúc", render: (row) => dateTime(String(row.endTimeUtc)) },
          { key: "status", label: "Trạng thái", render: (row) => {
            const statusValue = Number(row.status);
            return <Badge tone={statusValue === BOOKING_CANCELLED || statusValue === BOOKING_NO_SHOW || statusValue === BOOKING_EXPIRED ? "red" : statusValue === BOOKING_CONFIRMED ? "green" : statusValue === BOOKING_COMPLETED || statusValue === BOOKING_IN_PROGRESS ? "blue" : "yellow"}>{label(bookingStatus, statusValue)}</Badge>;
          } },
          { key: "deposit", label: "Cọc", render: (row) => {
            const deposit = row.deposit as { requiredAmount?: number; paidAmount?: number; appliedAmount?: number; refundedAmount?: number; forfeitedAmount?: number; status?: number } | undefined;
            return (
              <div style={{ display: "grid", gap: 3, fontSize: 13 }}>
                <span>Tạm tính: <strong>{money(Number(row.estimatedAmount || 0))}</strong></span>
                <span>Cần cọc: <strong>{money(deposit?.requiredAmount)}</strong></span>
                <span>Đã cọc: <strong>{money(deposit?.paidAmount)}</strong></span>
                <span>Đã trừ HĐ: <strong>{money(deposit?.appliedAmount)}</strong></span>
                <span>Hoàn / mất: <strong>{money(deposit?.refundedAmount)} / {money(deposit?.forfeitedAmount)}</strong></span>
                <span>Trạng thái cọc: {getDepositFlowLabel(Number(row.status), deposit?.status)}</span>
              </div>
            );
          } }
        ]}
        actions={(row) => {
          const statusValue = Number(row.status);
          const hasSession = hasStartedSession(row);
          const isPending = statusValue === BOOKING_PENDING;
          const isPendingDeposit = statusValue === BOOKING_PENDING_DEPOSIT;
          const isPendingApproval = statusValue === BOOKING_PENDING_APPROVAL;
          const isConfirmed = statusValue === BOOKING_CONFIRMED;
          const deposit = row.deposit as { requiredAmount?: number; status?: number } | undefined;
          const depositStatusValue = Number(deposit?.status || 0);

          const canConfirm = isPending && !hasSession;
          const canApprove = isPendingApproval && !hasSession;
          const canConfirmDeposit = isPendingDeposit && !hasSession;
          const canRejectDeposit = isPendingDeposit && depositStatusValue === DEPOSIT_PENDING_VERIFICATION && !hasSession;
          const startSessionState = getStartSessionState(row, nowMs);
          const noShowState = getNoShowState(row, nowMs);
          const canStartSession = isConfirmed && !hasSession;
          const canEdit = (isPending || isPendingDeposit || isPendingApproval || isConfirmed) && !hasSession;
          const canNoShow = isConfirmed && !hasSession;
          const canCancel = (isPending || isPendingDeposit || isPendingApproval || isConfirmed) && !hasSession;

          return (
            <div style={{ display: "flex", gap: 6, flexWrap: "wrap", alignItems: "center", justifyContent: "flex-start", minWidth: 270 }}>
              {canConfirm && (
                <button
                  className="primary-btn compact"
                  style={{ whiteSpace: "nowrap" }}
                  onClick={() => action(bookingApi.confirm(Number(row.bookingId)), "Đã xác nhận booking.")}
                >
                  Xác nhận
                </button>
              )}
              {canApprove && (
                <button
                  className="primary-btn compact"
                  style={{ whiteSpace: "nowrap", background: "#7c3aed", borderColor: "#6d28d9" }}
                  onClick={() => action(bookingApi.approve(Number(row.bookingId)), "Đã duyệt booking.")}
                >
                  Duyệt
                </button>
              )}
              {canConfirmDeposit && (
                <button
                  className="primary-btn compact"
                  style={{ whiteSpace: "nowrap", background: "#0f766e", borderColor: "#0f766e" }}
                  onClick={() => action(
                    bookingApi.confirmDeposit(Number(row.bookingId), Number(deposit?.requiredAmount || 0)),
                    "Đã xác nhận nhận cọc và xác nhận booking."
                  )}
                >
                  Xác nhận cọc
                </button>
              )}
              {canRejectDeposit && (
                <button
                  className="ghost-btn compact"
                  style={{ whiteSpace: "nowrap", borderColor: "#f59e0b", color: "#92400e", background: "#fffbeb" }}
                  onClick={() => action(bookingApi.rejectDepositTransfer(Number(row.bookingId), "Staff rejected deposit verification."), "Đã từ chối xác minh cọc.")}
                >
                  Từ chối cọc
                </button>
              )}
              {canStartSession && (
                <span title={startSessionState.message || undefined}>
                  <button
                    className="primary-btn compact"
                    disabled={!startSessionState.canStart}
                    style={{
                      whiteSpace: "nowrap",
                      background: startSessionState.canStart ? "#10b981" : "#94a3b8",
                      borderColor: startSessionState.canStart ? "#059669" : "#94a3b8",
                      cursor: startSessionState.canStart ? "pointer" : "not-allowed"
                    }}
                    onClick={() => {
                      if (!startSessionState.canStart) {
                        toast(startSessionState.message || "Chưa thể nhận bàn.", "error");
                        return;
                      }
                      if (!getBookingTables(row as Partial<Booking>, tables).length) {
                        toast("Booking chưa xếp bàn. Vui lòng chỉnh sửa để chọn bàn trước.", "error");
                        return;
                      }
                      setStartingSession(row as unknown as Booking);
                    }}
                  >
                    Nhận bàn
                  </button>
                </span>
              )}
              {canEdit && (
                <button
                  className="ghost-btn compact"
                  style={{ whiteSpace: "nowrap", borderColor: "#93c5fd", color: "#1d4ed8", background: "#eff6ff" }}
                  onClick={() => setEditing(row as unknown as Booking)}
                >
                  Chỉnh sửa
                </button>
              )}
              {canNoShow && (
                <span title={noShowState.message || undefined}>
                  <button
                    className="ghost-btn compact"
                    disabled={!noShowState.canNoShow}
                    style={{
                      whiteSpace: "nowrap",
                      borderColor: "#fcd34d",
                      color: noShowState.canNoShow ? "#b45309" : "#94a3b8",
                      background: noShowState.canNoShow ? "#fffbeb" : "#f8fafc",
                      cursor: noShowState.canNoShow ? "pointer" : "not-allowed"
                    }}
                    onClick={() => {
                      if (!noShowState.canNoShow) {
                        toast(noShowState.message || "Chưa thể đánh dấu không đến.", "error");
                        return;
                      }
                      setNoShowing(row as unknown as Booking);
                    }}
                  >
                    Không đến
                  </button>
                </span>
              )}
              {canCancel && (
                <button
                  className="danger-btn compact"
                  style={{ whiteSpace: "nowrap" }}
                  onClick={() => setCancelling(row as unknown as Booking)}
                >
                  Hủy
                </button>
              )}
              {hasSession && row.sessionId ? (
                <a
                  href={`/operation/sessions/${row.sessionId}`}
                  className="ghost-btn compact"
                  style={{ whiteSpace: "nowrap", textDecoration: "none", borderColor: "#cbd5e1", color: "#475569" }}
                >
                  Xem phiên
                </a>
              ) : null}
            </div>
          );
        }}
      />
      <Pagination
        pageNumber={params.pageNumber}
        totalPages={getTotalPages(data?.bookings, params.pageSize)}
        onChange={(page) => setParams((prev) => ({ ...prev, pageNumber: page }))}
      />
      {startingSession ? (
        <ConfirmDialog
          title="Nhận bàn / Mở bàn"
          message={`Xác nhận mở phiên cho booking ${startingSession.bookingCode || startingSession.bookingId}: ${getBookingTables(startingSession, tables).map((table) => table.tableName || table.tableCode || `Bàn #${table.tableId}`).join(", ")}?`}
          confirmLabel="Mở bàn"
          onCancel={() => setStartingSession(null)}
          onConfirm={async () => {
            const startState = getStartSessionState(startingSession as unknown as Record<string, unknown>, Date.now());
            if (!startState.canStart) {
              toast(startState.message || "Chưa thể nhận bàn.", "error");
              setStartingSession(null);
              return;
            }
            await action(
              bookingApi.startSession(Number(startingSession.bookingId)),
              "Đã nhận bàn và mở phiên chơi thành công."
            );
            setStartingSession(null);
          }}
        />
      ) : null}
      {noShowing ? (
        <ConfirmDialog
          title="Khách không đến"
          message={`Xác nhận đánh dấu booking ${noShowing.bookingCode || noShowing.bookingId} là khách không đến (No-Show)?`}
          confirmLabel="Xác nhận"
          danger
          onCancel={() => setNoShowing(null)}
          onConfirm={async () => {
            await action(bookingApi.noShow(Number(noShowing.bookingId)), "Đã đánh dấu khách không đến.");
            setNoShowing(null);
          }}
        />
      ) : null}
      {cancelling ? <ConfirmDialog title="Hủy đặt bàn" message={`Xác nhận hủy đặt bàn ${cancelling.bookingCode || cancelling.bookingId}?`} confirmLabel="Hủy đặt bàn" danger onCancel={() => setCancelling(null)} onConfirm={async () => { await action(bookingApi.cancel(cancelling.bookingId), "Đã hủy booking."); setCancelling(null); }} /> : null}
    </>
  );
}

function EditBookingModal({
  booking,
  tables,
  tableTypes,
  onClose,
  onSaved
}: {
  booking: Booking;
  tables: TableOption[];
  tableTypes: TableTypeOption[];
  onClose: () => void;
  onSaved: () => Promise<void>;
}) {
  const toast = useToast();
  const [selectedTableIds, setSelectedTableIds] = useState<number[]>(() => normalizeTableIds(getBookingTables(booking, tables).map((table) => table.tableId)));
  const [tableTypeId, setTableTypeId] = useState(String(booking.tableTypeId || ""));
  const [startTime, setStartTime] = useState(utcToVietnamDatetimeLocal(booking.startTimeUtc));
  const [endTime, setEndTime] = useState(utcToVietnamDatetimeLocal(booking.endTimeUtc));
  const [numberOfGuests, setNumberOfGuests] = useState(String(booking.numberOfGuests || 1));
  const [note, setNote] = useState(booking.note || "");
  const [saving, setSaving] = useState(false);

  async function save() {
    const startUtc = startTime ? new Date(vietnamDatetimeLocalToUtcIso(startTime)) : null;
    const endUtc = endTime ? new Date(vietnamDatetimeLocalToUtcIso(endTime)) : null;
    if (!startUtc || !endUtc || Number.isNaN(startUtc.getTime()) || Number.isNaN(endUtc.getTime())) {
      toast("Vui lòng chọn thời gian hợp lệ.", "error");
      return;
    }
    if (endUtc <= startUtc) {
      toast("Giờ kết thúc phải lớn hơn giờ bắt đầu.", "error");
      return;
    }
    if (endUtc.getTime() <= Date.now()) {
      toast("Giờ kết thúc phải ở tương lai.", "error");
      return;
    }
    if ((Number(numberOfGuests) || 0) <= 0) {
      toast("Số lượng khách phải lớn hơn 0.", "error");
      return;
    }
    if (!selectedTableIds.length) {
      toast("Vui lòng chọn ít nhất một bàn.", "error");
      return;
    }

    setSaving(true);
    try {
      await bookingApi.update(booking.bookingId, {
        startTimeUtc: vietnamDatetimeLocalToUtcIso(startTime),
        endTimeUtc: vietnamDatetimeLocalToUtcIso(endTime),
        tableId: selectedTableIds[0] || undefined,
        tableIds: selectedTableIds,
        tableTypeId: tableTypeId ? Number(tableTypeId) : undefined,
        numberOfGuests: Number(numberOfGuests),
        note
      });
      toast("Cập nhật booking thành công.", "success");
      await onSaved();
    } catch (err: any) {
      const message = err?.status === 409
        ? "Khung giờ hoặc bàn đã có booking/session khác."
        : err?.message || "Cập nhật booking thất bại.";
      toast(message, "error");
    } finally {
      setSaving(false);
    }
  }

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
          width: "min(640px, 100%)",
          maxHeight: "calc(100vh - 48px)",
          overflow: "auto",
          borderRadius: 16,
          background: "#ffffff",
          color: "#0f172a",
          boxShadow: "0 24px 80px rgba(15, 23, 42, 0.28)",
          border: "1px solid #e2e8f0",
          padding: 24
        }}
      >
        <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", gap: 16, paddingBottom: 14, marginBottom: 18, borderBottom: "1px solid #e2e8f0" }}>
          <div>
            <h3 style={{ margin: 0, color: "#0f172a", fontSize: 20, fontWeight: 800 }}>Chỉnh sửa booking</h3>
            <p style={{ margin: "6px 0 0", color: "#475569", fontSize: 14 }}>{booking.bookingCode || `#${booking.bookingId}`}</p>
          </div>
          <button
            onClick={onClose}
            style={{
              border: "1px solid #cbd5e1",
              borderRadius: 8,
              background: "#f8fafc",
              color: "#0f172a",
              fontWeight: 700,
              padding: "8px 14px",
              cursor: "pointer"
            }}
          >
            Đóng
          </button>
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 14 }}>
          <BookingEditField label="Khách hàng"><input style={bookingInputStyle} value={booking.customerName || booking.phoneNumber || "Khách vãng lai"} readOnly /></BookingEditField>
          <BookingEditField label="Trạng thái"><input style={bookingInputStyle} value={label(bookingStatus, booking.status)} readOnly /></BookingEditField>
          <BookingEditField label="Bàn" full>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(150px, 1fr))", gap: 8 }}>
              {tables.map((table) => {
                const tableId = Number(table.tableId);
                const checked = selectedTableIds.includes(tableId);
                return (
                  <label key={tableId} style={{ display: "flex", alignItems: "center", gap: 8, border: "1px solid #cbd5e1", borderRadius: 8, padding: "8px 10px", background: checked ? "#eff6ff" : "#ffffff" }}>
                    <input
                      type="checkbox"
                      checked={checked}
                      onChange={(event) => setSelectedTableIds((current) => event.target.checked ? normalizeTableIds([...current, tableId]) : current.filter((id) => id !== tableId))}
                    />
                    <span>{table.tableName || table.tableCode || table.tableId}</span>
                  </label>
                );
              })}
            </div>
          </BookingEditField>
          <BookingEditField label="Loại bàn"><select style={bookingInputStyle} value={tableTypeId} onChange={(e) => setTableTypeId(e.target.value)}>
            <option value="">Không chọn</option>
            {tableTypes.map((type) => <option key={type.tableTypeId} value={type.tableTypeId}>{type.name || type.tableTypeId}</option>)}
          </select></BookingEditField>
          <BookingEditField label="Bắt đầu"><input style={bookingInputStyle} type="datetime-local" value={startTime} onChange={(e) => setStartTime(e.target.value)} /></BookingEditField>
          <BookingEditField label="Kết thúc"><input style={bookingInputStyle} type="datetime-local" value={endTime} onChange={(e) => setEndTime(e.target.value)} /></BookingEditField>
          <BookingEditField label="Số lượng khách"><input style={bookingInputStyle} type="number" min="1" value={numberOfGuests} onChange={(e) => setNumberOfGuests(e.target.value)} /></BookingEditField>
          <BookingEditField label="Ghi chú" full><textarea style={{ ...bookingInputStyle, minHeight: 92, resize: "vertical" }} value={note} onChange={(e) => setNote(e.target.value)} rows={3} /></BookingEditField>
        </div>
        <div style={{ display: "flex", justifyContent: "flex-end", gap: 10, marginTop: 20 }}>
          <button className="ghost-btn" onClick={onClose} disabled={saving}>Hủy</button>
          <button className="primary-btn" onClick={save} disabled={saving}>{saving ? "Đang lưu..." : "Lưu thay đổi"}</button>
        </div>
      </div>
    </div>
  );
}

const bookingInputStyle: CSSProperties = {
  width: "100%",
  minHeight: 42,
  border: "1px solid #cbd5e1",
  borderRadius: 8,
  background: "#ffffff",
  color: "#0f172a",
  padding: "9px 12px",
  fontWeight: 600
};

function BookingEditField({ label, full, children }: { label: string; full?: boolean; children: ReactNode }) {
  return (
    <label style={{ display: "grid", gap: 6, gridColumn: full ? "1 / -1" : undefined }}>
      <span style={{ color: "#475569", fontSize: 13, fontWeight: 700 }}>{label}</span>
      {children}
    </label>
  );
}
