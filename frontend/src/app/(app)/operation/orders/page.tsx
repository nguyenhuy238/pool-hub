"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { orderApi, productApi, sessionApi } from "@/lib/api/endpoints";
import { dateTime, label, money, sessionStatus } from "@/lib/status";
import { Badge, ConfirmDialog, Modal, PageHeader, SearchableSelect, StateBlock, useList, useLoad } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { Order, OrderItem, Product, Session } from "@/types";

const SESSION_OPEN = 1;

const statusText = (status?: number) => {
  if (status === 2) return "Hoàn thành";
  if (status === 3) return "Đã hủy";
  return "Mới";
};

export default function OrdersPage() {
  const router = useRouter();
  const toast = useToast();
  const [sessionId, setSessionId] = useState<number | null>(null);
  const [returnTo, setReturnTo] = useState("");
  const [selectedOrderId, setSelectedOrderId] = useState<number | null>(null);
  const [cancellingOrder, setCancellingOrder] = useState<Order | null>(null);
  const [removingItem, setRemovingItem] = useState<OrderItem | null>(null);
  const [itemQuantities, setItemQuantities] = useState<Record<number, number>>({});
  const [detailItemPage, setDetailItemPage] = useState(1);
  const [summary, setSummary] = useState<any | null>(null);
  const [summaryOpen, setSummaryOpen] = useState(false);
  const [closeConfirmOpen, setCloseConfirmOpen] = useState(false);
  const [busyAction, setBusyAction] = useState<"summary" | "close" | null>(null);
  const { data, loading, error, reload } = useLoad(async () => {
    const [sessions, products, orders, session, summaryResult] = await Promise.all([
      sessionApi.list({ Status: 1, PageSize: 100 }),
      productApi.list(),
      sessionId ? orderApi.bySession(sessionId) : Promise.resolve([]),
      sessionId ? sessionApi.detail(sessionId).catch(() => null) : Promise.resolve(null),
      sessionId ? sessionApi.summary(sessionId).catch(() => null) : Promise.resolve(null)
    ]);
    return { sessions, products, orders, session, summary: summaryResult };
  }, [sessionId]);

  const sessions = useList<Session>(data?.sessions);
  const activeSessions = sessions.filter((s) => Number(s.status) === 1);
  const sessionOptions = activeSessions.map((item) => {
    const rawTable = item.tableName?.trim() || "";
    const tableStr = rawTable ? (/^(bàn|table)/i.test(rawTable) ? rawTable : `Bàn ${rawTable}`) : "Bàn";
    const codeStr = item.sessionCode || `#${item.sessionId}`;
    return {
      value: String(item.sessionId),
      label: `${tableStr} - ${codeStr} - Đang chơi ${item.durationMinutes ?? 0} phút`
    };
  });
  const products = useList<Product>(data?.products);
  const orders = data?.orders || [] as Order[];
  const selectedSession = (data?.session as Session | null) || activeSessions.find((session) => session.sessionId === sessionId) || null;
  const selectedSummary = summary || data?.summary;
  const isSessionOpen = Number(selectedSession?.status) === SESSION_OPEN;
  const canEditOrder = Boolean(sessionId && selectedSession && isSessionOpen);
  const currentOrder = (selectedOrderId ? orders.find((order) => order.orderId === selectedOrderId) : null) ?? orders[0] ?? null;
  const currentOrderItems = currentOrder?.items ?? [];
  const detailItemPageCount = Math.max(1, currentOrderItems.length);
  const activeDetailItemPage = Math.min(detailItemPage, detailItemPageCount);
  const activeDetailItem = currentOrderItems[activeDetailItemPage - 1] ?? null;
  const currentTable = (selectedSession?.assignments || []).find((assignment) => !assignment.endedAtUtc) || selectedSession?.assignments?.at?.(-1);

  useEffect(() => {
    const query = new URLSearchParams(window.location.search);
    const initialSessionId = Number(query.get("sessionId") || 0) || null;
    if (initialSessionId) setSessionId(initialSessionId);
    setReturnTo(query.get("returnTo") || "");
  }, []);

  useEffect(() => {
    setSelectedOrderId(null);
    setItemQuantities({});
    setSummary(null);
    setSummaryOpen(false);
    setCloseConfirmOpen(false);
  }, [sessionId]);

  useEffect(() => {
    setItemQuantities({});
    setDetailItemPage(1);
  }, [currentOrder?.orderId]);

  async function ensureOrder() {
    if (currentOrder) return currentOrder.orderId;
    if (!sessionId) throw new Error("Chọn session trước.");
    if (!canEditOrder) throw new Error("Phiên đã đóng, không thể thêm order.");
    const order = await orderApi.create(sessionId);
    setSelectedOrderId(order.orderId);
    return order.orderId;
  }

  async function addProduct(product: Product) {
    try {
      const orderId = await ensureOrder();
      const existingItem = currentOrder?.items?.find((item) => item.productId === product.productId);
      if (existingItem) {
        await orderApi.updateItem(orderId, existingItem.orderItemId, existingItem.quantity + 1);
      } else {
        await orderApi.addItem(orderId, { productId: product.productId, quantity: 1 });
      }
      toast("Đã thêm vào đơn hàng.", "success");
      await reload();
      if (sessionId) {
        setSummary(await sessionApi.summary(sessionId).catch(() => null));
      }
    } catch (err) {
      toast(err instanceof Error ? err.message : "Thao tác thất bại.", "error");
    }
  }

  function sessionReturnPath() {
    if (!sessionId) return returnTo || "/operation/sessions";
    if (returnTo && returnTo.includes(`/operation/sessions/${sessionId}`)) return returnTo;
    return `/operation/sessions/${sessionId}`;
  }

  async function openSummary() {
    if (!sessionId) {
      toast("Vui lòng chọn phiên chơi.", "error");
      return;
    }

    setBusyAction("summary");
    try {
      const result = await sessionApi.summary(sessionId);
      setSummary(result);
      setSummaryOpen(true);
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không tải được tạm tính.", "error");
    } finally {
      setBusyAction(null);
    }
  }

  async function openCloseConfirm() {
    if (!sessionId || !isSessionOpen) {
      toast("Phiên đã đóng hoặc chưa chọn phiên.", "error");
      return;
    }

    setBusyAction("summary");
    try {
      const result = await sessionApi.summary(sessionId);
      setSummary(result);
      setCloseConfirmOpen(true);
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không tải được tạm tính.", "error");
    } finally {
      setBusyAction(null);
    }
  }

  async function closeSession() {
    if (!sessionId || !isSessionOpen) return;

    setBusyAction("close");
    try {
      const result = await sessionApi.end(sessionId);
      const invoiceId = result?.invoiceId ?? result?.InvoiceId;
      toast("Đã kết thúc phiên và tạo hóa đơn.", "success");
      router.push(invoiceId ? `/operation/invoices?invoiceId=${invoiceId}` : "/operation/invoices");
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể kết thúc phiên.", "error");
    } finally {
      setBusyAction(null);
    }
  }

  async function updateItem(item: OrderItem, quantity: number) {
    try {
      if (!currentOrder) return;
      await orderApi.updateItem(currentOrder.orderId, item.orderItemId, quantity);
      toast("Cập nhật số lượng thành công.", "success");
      await reload();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Thao tác thất bại.", "error");
    }
  }

  async function deleteItem(item: OrderItem) {
    try {
      if (!currentOrder) return;
      await orderApi.deleteItem(currentOrder.orderId, item.orderItemId);
      toast("Đã xóa sản phẩm khỏi order.", "success");
      await reload();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Thao tác thất bại.", "error");
    }
  }

  async function cancelOrder(order: Order) {
    try {
      await orderApi.cancel(order.orderId);
      if (selectedOrderId === order.orderId) setSelectedOrderId(null);
      setCancellingOrder(null);
      toast("Đã hủy order.", "success");
      await reload();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể hủy order.", "error");
    }
  }

  return (
    <>
      <PageHeader
        title="Order POS"
        description="Tạo đơn hàng theo phiên chơi và thêm sản phẩm."
        action={
          <div className="orders-session-picker">
            {sessionId ? <button className="ghost-btn" type="button" onClick={() => router.push(sessionReturnPath())}>Quay lại phiên</button> : null}
            <SearchableSelect
              options={sessionOptions}
              value={sessionId ? String(sessionId) : ""}
              onChange={(val) => setSessionId(val ? Number(val) : null)}
              placeholder="Chọn hoặc tìm kiếm phiên chơi..."
            />
          </div>
        }
      />
      <StateBlock loading={loading} error={error} />
      {selectedSession ? (
        <section style={{ padding: "0 24px 18px" }}>
          <div className="panel">
            <div className="panel-head">
              <div>
                <h3>{selectedSession.sessionCode || `Phiên #${selectedSession.sessionId}`}</h3>
                <p>{currentTable?.tableName || currentTable?.tableCode || "Bàn"} · bắt đầu {dateTime(selectedSession.startedAtUtc)} · đang chơi {selectedSession.durationMinutes ?? selectedSummary?.currentDurationMinutes ?? 0} phút</p>
              </div>
              <Badge tone={isSessionOpen ? "green" : "neutral"}>{label(sessionStatus, Number(selectedSession.status))}</Badge>
            </div>
            {!isSessionOpen ? <div className="inline-alert error">Phiên đã đóng, không thể thêm order. Bạn chỉ có thể xem đơn hàng hiện có.</div> : null}
          </div>
        </section>
      ) : sessionId && !loading ? (
        <section style={{ padding: "0 24px 18px" }}>
          <div className="inline-alert error">Không tải được thông tin phiên. Vui lòng chọn phiên khác hoặc quay lại danh sách phiên.</div>
        </section>
      ) : null}
      <div className="section-grid orders-layout">
        <div className="card">
          <h2>Sản phẩm</h2>
          <div className="floor-grid">
            {products.map((product) => (
              <button
                className="card"
                key={product.productId}
                type="button"
                disabled={!canEditOrder || busyAction !== null}
                onClick={() => addProduct(product)}
              >
                <strong>{product.name}</strong>
                <p>{money(product.unitPrice)} · Kho {product.stockQuantity}</p>
              </button>
            ))}
          </div>
        </div>

        <div className="card orders-card">
          <div className="orders-card-head">
            <div>
              <h2>Đơn hàng</h2>
              <div style={{ color: "#555", marginTop: 4 }}>
                {currentOrder ? `Đã chọn: ${currentOrder.orderCode}` : "Chưa chọn đơn hàng."}
              </div>
            </div>
            <button
              className="primary-btn"
              type="button"
              disabled={!canEditOrder || busyAction !== null}
              onClick={async () => {
                try {
                  const orderId = await ensureOrder();
                  setSelectedOrderId(orderId);
                  toast("Order đã sẵn sàng.", "success");
                  await reload();
                } catch (err) {
                  toast(err instanceof Error ? err.message : "Thao tác thất bại.", "error");
                }
              }}
            >
              Tạo / Chọn đơn hàng
            </button>
          </div>

          <div className="order-list" aria-label="Danh sách đơn hàng">
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
                        <strong>{statusText(Number(order.status))}</strong>
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
                    <button className="danger-btn" type="button" disabled={!canEditOrder} onClick={() => setCancellingOrder(order)}>Hủy</button>
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
              {currentOrder.items?.length ? (
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
                    <nav className="order-detail-pagination" aria-label="Phân trang chi tiết đơn hàng">
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
                    {currentOrderItems.slice(activeDetailItemPage - 1, activeDetailItemPage).map((item) => {
                      const quantity = itemQuantities[item.orderItemId] ?? item.quantity;
                      return (
                        <article className="order-detail-item" key={item.orderItemId}>
                        <div className="order-detail-cell order-detail-product">
                          <small>Sản phẩm</small>
                          <strong>{item.productNameSnapshot}</strong>
                        </div>
                        <div className="order-detail-cell">
                          <small>Đơn giá</small>
                          <span>{money(item.unitPriceSnapshot ?? 0)}</span>
                        </div>
                        <label className="order-detail-quantity">
                          <span>Số lượng</span>
                          <input
                            type="number"
                            min={1}
                            value={quantity}
                            disabled={!canEditOrder}
                            onChange={(event) => setItemQuantities((current) => ({
                              ...current,
                              [item.orderItemId]: Number(event.target.value)
                            }))}
                          />
                        </label>
                        <div className="order-detail-cell">
                          <small>Thành tiền</small>
                          <strong>{money(item.lineTotalAmount ?? 0)}</strong>
                        </div>
                        <div className="order-detail-actions">
                          <button
                            className="ghost-btn"
                            type="button"
                            disabled={!canEditOrder}
                            onClick={() => updateItem(item, quantity)}
                          >
                            Cập nhật
                          </button>
                          <button className="danger-btn" type="button" disabled={!canEditOrder} onClick={() => setRemovingItem(item)}>
                            Xóa
                          </button>
                        </div>
                        </article>
                      );
                    })}
                  </div>
                </>
              ) : (
                <p>Chưa có sản phẩm trong order.</p>
              )}
            </section>
          ) : null}

          <section style={{ marginTop: 24, borderTop: "1px solid var(--line)", paddingTop: 18 }}>
            <h3>Bước tiếp theo</h3>
            <div style={{ display: "grid", gap: 10 }}>
              <button className="ghost-btn" type="button" disabled={!sessionId} onClick={() => router.push(sessionReturnPath())}>Quay lại phiên</button>
              <button className="ghost-btn" type="button" disabled={!sessionId || busyAction !== null} onClick={openSummary}>{busyAction === "summary" ? "Đang tải..." : "Tạm tính"}</button>
              <button className="danger-btn" type="button" disabled={!isSessionOpen || busyAction !== null} onClick={openCloseConfirm}>{busyAction === "close" ? "Đang xử lý..." : "Kết thúc & tạo hóa đơn"}</button>
              <button className="primary-btn" type="button" disabled={!isSessionOpen} onClick={() => toast("Tiếp tục chọn sản phẩm ở panel bên trái.", "success")}>Tiếp tục thêm món</button>
            </div>
          </section>
        </div>
      </div>
      {cancellingOrder ? <ConfirmDialog title="Hủy đơn hàng" message={`Xác nhận hủy đơn hàng ${cancellingOrder.orderCode || cancellingOrder.orderId}?`} confirmLabel="Hủy đơn hàng" danger onCancel={() => setCancellingOrder(null)} onConfirm={() => cancelOrder(cancellingOrder)} /> : null}
      {removingItem ? <ConfirmDialog title="Xóa sản phẩm khỏi đơn hàng" message={`Xóa “${removingItem.productNameSnapshot || removingItem.productId}” khỏi đơn hàng?`} confirmLabel="Xóa" danger onCancel={() => setRemovingItem(null)} onConfirm={async () => { await deleteItem(removingItem); setRemovingItem(null); }} /> : null}
      {summaryOpen && selectedSummary ? <OrderSummaryModal summary={selectedSummary} onClose={() => setSummaryOpen(false)} /> : null}
      {closeConfirmOpen && selectedSummary ? <CloseSessionModal summary={selectedSummary} busy={busyAction === "close"} onCancel={() => setCloseConfirmOpen(false)} onConfirm={closeSession} /> : null}
    </>
  );
}

