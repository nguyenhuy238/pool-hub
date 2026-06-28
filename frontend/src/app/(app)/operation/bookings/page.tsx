"use client";

import { useState } from "react";
import type { CSSProperties, ReactNode } from "react";
import { bookingApi, venueApi } from "@/lib/api/endpoints";
import { getTotalPages } from "@/lib/api/client";
import { utcToVietnamDatetimeLocal, vietnamDatetimeLocalToUtcIso } from "@/lib/dateTime";
import { dateTime, label, bookingStatus } from "@/lib/status";
import { Badge, ConfirmDialog, DataTable, ListControls, PageHeader, StateBlock, useList, useLoad, Pagination } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { Booking } from "@/types";
import { BookingModal } from "./BookingModal";

const BOOKING_PENDING = 1;
const BOOKING_CONFIRMED = 2;
const BOOKING_CANCELLED = 3;
const BOOKING_COMPLETED = 4;

type TableOption = { tableId: number; tableName?: string; tableCode?: string; tableTypeId?: number };
type TableTypeOption = { tableTypeId: number; name?: string };

function isExpired(row: Record<string, unknown>) {
  return false;
}

function effectiveStatus(row: Record<string, unknown>) {
  return Number(row.status);
}

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

export default function BookingsPage() {
  const toast = useToast();
  const [status, setStatus] = useState("");
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editing, setEditing] = useState<Booking | null>(null);
  const [cancelling, setCancelling] = useState<Booking | null>(null);

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
          { key: "bookingCode", label: "Ma Booking" },
          { key: "customerName", label: "Khách hàng", render: (row) => (
            <div>
              <strong style={{ color: "var(--ink)" }}>{getBookingCustomerDisplay(row)}</strong>
              {getBookingCustomerDisplay(row) !== "Khách vãng lai" ? <span style={{ fontSize: 12, color: "var(--muted)", fontWeight: 500 }}> (Khách vãng lai)</span> : null}<br />
              <span style={{ fontSize: "13px", color: "var(--muted)" }}>{String(row.phoneNumber || "-")}</span>
            </div>
          ) },
          { key: "tableId", label: "Bàn / Loại bàn", render: (row) => {
            if (row.tableId) {
              const table = tables.find((item) => item.tableId === Number(row.tableId));
              return table ? <strong>{table.tableName}</strong> : `Bàn #${row.tableId}`;
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
            const statusValue = effectiveStatus(row);
            return <Badge tone={statusValue === 3 ? "red" : statusValue === 2 ? "green" : statusValue === 4 ? "blue" : "yellow"}>{label(bookingStatus, statusValue)}</Badge>;
          } }
        ]}
        actions={(row) => {
          const statusValue = effectiveStatus(row);
          const canConfirm = statusValue === BOOKING_PENDING;
          const canEdit = (statusValue === BOOKING_PENDING || statusValue === BOOKING_CONFIRMED) && !hasStartedSession(row);
          const canCancel = (statusValue === BOOKING_PENDING || statusValue === BOOKING_CONFIRMED) && !hasStartedSession(row);
          return (
            <div style={{ display: "flex", gap: 6, flexWrap: "wrap", alignItems: "center", justifyContent: "flex-start", minWidth: 240 }}>
              {canConfirm && (
                <button
                  className="primary-btn compact"
                  style={{ whiteSpace: "nowrap" }}
                  onClick={() => action(bookingApi.confirm(Number(row.bookingId)), "Đã xác nhận booking.")}
                >
                  Xác nhận
                </button>
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
              {canCancel && (
                <button
                  className="danger-btn compact"
                  style={{ whiteSpace: "nowrap" }}
                  onClick={() => setCancelling(row as unknown as Booking)}
                >
                  Hủy
                </button>
              )}
            </div>
          );
        }}
      />
      <Pagination
        pageNumber={params.pageNumber}
        totalPages={getTotalPages(data?.bookings, params.pageSize)}
        onChange={(page) => setParams((prev) => ({ ...prev, pageNumber: page }))}
      />
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
  const [tableId, setTableId] = useState(String(booking.tableId || ""));
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

    setSaving(true);
    try {
      await bookingApi.update(booking.bookingId, {
        startTimeUtc: vietnamDatetimeLocalToUtcIso(startTime),
        endTimeUtc: vietnamDatetimeLocalToUtcIso(endTime),
        tableId: tableId ? Number(tableId) : undefined,
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
          <BookingEditField label="Bàn"><select style={bookingInputStyle} value={tableId} onChange={(e) => setTableId(e.target.value)}>
            <option value="">Chưa xếp bàn</option>
            {tables.map((table) => <option key={table.tableId} value={table.tableId}>{table.tableName || table.tableCode || table.tableId}</option>)}
          </select></BookingEditField>
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
