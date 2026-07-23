"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { bookingApi, orderApi, productApi, sessionApi, venueApi } from "@/lib/api/endpoints";
import { dateTime, label, money, sessionStatus } from "@/lib/status";
import { formatElapsedDuration } from "@/lib/sessionDuration";
import { connectOperationHub, type OperationRealtimeStatus } from "@/lib/realtime/operationHub";
import { Badge, Modal, PageHeader, StateBlock, useList, useLoad } from "@/components/ui";
import { DepositRefundSummaryPanel } from "@/components/refunds/DepositRefundSummaryPanel";
import { useToast } from "@/components/toast";
import type { BookingCalendarItem, Order, Product, Session, SessionTableAssignment, VenueTable } from "@/types";

const SESSION_OPEN = 1;
const SESSION_CLOSED = 2;

const orderStatusText = (status?: number) => {
  if (status === 2) return "Hoàn thành";
  if (status === 3) return "Đã hủy";
  return "Mới";
};

export default function SessionDetailPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const toast = useToast();
  const sessionId = Number(params?.id);
  const [selectedOrderId, setSelectedOrderId] = useState<number | null>(null);
  const [detailItemPage, setDetailItemPage] = useState(1);
  const [closing, setClosing] = useState(false);
  const [releaseSaving, setReleaseSaving] = useState(false);
  const [selectedReleaseAssignmentIds, setSelectedReleaseAssignmentIds] = useState<number[]>([]);
  const [summaryOpen, setSummaryOpen] = useState(false);
  const [transferOpen, setTransferOpen] = useState(false);
  const [reopenOpen, setReopenOpen] = useState(false);
  const [realtimeStatus, setRealtimeStatus] = useState<OperationRealtimeStatus>("connecting");
  const [, setDurationTick] = useState(0);

  const { data, loading, error, reload } = useLoad(async () => {
    if (!Number.isFinite(sessionId) || sessionId <= 0) throw new Error("Phiên không hợp lệ.");
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
      onSessionUpdated: (payload) => {
        if (!payload.sessionId || payload.sessionId === sessionId) scheduleRealtimeReload();
      },
      onOrderUpdated: (payload) => {
        if (!payload.sessionId || payload.sessionId === sessionId) scheduleRealtimeReload();
      },
      onBookingUpdated: scheduleRealtimeReload,
      onTableStatusChanged: scheduleRealtimeReload,
      onStatusChange: setRealtimeStatus
    });

    return () => {
      cleanup();
      if (realtimeReloadTimerRef.current !== null) {
        window.clearTimeout(realtimeReloadTimerRef.current);
        realtimeReloadTimerRef.current = null;
      }
    };
  }, [scheduleRealtimeReload, sessionId]);

  const session = data?.session as Session | undefined;
  const summary = data?.summary as any;
  const orders = (data?.orders || []) as Order[];
  const products = useList<Product>(data?.products);
  const tables = useList<VenueTable>(data?.tables);
  const activeSessions = useList<Session>(data?.activeSessions);
  const upcomingBookings = useList<BookingCalendarItem>(data?.upcomingBookings);
  const currentOrder = (selectedOrderId ? orders.find((order) => order.orderId === selectedOrderId) : null) ?? orders[0] ?? null;
  const currentOrderItems = currentOrder?.items ?? [];
  const detailItemPageCount = Math.max(1, currentOrderItems.length);
  const activeDetailItemPage = Math.min(detailItemPage, detailItemPageCount);
  const activeDetailItem = currentOrderItems[activeDetailItemPage - 1] ?? null;
  const currentAssignment = useMemo(() => {
    const assignments = session?.assignments || [];
    return assignments.find((assignment) => !assignment.endedAtUtc) || assignments[assignments.length - 1];
  }, [session?.assignments]);
  const activeAssignments = useMemo(() => (session?.assignments || []).filter((assignment) => !assignment.endedAtUtc), [session?.assignments]);
  const isOpen = Number(session?.status) === SESSION_OPEN;
  const isClosed = Number(session?.status) === SESSION_CLOSED;
  const canReopen = isClosed && Number(summary?.invoiceStatus ?? 0) !== 2;

  useEffect(() => {
    setDetailItemPage(1);
  }, [currentOrder?.orderId]);

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
    if (!isOpen) throw new Error("Phiên đã đóng hoặc hủy, không thể thêm đơn hàng.");
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
      toast("Đã thêm sản phẩm vào đơn hàng.", "success");
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

  async function releaseSelectedTables() {
    if (!selectedReleaseAssignmentIds.length) {
      toast("Chọn ít nhất một bàn cần kết thúc.", "error");
      return;
    }

    setReleaseSaving(true);
    try {
      const result = await sessionApi.releaseTables(sessionId, { assignmentIds: selectedReleaseAssignmentIds });
      setSelectedReleaseAssignmentIds([]);
      toast(result?.wasSessionAutoClosed ? "Đã kết thúc bàn cuối và tạo hóa đơn." : "Đã kết thúc bàn đã chọn.", "success");
      await reload();
      if (result?.invoiceId) {
        router.push(`/operation/invoices?invoiceId=${result.invoiceId}`);
      }
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể kết thúc bàn.", "error");
    } finally {
      setReleaseSaving(false);
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
        description="Không gian vận hành phiên chơi, đơn hàng, tạm tính và kết thúc hóa đơn."
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
                <Info label="Mã phiên" value={session.sessionCode || `#${session.sessionId}`} />
                <Info label="Bàn hiện tại" value={currentAssignment?.tableName || currentAssignment?.tableCode || "-"} />
                <Info label="Khách hàng" value={session.customerId ? `#${session.customerId}` : "Khách vãng lai"} />
                <Info label="Thời lượng thực tế" value={`Đã chơi: ${formatElapsedDuration(session.startedAtUtc, isOpen ? undefined : session.endedAtUtc)}`} />
              </div>
            </div>

            <div className="panel">
              <div className="panel-head">
                <div>
                  <h3>Tạm tính</h3>
                  <p>Hệ thống tính tiền giờ theo bảng giá, thời gian tối thiểu và khung làm tròn hiện hành.</p>
                </div>
                <button className="ghost-btn" type="button" onClick={async () => { await reload(); setSummaryOpen(true); }}>Xem chi tiết tạm tính</button>
              </div>
              <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(180px, 1fr))", gap: 12 }}>
                <Info label="Tiền giờ" value={money(Number(timeAmount))} />
                <Info label="Tiền đơn hàng" value={money(Number(productAmount))} />
                <Info label="Giảm giá" value={`-${money(Number(discountAmount))}`} />
                <Info label="Tổng tiền" value={money(Number(grandTotal))} strong />
              </div>
              <div style={{ marginTop: 14 }}>
                <DepositRefundSummaryPanel summary={summary?.depositRefundSummary || session.depositRefundSummary} />
              </div>
            </div>

            {isOpen ? (
              <div className="panel">
                <div className="panel-head">
                  <div>
                    <h3>Bàn đang chơi</h3>
                    <p>Chọn một hoặc nhiều bàn để kết thúc trong phiên. Bàn cuối sẽ tự động đóng phiên và tạo hóa đơn.</p>
                  </div>
                  <button className="secondary-btn" type="button" disabled={releaseSaving || selectedReleaseAssignmentIds.length === 0} onClick={releaseSelectedTables}>
                    {releaseSaving ? "Đang xử lý..." : "Kết thúc bàn đã chọn"}
                  </button>
                </div>
                <div style={{ display: "grid", gap: 10 }}>
                  {activeAssignments.map((assignment) => {
                    const assignmentId = Number(assignment.sessionTableAssignmentId || assignment.assignmentId || 0);
                    return (
                      <label key={assignmentId} style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12, border: "1px solid var(--line)", borderRadius: 8, padding: 12 }}>
                        <span style={{ display: "flex", alignItems: "center", gap: 10 }}>
                          <input
                            type="checkbox"
                            style={{ width: 22, height: 22, cursor: "pointer", accentColor: "#0f766e" }}
                            checked={selectedReleaseAssignmentIds.includes(assignmentId)}
                            onChange={(event) => setSelectedReleaseAssignmentIds((current) =>
                              event.target.checked ? [...current, assignmentId] : current.filter((id) => id !== assignmentId))}
                          />
                          <strong>{assignment.tableName || assignment.tableCode || `Bàn #${assignment.tableId}`}</strong>
                        </span>
                        <span style={{ color: "var(--muted)", fontSize: 13 }}>Bắt đầu {dateTime(assignment.startedAtUtc)}</span>
                      </label>
                    );
                  })}
                  {!activeAssignments.length ? <div className="inline-note">Không còn bàn đang hoạt động trong phiên.</div> : null}
                </div>
              </div>
            ) : null}

            <div className="section-grid orders-layout">
              <div className="card">
                <h2>Sản phẩm</h2>
                {!isOpen ? <div className="inline-alert error">Phiên đã đóng hoặc hủy, không thể thêm đơn hàng.</div> : null}
                <div className="floor-grid">
                  {products.map((product) => (
                    <button className="card" key={product.productId} type="button" disabled={!isOpen} onClick={() => addProduct(product)}>
                      <strong>{product.name}</strong>
                      <p>{money(product.unitPrice)} · Kho {product.stockQuantity}</p>
                    </button>
                  ))}
                </div>
              </div>

              <div className="card orders-card">
                <div className="orders-card-head">
                  <div>
                    <h2>Đơn hàng của phiên</h2>
                    <div style={{ color: "var(--muted)", marginTop: 4 }}>
                      {currentOrder ? `Đã chọn: ${currentOrder.orderCode || `Đơn #${currentOrder.orderId}`}` : "Chưa chọn đơn hàng."}
                    </div>
                  </div>
                </div>

                <div className="order-list" aria-label="Danh sách đơn hàng của phiên">
                  {orders.length ? orders.map((order) => {
                    const selected = currentOrder?.orderId === order.orderId;
                    return (
                      <article className={`order-list-item${selected ? " is-selected" : ""}`} key={order.orderId}>
                        <button
                          className="order-list-select"
                          type="button"
                          aria-pressed={selected}
                          onClick={() => setSelectedOrderId(order.orderId)}
                        >
                          <span className="order-list-code">
                            <strong>{order.orderCode || `Đơn #${order.orderId}`}</strong>
                            {selected ? <span className="badge blue">Đang chọn</span> : null}
                          </span>
                          <span className="order-list-meta">
                            <span>
                              <small>Trạng thái</small>
                              <strong>{orderStatusText(Number(order.status))}</strong>
                            </span>
                            <span>
                              <small>Tổng tiền</small>
                              <strong>{money(Number(order.subtotalAmount || 0))}</strong>
                            </span>
                          </span>
                        </button>
                        <div className="order-list-actions">
                          <button
                            className={selected ? "primary-btn" : "ghost-btn"}
                            type="button"
                            onClick={() => setSelectedOrderId(order.orderId)}
                          >
                            {selected ? "Đang chọn" : "Chọn"}
                          </button>
                        </div>
                      </article>
                    );
                  }) : <p className="order-list-empty">Chưa có đơn hàng trong phiên này.</p>}
                </div>

                {currentOrder ? (
                  <section className="order-detail-section">
                    <div className="order-detail-heading">
                      <div>
                        <h3>Chi tiết đơn hàng</h3>
                        <p>{currentOrder.orderCode || `Đơn #${currentOrder.orderId}`}</p>
                      </div>
                      <strong>{money(Number(currentOrder.subtotalAmount || 0))}</strong>
                    </div>
                    {currentOrderItems.length ? (
                      <>
                        <div className="order-detail-controls">
                          <label className="order-detail-picker">
                            <span>Sản phẩm trong đơn</span>
                            <select
                              value={activeDetailItem ? String(activeDetailItem.orderItemId) : ""}
                              onChange={(event) => {
                                const selectedIndex = currentOrderItems.findIndex(
                                  (item) => item.orderItemId === Number(event.target.value)
                                );
                                if (selectedIndex >= 0) setDetailItemPage(selectedIndex + 1);
                              }}
                            >
                              {currentOrderItems.map((item, index) => (
                                <option key={item.orderItemId} value={item.orderItemId}>
                                  {index + 1}. {item.productNameSnapshot} · SL {item.quantity}
                                </option>
                              ))}
                            </select>
                          </label>
                          <nav className="order-detail-pagination" aria-label="Phân trang sản phẩm của đơn hàng">
                            <button
                              className="ghost-btn"
                              type="button"
                              disabled={activeDetailItemPage <= 1}
                              onClick={() => setDetailItemPage(activeDetailItemPage - 1)}
                            >
                              Trước
                            </button>
                            <span>
                              Sản phẩm {activeDetailItemPage}/{currentOrderItems.length}
                            </span>
                            <button
                              className="ghost-btn"
                              type="button"
                              disabled={activeDetailItemPage >= currentOrderItems.length}
                              onClick={() => setDetailItemPage(activeDetailItemPage + 1)}
                            >
                              Sau
                            </button>
                          </nav>
                        </div>
                        <div className="order-detail-list">
                          {currentOrderItems.slice(activeDetailItemPage - 1, activeDetailItemPage).map((item) => (
                            <article className="order-detail-item" key={item.orderItemId}>
                              <div className="order-detail-cell order-detail-product">
                                <small>Sản phẩm</small>
                                <strong>{item.productNameSnapshot}</strong>
                              </div>
                              <div className="order-detail-cell">
                                <small>Đơn giá</small>
                                <span>{money(item.unitPriceSnapshot ?? 0)}</span>
                              </div>
                              <div className="order-detail-cell">
                                <small>Số lượng</small>
                                <strong>{item.quantity}</strong>
                              </div>
                              <div className="order-detail-cell">
                                <small>Thành tiền</small>
                                <strong>{money(item.lineTotalAmount ?? 0)}</strong>
                              </div>
                            </article>
                          ))}
                        </div>
                      </>
                    ) : <p className="order-list-empty">Chưa có sản phẩm trong đơn hàng.</p>}
                  </section>
                ) : null}
              </div>
            </div>

            <div className="panel">
              <div className="modal-actions">
                <button className="primary-btn" type="button" disabled={!isOpen} onClick={() => router.push(`/operation/orders?sessionId=${sessionId}&returnTo=${encodeURIComponent(`/operation/sessions/${sessionId}`)}`)}>Thêm đơn hàng nâng cao</button>
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
          <div className="inline-note">{summary.timeCharge?.note || "Thời gian tối thiểu/khung làm tròn áp dụng một lần cho toàn phiên, không áp lại sau mỗi lần chuyển bàn."}</div>
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
            <h3 style={{ margin: "4px 0 10px" }}>Đơn hàng trong phiên</h3>
            {(summary.orders || []).length ? (
              <div style={{ display: "grid", gap: 12 }}>
                {(summary.orders || []).map((order: any) => (
                  <div key={order.orderId} style={{ border: "1px solid var(--line)", borderRadius: 8, padding: 12 }}>
                    <div style={{ display: "flex", justifyContent: "space-between", gap: 12, flexWrap: "wrap" }}>
                      <strong>{order.orderCode || `Đơn hàng #${order.orderId}`}</strong>
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
            ) : <div className="inline-note">Chưa có đơn hàng sản phẩm/dịch vụ.</div>}
          </div>

          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(180px, 1fr))", gap: 12 }}>
            <Info label="Tiền giờ" value={money(Number(timeAmount))} />
            <Info label="Tiền đơn hàng" value={money(Number(productAmount))} />
            <Info label="Giảm giá" value={`-${money(Number(discountAmount))}`} />
            <Info label="Tổng tạm tính" value={money(Number(grandTotal))} strong />
          </div>
        </div>
      </Modal> : null}
      {transferOpen && session ? <TransferTableModal
        sessionId={sessionId}
        activeAssignments={activeAssignments}
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
  activeAssignments,
  tables,
  activeSessions,
  upcomingBookings,
  onClose,
  onTransferred
}: {
  sessionId: number;
  activeAssignments: SessionTableAssignment[];
  tables: VenueTable[];
  activeSessions: Session[];
  upcomingBookings: BookingCalendarItem[];
  onClose: () => void;
  onTransferred: () => Promise<void>;
}) {
  const toast = useToast();
  const [selectedTransfers, setSelectedTransfers] = useState<Record<number, string>>({});
  const [reason, setReason] = useState("CustomerRequest");
  const [note, setNote] = useState("");
  const [markOldTableMaintenance, setMarkOldTableMaintenance] = useState(false);
  const [saving, setSaving] = useState(false);

  const currentSessionActiveTableIds = new Set(activeAssignments
    .map((assignment) => Number(assignment.tableId))
    .filter((tableId) => tableId > 0));
  const activeTableIds = new Set([
    ...activeSessions.flatMap((session) => [
      ...((session.activeAssignments || []).map((assignment) => Number(assignment.tableId))),
      Number((session as any).currentTable?.tableId || session.tableId || 0)
    ]),
    ...currentSessionActiveTableIds
  ].filter((tableId) => tableId > 0));
  const bookingByTableId = new Map(upcomingBookings
    .filter((booking) => booking.tableId)
    .map((booking) => [Number(booking.tableId), booking]));
  const selectedTargetIds = new Set(Object.values(selectedTransfers).map(Number).filter((tableId) => tableId > 0));

  function getAssignmentId(assignment: SessionTableAssignment) {
    return Number(assignment.sessionTableAssignmentId || assignment.assignmentId || 0);
  }

  function toggleTransfer(assignment: SessionTableAssignment, checked: boolean) {
    const assignmentId = getAssignmentId(assignment);
    setSelectedTransfers((current) => {
      const next = { ...current };
      if (checked) {
        next[assignmentId] = next[assignmentId] || "";
      } else {
        delete next[assignmentId];
      }
      return next;
    });
  }

  function setTransferTarget(assignmentId: number, tableId: string) {
    setSelectedTransfers((current) => ({ ...current, [assignmentId]: tableId }));
  }

  function disabledReason(table: VenueTable, currentTargetId: number) {
    if (table.isActive === false) return "Bàn ngưng hoạt động";
    if (Number(table.operationalStatus) === 4) return "Bảo trì";
    if (Number(table.operationalStatus) !== 1) return "Không khả dụng";
    if (currentSessionActiveTableIds.has(Number(table.tableId))) return "Bàn đang trong phiên này";
    if (activeTableIds.has(Number(table.tableId))) return "Đang có khách";
    if (selectedTargetIds.has(Number(table.tableId)) && Number(table.tableId) !== currentTargetId) return "Đã chọn cho bàn khác";
    const booking = bookingByTableId.get(Number(table.tableId));
    if (booking) return `Booking ${booking.bookingCode || booking.bookingId} lúc ${dateTime(booking.startTimeUtc)}`;
    return "";
  }

  async function transfer() {
    const transfers = Object.entries(selectedTransfers)
      .map(([sourceAssignmentId, toTableId]) => ({ sourceAssignmentId: Number(sourceAssignmentId), toTableId: Number(toTableId) }))
      .filter((item) => item.sourceAssignmentId > 0);
    if (!transfers.length) {
      toast("Chọn ít nhất một bàn cần chuyển.", "error");
      return;
    }
    if (transfers.some((item) => item.toTableId <= 0)) {
      toast("Chọn bàn chuyển đến cho từng bàn cần chuyển.", "error");
      return;
    }

    setSaving(true);
    try {
      for (const item of transfers) {
        await sessionApi.transfer(sessionId, { ...item, reason, note, markOldTableMaintenance });
      }
      await onTransferred();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể chuyển bàn.", "error");
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal title="Chuyển bàn" onClose={onClose} size="large">
      <div style={{ display: "grid", gap: 14 }}>
        <div className="inline-note">Chọn một hoặc nhiều bàn trong phiên, sau đó chọn bàn trống tương ứng để chuyển đến.</div>
        <div style={{ display: "grid", gap: 10 }}>
          {activeAssignments.map((assignment) => {
            const assignmentId = getAssignmentId(assignment);
            const selected = Object.prototype.hasOwnProperty.call(selectedTransfers, assignmentId);
            const currentTargetId = Number(selectedTransfers[assignmentId] || 0);
            return (
              <div key={assignmentId} style={{ border: "1px solid var(--line)", borderRadius: 8, padding: 12, display: "grid", gap: 10 }}>
                <label style={{ display: "flex", alignItems: "center", gap: 10, fontWeight: 700 }}>
                  <input type="checkbox" style={{ width: 24, height: 24, cursor: "pointer", accentColor: "#0f766e" }} checked={selected} onChange={(event) => toggleTransfer(assignment, event.target.checked)} />
                  <span>{assignment.tableName || assignment.tableCode || `Bàn #${assignment.tableId}`}</span>
                </label>
                <label style={{ display: "grid", gap: 6 }}>
                  <span>Bàn chuyển đến</span>
                  <select value={selectedTransfers[assignmentId] || ""} disabled={!selected} onChange={(event) => setTransferTarget(assignmentId, event.target.value)}>
                    <option value="">Chọn bàn trống</option>
                    {tables.map((table) => {
                      const reasonText = disabledReason(table, currentTargetId);
                      return (
                        <option key={table.tableId} value={table.tableId} disabled={Boolean(reasonText)}>
                          {table.tableName || table.tableCode} {reasonText ? `- ${reasonText}` : ""}
                        </option>
                      );
                    })}
                  </select>
                </label>
              </div>
            );
          })}
        </div>
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
          <input type="checkbox" style={{ width: 22, height: 22, cursor: "pointer", accentColor: "#0f766e" }} checked={markOldTableMaintenance} onChange={(event) => setMarkOldTableMaintenance(event.target.checked)} />
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