function OrderSummaryModal({ summary, onClose }: { summary: any; onClose: () => void }) {
  const timeAmount = summary?.timeSubtotalAmount ?? 0;
  const productAmount = summary?.productSubtotalAmount ?? summary?.orderSubtotalAmount ?? 0;
  const discountAmount = summary?.discountAmount ?? 0;
  const total = summary?.grandTotalAmount ?? Math.max(0, Number(timeAmount) + Number(productAmount) - Number(discountAmount));

  return (
    <Modal title="Tạm tính phiên chơi" onClose={onClose} size="large">
      <div style={{ display: "grid", gap: 12 }}>
        <SummaryLine label="Tiền giờ" value={money(Number(timeAmount))} />
        <SummaryLine label="Tiền order" value={money(Number(productAmount))} />
        <SummaryLine label="Giảm giá" value={`-${money(Number(discountAmount))}`} />
        <SummaryLine label="Tổng tiền" value={money(Number(total))} strong />
        {(summary?.assignments || []).map((assignment: any) => (
          <div key={assignment.sessionTableAssignmentId} style={{ border: "1px solid var(--line)", borderRadius: 8, padding: 12 }}>
            <strong>{assignment.tableName || assignment.tableCode || "Bàn"}</strong>
            <p style={{ margin: "6px 0 0", color: "var(--muted)" }}>
              {money(Number(assignment.hourlyRate || assignment.hourlyRateSnapshot || 0))}/giờ · thực tế {assignment.actualDurationMinutes ?? assignment.durationMinutes} phút · tính tiền {assignment.billableDurationMinutes ?? assignment.durationMinutes} phút
            </p>
          </div>
        ))}
      </div>
    </Modal>
  );
}

