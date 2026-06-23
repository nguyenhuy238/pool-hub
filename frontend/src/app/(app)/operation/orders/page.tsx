"use client";

import { useState } from "react";
import { orderApi, productApi, sessionApi } from "@/lib/api/endpoints";
import { money } from "@/lib/status";
import { DataTable, PageHeader, StateBlock, useList, useLoad } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { Order, Product, Session } from "@/types";

export default function OrdersPage() {
  const toast = useToast();
  const [sessionId, setSessionId] = useState<number | null>(null);
  const [orderId, setOrderId] = useState<number | null>(null);
  const { data, loading, error, reload } = useLoad(async () => {
    const [sessions, products, orders] = await Promise.all([sessionApi.list(), productApi.list(), sessionId ? orderApi.bySession(sessionId) : Promise.resolve([])]);
    return { sessions, products, orders };
  }, [sessionId]);
  const sessions = useList<Session>(data?.sessions);
  const products = useList<Product>(data?.products);
  const orders = data?.orders || [];

  async function ensureOrder() {
    if (orderId) return orderId;
    if (!sessionId) throw new Error("Chọn session trước.");
    const order = await orderApi.create(sessionId);
    setOrderId(order.orderId);
    return order.orderId;
  }

  return (
    <>
      <PageHeader title="Quản lý đơn hàng" description="Tạo đơn hàng theo phiên chơi và thêm sản phẩm." action={<select value={sessionId || ""} onChange={(e) => setSessionId(Number(e.target.value))}><option value="">Chọn phiên chơi</option>{sessions.map((item) => <option key={item.sessionId} value={item.sessionId}>{item.sessionCode || item.sessionId}</option>)}</select>} />
      <StateBlock loading={loading} error={error} />
      <div className="section-grid">
        <div className="card"><h2>Sản phẩm</h2><div className="floor-grid">{products.map((product) => <button className="card" key={product.productId} onClick={async () => { await orderApi.addItem(await ensureOrder(), { productId: product.productId, quantity: 1 }).then(() => toast("Đã thêm item.", "success")).catch((err) => toast(err.message, "error")); reload(); }}><strong>{product.name}</strong><p>{money(product.unitPrice)} · Kho {product.stockQuantity}</p></button>)}</div></div>
        <div className="card"><h2>Orders</h2><DataTable rows={orders as unknown as Record<string, unknown>[]} columns={[{ key: "orderCode", label: "Order" }, { key: "status", label: "Status" }, { key: "subtotalAmount", label: "Tổng", render: (row) => money(Number(row.subtotalAmount || 0)) }]} actions={(row) => <button className="danger-btn" onClick={() => orderApi.cancel(Number(row.orderId)).then(() => reload())}>Cancel</button>} /></div>
      </div>
    </>
  );
}
