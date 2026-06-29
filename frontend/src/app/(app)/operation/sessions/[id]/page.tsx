"use client";

import { useMemo, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { orderApi, productApi, sessionApi } from "@/lib/api/endpoints";
import { dateTime, label, money, sessionStatus } from "@/lib/status";
import { Badge, DataTable, Modal, PageHeader, StateBlock, useList, useLoad } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { Order, Product, Session } from "@/types";

const SESSION_OPEN = 1;

export default function SessionDetailPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const toast = useToast();
  const sessionId = Number(params?.id);
  const [selectedOrderId, setSelectedOrderId] = useState<number | null>(null);
  const [closing, setClosing] = useState(false);
  const [summaryOpen, setSummaryOpen] = useState(false);

  const { data, loading, error, reload } = useLoad(async () => {
    if (!Number.isFinite(sessionId) || sessionId <= 0) throw new Error("Session không hợp lệ.");
    const [session, summary, orders, products] = await Promise.all([
      sessionApi.detail(sessionId),
      sessionApi.summary(sessionId),
      orderApi.bySession(sessionId),
      productApi.list()
    ]);
    return { session, summary, orders, products };
  }, [sessionId]);

  const session = data?.session as Session | undefined;
  const summary = data?.summary as any;
  const orders = (data?.orders || []) as Order[];
  const products = useList<Product>(data?.products);
  const currentOrder = selectedOrderId ? orders.find((order) => order.orderId === selectedOrderId) : orders[0] ?? null;
  const currentAssignment = useMemo(() => {
    const assignments = session?.assignments || [];
    return assignments.find((assignment) => !assignment.endedAtUtc) || assignments[assignments.length - 1];
  }, [session?.assignments]);
  const isOpen = Number(session?.status) === SESSION_OPEN;

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
                </div>
                <Badge tone={isOpen ? "green" : "neutral"}>{label(sessionStatus, Number(session.status))}</Badge>
              </div>
              <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(180px, 1fr))", gap: 12 }}>
                <Info label="Mã session" value={session.sessionCode || `#${session.sessionId}`} />
                <Info label="Bàn hiện tại" value={currentAssignment?.tableName || currentAssignment?.tableCode || "-"} />
                <Info label="Khách hàng" value={session.customerId ? `#${session.customerId}` : "Khách vãng lai"} />
                <Info label="Thời lượng thực tế" value={`${totalDuration} phút`} />
              </div>
            </div>

            <div className="panel">
              <div className="panel-head">
                <div>
                  <h3>Tạm tính</h3>
                  <p>Backend tính tiền giờ theo bảng giá, minimum và block hiện hành.</p>
                </div>
                <button className="ghost-btn" type="button" onClick={() => setSummaryOpen(true)}>Xem chi tiết tạm tính</button>
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
                <button className="ghost-btn" type="button" onClick={async () => { await reload(); setSummaryOpen(true); }}>Tạm tính</button>
                <button className="danger-btn" type="button" disabled={!isOpen || closing} onClick={closeSession}>{closing ? "Đang xử lý..." : "Kết thúc & tạo hóa đơn"}</button>
              </div>
            </div>
          </div>
        ) : null}
      </section>
      {summaryOpen && summary ? <Modal title="Chi tiết tạm tính" onClose={() => setSummaryOpen(false)} size="large">
        <div style={{ display: "grid", gap: 12 }}>
          {(summary.assignments || []).map((assignment: any) => (
            <div key={assignment.sessionTableAssignmentId} style={{ border: "1px solid var(--line)", borderRadius: 8, padding: 12 }}>
              <strong>{assignment.tableName || assignment.tableCode}</strong>
              <p style={{ margin: "6px 0", color: "var(--muted)" }}>
                {money(Number(assignment.hourlyRate || assignment.hourlyRateSnapshot || 0))}/giờ · thực tế {assignment.actualDurationMinutes ?? assignment.durationMinutes} phút · tính tiền {assignment.billableDurationMinutes ?? assignment.durationMinutes} phút
              </p>
              {(assignment.billableDurationMinutes ?? 0) > (assignment.actualDurationMinutes ?? assignment.durationMinutes ?? 0)
                ? <div className="inline-note">Áp dụng minimum {assignment.minimumMinutes} phút hoặc block {assignment.billingBlockMinutes} phút.</div>
                : null}
            </div>
          ))}
          <Info label="Tổng thanh toán" value={money(Number(grandTotal))} strong />
        </div>
      </Modal> : null}
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