function CloseSessionModal({ summary, busy, onCancel, onConfirm }: { summary: any; busy: boolean; onCancel: () => void; onConfirm: () => void }) {
  const timeAmount = summary?.timeSubtotalAmount ?? 0;
  const productAmount = summary?.productSubtotalAmount ?? summary?.orderSubtotalAmount ?? 0;
  const discountAmount = summary?.discountAmount ?? 0;
  const total = summary?.grandTotalAmount ?? Math.max(0, Number(timeAmount) + Number(productAmount) - Number(discountAmount));

  return (
    <Modal title="Kết thúc & tạo hóa đơn" onClose={onCancel} size="small">
      <div style={{ display: "grid", gap: 12 }}>
        <p className="modal-message" style={{ margin: 0 }}>Xác nhận kết thúc phiên và tạo hóa đơn thanh toán?</p>
        <SummaryLine label="Tiền giờ" value={money(Number(timeAmount))} />
        <SummaryLine label="Tiền order" value={money(Number(productAmount))} />
        <SummaryLine label="Giảm giá" value={`-${money(Number(discountAmount))}`} />
        <SummaryLine label="Tổng tiền" value={money(Number(total))} strong />
        <div className="modal-actions">
          <button className="ghost-btn" type="button" onClick={onCancel} disabled={busy}>Tiếp tục thêm món</button>
          <button className="danger-btn" type="button" onClick={onConfirm} disabled={busy}>{busy ? "Đang xử lý..." : "Kết thúc & tạo hóa đơn"}</button>
        </div>
      </div>
    </Modal>
  );
}

function SummaryLine({ label, value, strong = false }: { label: string; value: string; strong?: boolean }) {
  return (
    <div style={{ display: "flex", justifyContent: "space-between", gap: 12, border: "1px solid var(--line)", borderRadius: 8, padding: "10px 12px", background: "var(--soft)" }}>
      <span style={{ color: "var(--muted)" }}>{label}</span>
      <strong style={{ fontWeight: strong ? 800 : 700 }}>{value}</strong>
    </div>
  );
}
