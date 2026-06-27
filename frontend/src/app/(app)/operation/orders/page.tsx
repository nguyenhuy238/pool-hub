"use client";

import { useEffect, useState } from "react";
import { orderApi, productApi, sessionApi } from "@/lib/api/endpoints";
import { money } from "@/lib/status";
import { ConfirmDialog, DataTable, PageHeader, StateBlock, useList, useLoad } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { Order, OrderItem, Product, Session } from "@/types";

const statusText = (status?: number) => {
  if (status === 2) return "Hoàn thành";
  if (status === 3) return "Đã hủy";
  return "Mới";
};

export default function OrdersPage() {
  const toast = useToast();
  const [sessionId, setSessionId] = useState<number | null>(null);
  const [selectedOrderId, setSelectedOrderId] = useState<number | null>(null);
  const [cancellingOrder, setCancellingOrder] = useState<Order | null>(null);
  const [removingItem, setRemovingItem] = useState<OrderItem | null>(null);
  const [itemQuantities, setItemQuantities] = useState<Record<number, number>>({});
  const { data, loading, error, reload } = useLoad(async () => {
    const [sessions, products, orders] = await Promise.all([sessionApi.list(), productApi.list(), sessionId ? orderApi.bySession(sessionId) : Promise.resolve([])]);
    return { sessions, products, orders };
  }, [sessionId]);

  const sessions = useList<Session>(data?.sessions);
  const products = useList<Product>(data?.products);
  const orders = data?.orders || [] as Order[];
  const currentOrder = selectedOrderId ? orders.find((order) => order.orderId === selectedOrderId) : orders[0] ?? null;

  useEffect(() => {
    const initialSessionId = Number(new URLSearchParams(window.location.search).get("sessionId") || 0) || null;
    if (initialSessionId) setSessionId(initialSessionId);
  }, []);

  useEffect(() => {
    setSelectedOrderId(null);
    setItemQuantities({});
  }, [sessionId]);

  useEffect(() => {
    setItemQuantities({});
  }, [currentOrder?.orderId]);

  async function ensureOrder() {
    if (selectedOrderId) return selectedOrderId;
    if (!sessionId) throw new Error("Chọn session trước.");
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
      toast("Đã thêm sản phẩm vào order.", "success");
      await reload();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Thao tác thất bại.", "error");
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
          <select value={sessionId || ""} onChange={(e) => {
            setSessionId(Number(e.target.value) || null);
          }}>
            <option value="">Chọn phiên chơi</option>
            {sessions.map((item) => (
              <option key={item.sessionId} value={item.sessionId}>
                {item.sessionCode || item.sessionId}
              </option>
            ))}
          </select>
        }
      />
      <StateBlock loading={loading} error={error} />
      <div className="section-grid">
        <div className="card">
          <h2>Sản phẩm</h2>
          <div className="floor-grid">
            {products.map((product) => (
              <button
                className="card"
                key={product.productId}
                onClick={() => addProduct(product)}
              >
                <strong>{product.name}</strong>
                <p>{money(product.unitPrice)} · Kho {product.stockQuantity}</p>
              </button>
            ))}
          </div>
        </div>

        <div className="card">
          <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12, marginBottom: 16 }}>
            <div>
              <h2>Đơn hàng</h2>
              <div style={{ color: "#555", marginTop: 4 }}>
                {currentOrder ? `Đã chọn: ${currentOrder.orderCode}` : "Chưa chọn đơn hàng."}
              </div>
            </div>
            <button
              className="primary-btn"
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

          <DataTable
            rows={orders as unknown as Record<string, unknown>[]}
            columns={[
              { key: "orderCode", label: "Đơn hàng" },
              { key: "status", label: "Trạng thái", render: (row) => statusText(Number(row.status)) },
              { key: "subtotalAmount", label: "Tổng", render: (row) => money(Number(row.subtotalAmount || 0)) }
            ]}
            actions={(row) => (
              <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
                <button onClick={() => setSelectedOrderId(Number(row.orderId))}>Chọn</button>
                <button className="danger-btn" onClick={() => setCancellingOrder(row as unknown as Order)}>Hủy</button>
              </div>
            )}
          />

          {currentOrder ? (
            <section style={{ marginTop: 24 }}>
              <h3>Chi tiết đơn hàng</h3>
              {currentOrder.items?.length ? (
                <div className="table-wrap">
                  <table>
                    <thead>
                      <tr>
                        <th>Sản phẩm</th>
                        <th>Đơn giá</th>
                        <th>Số lượng</th>
                        <th>Thành tiền</th>
                        <th>Thao tác</th>
                      </tr>
                    </thead>
                    <tbody>
                      {currentOrder.items.map((item) => {
                        const quantity = itemQuantities[item.orderItemId] ?? item.quantity;
                        return (
                          <tr key={item.orderItemId}>
                            <td>{item.productNameSnapshot}</td>
                            <td>{money(item.unitPriceSnapshot ?? 0)}</td>
                            <td>
                              <input
                                type="number"
                                min={1}
                                value={quantity}
                                onChange={(event) => setItemQuantities((current) => ({
                                  ...current,
                                  [item.orderItemId]: Number(event.target.value)
                                }))}
                                style={{ width: 80 }}
                              />
                            </td>
                            <td>{money(item.lineTotalAmount ?? 0)}</td>
                            <td style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
                              <button
                                onClick={() => updateItem(item, quantity)}
                              >
                                Cập nhật
                              </button>
                              <button className="danger-btn" onClick={() => setRemovingItem(item)}>
                                Xóa
                              </button>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              ) : (
                <p>Chưa có sản phẩm trong order.</p>
              )}
            </section>
          ) : null}
        </div>
      </div>
      {cancellingOrder ? <ConfirmDialog title="Hủy đơn hàng" message={`Xác nhận hủy đơn hàng ${cancellingOrder.orderCode || cancellingOrder.orderId}?`} confirmLabel="Hủy đơn hàng" danger onCancel={() => setCancellingOrder(null)} onConfirm={() => cancelOrder(cancellingOrder)} /> : null}
      {removingItem ? <ConfirmDialog title="Xóa sản phẩm khỏi đơn hàng" message={`Xóa “${removingItem.productNameSnapshot || removingItem.productId}” khỏi đơn hàng?`} confirmLabel="Xóa" danger onCancel={() => setRemovingItem(null)} onConfirm={async () => { await deleteItem(removingItem); setRemovingItem(null); }} /> : null}
    </>
  );
}
