"use client";

import { useEffect, useMemo, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { bookingApi, orderApi, productApi, sessionApi, venueApi } from "@/lib/api/endpoints";
import { dateTime, label, money, sessionStatus } from "@/lib/status";
import { formatElapsedDuration } from "@/lib/sessionDuration";
import { connectOperationHub, type OperationRealtimeStatus } from "@/lib/realtime/operationHub";
import { Badge, DataTable, Modal, PageHeader, StateBlock, useList, useLoad } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { BookingCalendarItem, Order, Product, Session, VenueTable } from "@/types";

const SESSION_OPEN = 1;
const SESSION_CLOSED = 2;

export default function SessionDetailPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const toast = useToast();
  const sessionId = Number(params?.id);
  const [selectedOrderId, setSelectedOrderId] = useState<number | null>(null);
  const [closing, setClosing] = useState(false);
  const [summaryOpen, setSummaryOpen] = useState(false);
  const [transferOpen, setTransferOpen] = useState(false);
  const [reopenOpen, setReopenOpen] = useState(false);
  const [realtimeStatus, setRealtimeStatus] = useState<OperationRealtimeStatus>("connecting");
  const [, setDurationTick] = useState(0);

  const { data, loading, error, reload } = useLoad(async () => {
    if (!Number.isFinite(sessionId) || sessionId <= 0) throw new Error("Session không hợp lệ.");
    const now = new Date();
    const bookingWindowEnd = new Date(now.getTime() + 15 * 60 * 1000);
    const [session, summary, orders, products, tables, activeSessions, upcomingBookings] = await Promise.all([
      sessionApi.detail(sessionId),
      sessionApi.summary(sessionId),
      orderApi.bySession(sessionId),
      productApi.list(),
      venueApi.tables({ pageSize: 500 }),
      sessionApi.active(),
      bookingApi.calendar(now.toISOString(), bookingWindowEnd.toISOString(), { status: 2, pageNumber: 1, pageSize: 100 })
    ]);
    return { session, summary, orders, products, tables, activeSessions, upcomingBookings };
  }, [sessionId]);

  useEffect(() => {
    const timer = window.setInterval(() => setDurationTick((value) => value + 1), 1000);
    return () => window.clearInterval(timer);
  }, []);

  useEffect(() => {
    const cleanup = connectOperationHub({
      onSessionUpdated: (payload) => {
        if (!payload.sessionId || payload.sessionId === sessionId) reload();
      },
      onOrderUpdated: (payload) => {
        if (!payload.sessionId || payload.sessionId === sessionId) reload();
      },
      onBookingUpdated: () => reload(),
      onTableStatusChanged: () => reload(),
      onStatusChange: setRealtimeStatus
    });

    return cleanup;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sessionId]);

  const session = data?.session as Session | undefined;
  const summary = data?.summary as any;
  const orders = (data?.orders || []) as Order[];
  const products = useList<Product>(data?.products);
  const tables = useList<VenueTable>(data?.tables);
  const activeSessions = useList<Session>(data?.activeSessions);
  const upcomingBookings = useList<BookingCalendarItem>(data?.upcomingBookings);
  const currentOrder = selectedOrderId ? orders.find((order) => order.orderId === selectedOrderId) : orders[0] ?? null;
  const currentAssignment = useMemo(() => {
    const assignments = session?.assignments || [];
    return assignments.find((assignment) => !assignment.endedAtUtc) || assignments[assignments.length - 1];
  }, [session?.assignments]);
  const isOpen = Number(session?.status) === SESSION_OPEN;
  const isClosed = Number(session?.status) === SESSION_CLOSED;
  const canReopen = isClosed && Number(summary?.invoiceStatus ?? 0) !== 2;

  useEffect(() => {
    if (!isOpen) return;
    const timer = window.setInterval(() => {
      if (!document.hidden) {
        reload();
      }
    }, 45000);
    return () => window.clearInterval(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen, sessionId]);

  async function ensureOrder() {
    if (currentOrder) return currentOrder.orderId;
    if (!isOpen) throw new Error("Session đã đóng hoặc hủy, không thể thêm order.");
    const order = await orderApi.create(sessionId);
    setSelectedOrderId(order.orderId);
    await reload();
    return order.orderId;
  }

  async function addProduct(product: Product) {
    try {
      const orderId = await ensureOrder();
      const order = orders.find((item) => item.orderId === orderId);
      const existingItem = order?.items?.find((item) => item.productId === product.productId);
      if (existingItem) {
        await orderApi.updateItem(orderId, existingItem.orderItemId, existingItem.quantity + 1);
      } else {
        await orderApi.addItem(orderId, { productId: product.productId, quantity: 1 });
      }
      toast("Đã thêm sản phẩm vào order.", "success");
      await reload();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể thêm sản phẩm.", "error");
    }
  }

  async function closeSession() {
    if (!isOpen) return;
    setClosing(true);
    try {
      const result = await sessionApi.end(sessionId);
      const invoiceId = result?.invoiceId ?? result?.InvoiceId;
      toast("Đã kết thúc phiên và tạo hóa đơn.", "success");
      router.push(invoiceId ? `/operation/invoices?invoiceId=${invoiceId}` : "/operation/invoices");
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể kết thúc phiên.", "error");
    } finally {
      setClosing(false);
    }
  }

  const totalDuration = summary?.currentDurationMinutes ?? summary?.totalDurationMinutes ?? session?.durationMinutes ?? 0;
  const timeAmount = summary?.timeSubtotalAmount ?? 0;
  const productAmount = summary?.productSubtotalAmount ?? summary?.orderSubtotalAmount ?? 0;
  const discountAmount = summary?.discountAmount ?? 0;
  const grandTotal = summary?.grandTotalAmount ?? Math.max(0, Number(timeAmount) + Number(productAmount) - Number(discountAmount));

  return (
    <>
      <PageHeader
        title={session?.sessionCode || `Phiên #${sessionId}`}
        description="Workspace vận hành phiên chơi, order, tạm tính và kết thúc hóa đơn."
        action={<button className="ghost-btn" type="button" onClick={() => router.push("/operation/sessions")}>Danh sách phiên</button>}
      />
      <section style={{ padding: "0 24px 24px" }}>
        <StateBlock loading={loading} error={error} empty={!loading && !session} />
        {session ? (
          <div style={{ display: "grid", gap: 18 }}>
            <div className="panel">
              <div className="panel-head">
                <div>
                  <h3>Thông tin phiên</h3>
                  <p>{currentAssignment?.tableName || currentAssignment?.tableCode || "Bàn"} · bắt đầu {dateTime(session.startedAtUtc)}</p>
                  <p style={{ marginTop: 4, fontSize: 13, color: "var(--muted)" }}>
                    Thời lượng hiển thị realtime. Tiền giờ được backend tính theo bảng giá và quy tắc làm tròn.
                  </p>
                  <RealtimeStatusText status={realtimeStatus} />
                </div>
                <Badge tone={isOpen ? "green" : "neutral"}>{label(sessionStatus, Number(session.status))}</Badge>
              </div>
              <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(180px, 1fr))", gap: 12 }}>
                <Info label="Mã session" value={session.sessionCode || `#${session.sessionId}`} />
                <Info label="Bàn hiện tại" value={currentAssignment?.tableName || currentAssignment?.tableCode || "-"} />
                <Info label="Khách hàng" value={session.customerId ? `#${session.customerId}` : "Khách vãng lai"} />
                <Info label="Thời lượng thực tế" value={`Đã chơi: ${formatElapsedDuration(session.startedAtUtc, isOpen ? undefined : session.endedAtUtc)}`} />
              </div>
            </div>

            <div className="panel">
              <div className="panel-head">
                <div>
                  <h3>Tạm tính</h3>
                  <p>Backend tính tiền giờ theo bảng giá, minimum và block hiện hành.</p>
                </div>
                <button className="ghost-btn" type="button" onClick={async () => { await reload(); setSummaryOpen(true); }}>Xem chi tiết tạm tính</button>
              </div>
              <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(180px, 1fr))", gap: 12 }}>
                <Info label="Tiền giờ" value={money(Number(timeAmount))} />
                <Info label="Tiền order" value={money(Number(productAmount))} />
                <Info label="Giảm giá" value={`-${money(Number(discountAmount))}`} />
                <Info label="Tổng tiền" value={money(Number(grandTotal))} strong />
              </div>
            </div>

            <div className="section-grid">
              <div className="card">
                <h2>Thêm order</h2>
                {!isOpen ? <div className="inline-alert error">Session đã đóng hoặc hủy, không thể thêm order.</div> : null}
                <div className="floor-grid">
                  {products.map((product) => (
                    <button className="card" key={product.productId} type="button" disabled={!isOpen} onClick={() => addProduct(product)}>
                      <strong>{product.name}</strong>
                      <p>{money(product.unitPrice)} · Kho {product.stockQuantity}</p>
                    </button>
                  ))}
                </div>
              </div>

              <div className="card">
                <h2>Order của phiên</h2>
                <DataTable
                  rows={orders as unknown as Record<string, unknown>[]}
                  columns={[
                    { key: "orderCode", label: "Đơn hàng" },
                    { key: "status", label: "Trạng thái" },
                    { key: "subtotalAmount", label: "Tổng", render: (row) => money(Number(row.subtotalAmount || 0)) }
                  ]}
                  actions={(row) => <button className="ghost-btn" type="button" onClick={() => setSelectedOrderId(Number(row.orderId))}>Chọn</button>}
                />
                {currentOrder?.items?.length ? (
                  <div className="table-wrap" style={{ marginTop: 18 }}>
                    <table>
                      <thead>
                        <tr><th>Sản phẩm</th><th>Số lượng</th><th>Thành tiền</th></tr>
                      </thead>
                      <tbody>
                        {currentOrder.items.map((item) => (
                          <tr key={item.orderItemId}>
                            <td>{item.productNameSnapshot}</td>
                            <td>{item.quantity}</td>
                            <td>{money(item.lineTotalAmount || 0)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ) : <p>Chưa có sản phẩm trong order.</p>}
              </div>
            </div>

            <div className="panel">
              <div className="modal-actions">
                <button className="primary-btn" type="button" disabled={!isOpen} onClick={() => router.push(`/operation/orders?sessionId=${sessionId}&returnTo=${encodeURIComponent(`/operation/sessions/${sessionId}`)}`)}>Thêm order nâng cao</button>
                <button className="secondary-btn" type="button" disabled={!isOpen} onClick={() => setTransferOpen(true)}>Chuyển bàn</button>
                {canReopen ? <button className="secondary-btn" type="button" onClick={() => setReopenOpen(true)}>Khôi phục phiên</button> : null}
                <button className="ghost-btn" type="button" onClick={async () => { await reload(); setSummaryOpen(true); }}>Tạm tính</button>
                <button className="danger-btn" type="button" disabled={!isOpen || closing} onClick={closeSession}>{closing ? "Đang xử lý..." : "Kết thúc & tạo hóa đơn"}</button>
              </div>
            </div>
          </div>
        ) : null}
      </section>
      {summaryOpen && summary ? <Modal title="Chi tiết tạm tính" onClose={() => setSummaryOpen(false)} size="large">
        <div style={{ display: "grid", gap: 16 }}>
          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(180px, 1fr))", gap: 12 }}>
            <Info label="Tổng thực tế" value={`${summary.actualDurationMinutes ?? summary.timeCharge?.actualDurationMinutes ?? totalDuration} phút`} />
            <Info label="Tổng tính tiền" value={`${summary.billableDurationMinutes ?? summary.timeCharge?.billableDurationMinutes ?? totalDuration} phút`} />
            <Info label="Tiền giờ" value={money(Number(timeAmount))} strong />
          </div>
          <div className="inline-note">{summary.timeCharge?.note || "Minimum/block áp dụng một lần cho toàn phiên, không áp lại sau mỗi lần chuyển bàn."}</div>
          {(summary.timeCharge?.lines || summary.assignments || []).map((assignment: any) => (
            <div key={assignment.sessionTableAssignmentId} style={{ border: "1px solid var(--line)", borderRadius: 8, padding: 12 }}>
              <div style={{ display: "flex", justifyContent: "space-between", gap: 12, flexWrap: "wrap" }}>
                <strong>{assignment.tableName || assignment.tableCode}</strong>
                <strong>{assignment.isBillable === false ? "Miễn tính" : money(Number(assignment.amount || 0))}</strong>
              </div>
              <p style={{ margin: "6px 0", color: "var(--muted)" }}>
                {money(Number(assignment.hourlyRate || assignment.hourlyRateSnapshot || 0))}/giờ · thực tế {assignment.actualDurationMinutes ?? assignment.durationMinutes} phút · tính tiền {assignment.billableDurationMinutes ?? assignment.durationMinutes} phút
              </p>
              {assignment.note ? <div className="inline-note">{assignment.note}</div> : null}
            </div>
          ))}

          <div>
            <h3 style={{ margin: "4px 0 10px" }}>Order trong phiên</h3>
            {(summary.orders || []).length ? (
              <div style={{ display: "grid", gap: 12 }}>
                {(summary.orders || []).map((order: any) => (
                  <div key={order.orderId} style={{ border: "1px solid var(--line)", borderRadius: 8, padding: 12 }}>
                    <div style={{ display: "flex", justifyContent: "space-between", gap: 12, flexWrap: "wrap" }}>
                      <strong>{order.orderCode || `Order #${order.orderId}`}</strong>
                      <span>{money(Number(order.subtotalAmount || 0))}</span>
                    </div>
                    <p style={{ margin: "4px 0 10px", color: "var(--muted)" }}>Trạng thái #{order.status}</p>
                    <div className="table-wrap">
                      <table>
                        <thead><tr><th>Sản phẩm</th><th>Số lượng</th><th>Đơn giá</th><th>Thành tiền</th></tr></thead>
                        <tbody>
                          {(order.items || []).map((item: any) => (
                            <tr key={item.orderItemId || item.productName}>
                              <td>{item.productName || item.productNameSnapshot}</td>
                              <td>{item.quantity}</td>
                              <td>{money(Number(item.unitPrice || item.unitPriceSnapshot || 0))}</td>
                              <td>{money(Number(item.lineTotalAmount || 0))}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>
                ))}
              </div>
            ) : <div className="inline-note">Chưa có order sản phẩm/dịch vụ.</div>}
          </div>

          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(180px, 1fr))", gap: 12 }}>
            <Info label="Tiền giờ" value={money(Number(timeAmount))} />
            <Info label="Tiền order" value={money(Number(productAmount))} />
            <Info label="Giảm giá" value={`-${money(Number(discountAmount))}`} />
            <Info label="Tổng tạm tính" value={money(Number(grandTotal))} strong />
          </div>
        </div>
      </Modal> : null}
      {transferOpen && session ? <TransferTableModal
        sessionId={sessionId}
        currentTableId={Number(summary?.currentTable?.tableId || currentAssignment?.tableId || 0)}
        currentTableName={String(summary?.currentTable?.tableName || currentAssignment?.tableName || currentAssignment?.tableCode || "-")}
        tables={tables}
        activeSessions={activeSessions}
        upcomingBookings={upcomingBookings}
        onClose={() => setTransferOpen(false)}
        onTransferred={async () => {
          setTransferOpen(false);
          toast("Chuyển bàn thành công.", "success");
          await reload();
        }}
      /> : null}
      {reopenOpen ? <ReopenSessionModal
        onClose={() => setReopenOpen(false)}
        onReopen={async (reason) => {
          await sessionApi.reopen(sessionId, { reason, reopenLastTable: true });
          setReopenOpen(false);
          toast("Đã khôi phục phiên.", "success");
          await reload();
        }}
      /> : null}
    </>
  );
}

function Info({ label, value, strong = false }: { label: string; value: string; strong?: boolean }) {
  return (
    <div style={{ border: "1px solid var(--line)", borderRadius: 8, padding: "12px 14px", background: "var(--soft)" }}>
      <div style={{ color: "var(--muted)", fontSize: 13 }}>{label}</div>
      <div style={{ fontWeight: strong ? 800 : 700, marginTop: 4 }}>{value}</div>
    </div>
  );
}

function RealtimeStatusText({ status }: { status: OperationRealtimeStatus }) {
  if (status === "connected") return null;
  const text = status === "reconnecting" || status === "connecting"
    ? "Đang kết nối lại realtime..."
    : "Dữ liệu tự làm mới định kỳ.";

  return <p style={{ marginTop: 4, fontSize: 13, color: "var(--muted)" }}>{text}</p>;
}

function TransferTableModal({
  sessionId,
  currentTableId,
  currentTableName,
  tables,
  activeSessions,
  upcomingBookings,
  onClose,
  onTransferred
}: {
  sessionId: number;
  currentTableId: number;
  currentTableName: string;
  tables: VenueTable[];
  activeSessions: Session[];
  upcomingBookings: BookingCalendarItem[];
  onClose: () => void;
  onTransferred: () => Promise<void>;
}) {
  const toast = useToast();
  const [toTableId, setToTableId] = useState("");
  const [reason, setReason] = useState("CustomerRequest");
  const [note, setNote] = useState("");
  const [markOldTableMaintenance, setMarkOldTableMaintenance] = useState(false);
  const [saving, setSaving] = useState(false);

  const activeTableIds = new Set(activeSessions
    .map((session) => Number((session as any).currentTable?.tableId || session.tableId || 0))
    .filter((tableId) => tableId > 0 && tableId !== currentTableId));
  const bookingByTableId = new Map(upcomingBookings
    .filter((booking) => booking.tableId)
    .map((booking) => [Number(booking.tableId), booking]));

  function disabledReason(table: VenueTable) {
    if (table.tableId === currentTableId) return "Bàn hiện tại";
    if (table.isActive === false) return "Bàn ngưng hoạt động";
    if (Number(table.operationalStatus) === 4) return "Bảo trì";
    if (Number(table.operationalStatus) !== 1) return "Không khả dụng";
    if (activeTableIds.has(Number(table.tableId))) return "Đang có khách";
    const booking = bookingByTableId.get(Number(table.tableId));
    if (booking) return `Booking ${booking.bookingCode || booking.bookingId} lúc ${dateTime(booking.startTimeUtc)}`;
    return "";
  }

  async function transfer() {
    const targetId = Number(toTableId);
    if (!targetId) {
      toast("Chọn bàn cần chuyển đến.", "error");
      return;
    }

    setSaving(true);
    try {
      await sessionApi.transfer(sessionId, { toTableId: targetId, reason, note, markOldTableMaintenance });
      await onTransferred();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể chuyển bàn.", "error");
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal title="Chuyển bàn" onClose={onClose} size="medium">
      <div style={{ display: "grid", gap: 14 }}>
        <Info label="Bàn hiện tại" value={currentTableName} />
        <label>
          <span>Bàn chuyển đến</span>
          <select value={toTableId} onChange={(event) => setToTableId(event.target.value)}>
            <option value="">Chọn bàn trống</option>
            {tables.map((table) => {
              const reasonText = disabledReason(table);
              return (
                <option key={table.tableId} value={table.tableId} disabled={Boolean(reasonText)}>
                  {table.tableName || table.tableCode} {reasonText ? `- ${reasonText}` : ""}
                </option>
              );
            })}
          </select>
        </label>
        <label>
          <span>Lý do</span>
          <select value={reason} onChange={(event) => setReason(event.target.value)}>
            <option value="CustomerRequest">Khách yêu cầu</option>
            <option value="TableIssue">Bàn có sự cố</option>
            <option value="StaffCorrection">Nhân viên điều chỉnh</option>
            <option value="Other">Khác</option>
          </select>
        </label>
        <label>
          <span>Ghi chú</span>
          <textarea value={note} onChange={(event) => setNote(event.target.value)} rows={3} placeholder="Ghi chú thêm nếu có" />
        </label>
        <label style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <input type="checkbox" checked={markOldTableMaintenance} onChange={(event) => setMarkOldTableMaintenance(event.target.checked)} />
          <span>Đánh dấu bàn cũ cần bảo trì</span>
        </label>
        <div className="modal-actions">
          <button className="ghost-btn" type="button" onClick={onClose} disabled={saving}>Hủy</button>
          <button className="primary-btn" type="button" onClick={transfer} disabled={saving}>{saving ? "Đang xử lý..." : "Chuyển bàn"}</button>
        </div>
      </div>
    </Modal>
  );
}

function ReopenSessionModal({ onClose, onReopen }: {
  onClose: () => void;
  onReopen: (reason: string) => Promise<void>;
}) {
  const toast = useToast();
  const [reason, setReason] = useState("");
  const [saving, setSaving] = useState(false);

  async function reopen() {
    if (!reason.trim()) {
      toast("Nhập lý do khôi phục phiên.", "error");
      return;
    }

    setSaving(true);
    try {
      await onReopen(reason.trim());
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể khôi phục phiên.", "error");
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal title="Khôi phục phiên" onClose={onClose} size="small">
      <div style={{ display: "grid", gap: 14 }}>
        <div className="inline-note">Hóa đơn chưa thanh toán của phiên này sẽ bị hủy và tạo lại sau khi kết thúc phiên lần nữa.</div>
        <label>
          <span>Lý do khôi phục</span>
          <textarea value={reason} onChange={(event) => setReason(event.target.value)} rows={3} placeholder="Ví dụ: đóng nhầm, khách vẫn đang chơi" />
        </label>
        <div className="modal-actions">
          <button className="ghost-btn" type="button" onClick={onClose} disabled={saving}>Hủy</button>
          <button className="primary-btn" type="button" onClick={reopen} disabled={saving}>{saving ? "Đang xử lý..." : "Khôi phục phiên"}</button>
        </div>
      </div>
    </Modal>
  );
}
