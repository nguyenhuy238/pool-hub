"use client";

import { useMemo, useState } from "react";
import type { ReactNode } from "react";
import { bookingApi, sessionApi } from "@/lib/api/endpoints";
import { getVietnamDateInputValue, vietnamDateRangeToUtcIso } from "@/lib/dateTime";
import { dateTime, label, bookingStatus, sessionStatus, money } from "@/lib/status";
import { Badge, ConfirmDialog, DataTable, PageHeader, StateBlock, useList, useLoad } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { Booking, Session } from "@/types";

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

  const { data, loading, error, reload } = useLoad(async () => {
    const { startUtc, endUtc } = vietnamDateRangeToUtcIso(getVietnamDateInputValue());

    const [bookingsRes, activeSessionsRes] = await Promise.all([
      bookingApi.calendar(startUtc, endUtc, { status: BOOKING_CONFIRMED, pageNumber: 1, pageSize: 100 }),
      sessionApi.active()
    ]);

    return {
      bookings: bookingsRes,
      sessions: activeSessionsRes
    };
  }, []);

  const dueBookings = useMemo(() => {
    const now = Date.now();
    return unwrap<Booking>(data?.bookings as any).filter((booking) =>
      Number(booking.status) === BOOKING_CONFIRMED &&
      Boolean(booking.tableId) &&
      new Date(booking.startTimeUtc).getTime() <= now &&
      new Date(booking.endTimeUtc).getTime() > now
    );
  }, [data]);

  const activeSessions = useList<Session>(data?.sessions).filter((session) => Number(session.status) === SESSION_OPEN);

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
          </div>
          <StateBlock loading={loading} error={error} empty={!loading && activeSessions.length === 0} />
          <DataTable
            rows={activeSessions as unknown as Record<string, unknown>[]}
            columns={[
              { key: "sessionCode", label: "Ma session" },
              { key: "customerId", label: "Khach hang", render: (row) => String(row.customerId || "-") },
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
      {summary ? <SummaryModal summary={summary} onClose={() => setSummary(null)} /> : null}
    </>
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
