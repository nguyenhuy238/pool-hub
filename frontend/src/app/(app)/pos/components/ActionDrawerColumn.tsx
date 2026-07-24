"use client";

import React, { useEffect, useState } from 'react';
import { usePOS } from '../POSContext';
import { X, Receipt, ArrowRightLeft, Coffee, Play } from 'lucide-react';
import styles from '../pos.module.css';
import { sessionApi, productApi, orderApi } from '@/lib/api/endpoints';
import type { Session, Product, Order } from '@/types';
import { useElapsed, formatDuration } from '../hooks/useElapsed';
import { CheckoutModal, type ReviewBill, type CheckoutExitResult } from './CheckoutModal';
import { StartSessionModal } from './StartSessionModal';
import { TransferTableModal } from './TransferTableModal';

export function ActionDrawerColumn() {
  const { selectedTable, isDrawerOpen, setIsDrawerOpen, setSelectedTable, triggerRefresh } = usePOS();
  const [sessionData, setSessionData] = useState<Session | null>(null);
  const [products, setProducts] = useState<Product[]>([]);
  const [orders, setOrders] = useState<Order[]>([]);
  const [sessionSummary, setSessionSummary] = useState<any>(null);

  const [loading, setLoading] = useState(false);

  // Điều phối checkout: modal đang mở hay không, và hóa đơn "đã đóng phiên nhưng chưa thu tiền" (safety net).
  const [checkoutOpen, setCheckoutOpen] = useState(false);
  const [pendingInvoiceId, setPendingInvoiceId] = useState<number | null>(null);
  const [startModalOpen, setStartModalOpen] = useState(false);
  const [transferOpen, setTransferOpen] = useState(false);

  useEffect(() => {
    // Load products once
    productApi.list().then(res => {
      const items = Array.isArray(res) ? res : (res as any).items || [];
      setProducts(items);
    }).catch(err => console.error("Failed to load products", err));
  }, []);

  // Đồng hồ phiên — đóng băng trong lúc đang checkout / chờ thu tiền.
  const elapsed = useElapsed(sessionData?.startedAtUtc, !(checkoutOpen || pendingInvoiceId != null));

  const loadSessionDetails = async () => {
    if (!selectedTable?.activeSessionId) {
      setSessionData(null);
      setOrders([]);
      setSessionSummary(null);
      return;
    }

    try {
      setLoading(true);
      const [session, sessionOrders, summary] = await Promise.all([
        sessionApi.detail(selectedTable.activeSessionId),
        orderApi.bySession(selectedTable.activeSessionId),
        sessionApi.summary(selectedTable.activeSessionId)
      ]);
      setSessionData(session);
      setOrders(sessionOrders as Order[]);
      setSessionSummary(summary);
    } catch (err) {
      console.error("Failed to load session details", err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (isDrawerOpen && selectedTable) {
      setCheckoutOpen(false);
      setPendingInvoiceId(null);
      setStartModalOpen(false);
      setTransferOpen(false);
      loadSessionDetails();
    }
  }, [isDrawerOpen, selectedTable]);

  if (!isDrawerOpen || !selectedTable) {
    return (
      <div className={styles.emptyDrawer}>
        <div className={styles.emptyBox}>
          <Receipt size={32} />
        </div>
        <h3 className={styles.emptyTitle}>Chưa chọn bàn</h3>
        <p className={styles.emptyDesc}>
          Chọn một bàn đang hoạt động trên sơ đồ để xem chi tiết Bill và thực hiện thanh toán.
        </p>
      </div>
    );
  }

  const closeDrawer = () => {
    setIsDrawerOpen(false);
    setSelectedTable(null);
  };

  const handleOrderProduct = async (product: Product) => {
    if (!selectedTable.activeSessionId) {
      alert("Vui lòng Mở Bàn trước khi gọi đồ!");
      return;
    }
    if (!sessionData?.sessionId) return;
    try {
      let activeOrder = orders[0];
      if (!activeOrder) {
        // Create order first
        activeOrder = await orderApi.create(sessionData.sessionId);
      }
      await orderApi.addItem(activeOrder.orderId!, { productId: product.productId, quantity: 1 });
      // Reload session details
      loadSessionDetails();
    } catch (err) {
      console.error("Failed to order product", err);
      alert("Lỗi khi thêm món!");
    }
  };

  // Mở checkout ở bước "review" (việc đóng phiên thật diễn ra bên trong modal).
  const handleEndSession = () => {
    if (!sessionData?.sessionId) return;
    setCheckoutOpen(true);
  };

  // Kết quả trả về từ CheckoutModal.
  const handleCheckoutExit = (result: CheckoutExitResult) => {
    setCheckoutOpen(false);
    if (result.paid) {
      setPendingInvoiceId(null);
      triggerRefresh();
      closeDrawer();
    } else if (result.invoiceId != null) {
      // Phiên đã đóng nhưng chưa thu tiền -> giữ lại để mở lại và thu.
      setPendingInvoiceId(result.invoiceId);
    }
    // invoiceId null & !paid -> hủy ngay ở bước review, không làm gì.
  };

  let calculatedFbAmount = 0;
  if (orders.length > 0 && orders[0].items) {
    calculatedFbAmount = orders[0].items.reduce((sum, item) => sum + (item.lineTotalAmount || (item.unitPriceSnapshot || 0) * item.quantity), 0);
  }

  const timeAmount = sessionSummary?.timeSubtotalAmount || sessionSummary?.TimeSubtotalAmount || 0;
  const fbAmount = sessionSummary?.productSubtotalAmount || sessionSummary?.ProductSubtotalAmount || sessionSummary?.orderSubtotalAmount || calculatedFbAmount;
  const subtotal = timeAmount + fbAmount;
  const depositApplied = sessionSummary?.depositAmount || sessionSummary?.DepositAmount || 0;
  const finalTotal = Math.max(0, subtotal - depositApplied);
  const refundAmount = depositApplied > subtotal ? depositApplied - subtotal : 0;

  const isEmpty = !selectedTable.activeSessionId;

  const reviewBill: ReviewBill = {
    durationLabel: formatDuration(elapsed).substring(0, 5),
    timeAmount,
    fbAmount,
    subtotal,
    depositApplied,
    finalTotal,
    refundAmount,
  };

  return (
    <div className={styles.drawerColumn}>
      <div className={styles.drawerHeader}>
        <div>
          <h2 className={styles.drawerHeaderTitle}>{selectedTable.tableName}</h2>
          <div className={styles.drawerTimer}>
            {isEmpty ? "Trạng thái: Chưa mở" : loading ? "Đang tải..." : formatDuration(elapsed)}
          </div>
        </div>
        <button onClick={closeDrawer} className={styles.closeBtn}>
          <X size={20} strokeWidth={2.5} />
        </button>
      </div>

      <div className={styles.drawerBody}>

        {/* Quick F&B Grid */}
        <div style={{ opacity: isEmpty ? 0.5 : 1 }}>
          <h3 className={styles.sectionTitle}>
            <Coffee size={16} /> Menu Gọi Nhanh {isEmpty && <span style={{fontSize: '0.8rem', color: '#ef4444', marginLeft: '8px'}}>(Cần mở bàn)</span>}
          </h3>
          <div className={styles.fbGrid}>
            {products.slice(0, 8).map(p => (
              <button
                key={p.productId}
                onClick={() => handleOrderProduct(p)}
                className="primary-btn"
                style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', padding: '0.5rem', background: '#f8fafc', color: '#0f172a', border: '1px solid #cbd5e1', fontSize: '0.85rem' }}
              >
                <span>{p.name}</span>
                <span style={{color: '#64748b', fontSize: '0.8rem', marginTop: '2px'}}>{(p.unitPrice || 0).toLocaleString()}đ</span>
              </button>
            ))}
          </div>
        </div>

        {/* Ordered Items */}
        {!isEmpty && orders.length > 0 && orders[0].items && orders[0].items.length > 0 && (
          <div style={{ marginTop: '20px' }}>
            <h3 className={styles.sectionTitle} style={{ marginBottom: '8px' }}>
              Món Đã Gọi
            </h3>
            <ul style={{ listStyle: 'none', padding: 0, margin: 0, fontSize: '0.9rem' }}>
              {Object.values(
                orders[0].items.reduce((acc, item) => {
                  const key = item.productId;
                  if (!acc[key]) {
                    acc[key] = { ...item };
                  } else {
                    acc[key].quantity += item.quantity;
                    const price = item.lineTotalAmount || ((item.unitPriceSnapshot || 0) * item.quantity);
                    acc[key].lineTotalAmount = (acc[key].lineTotalAmount || ((acc[key].unitPriceSnapshot || 0) * acc[key].quantity)) + price;
                  }
                  return acc;
                }, {} as Record<number, any>)
              ).map((item: any) => (
                <li key={item.productId} style={{ display: 'flex', justifyContent: 'space-between', padding: '6px 0', borderBottom: '1px solid #e2e8f0' }}>
                  <span>{item.quantity}x {item.productNameSnapshot || `Món #${item.productId}`}</span>
                  <span>{(item.lineTotalAmount || (item.unitPriceSnapshot || 0) * item.quantity).toLocaleString()}Đ</span>
                </li>
              ))}
            </ul>
          </div>
        )}

        {/* Bill Summary */}
        <div style={{ marginTop: 'auto', paddingTop: '20px' }}>
          <h3 className={styles.sectionTitle}>
            <Receipt size={16} /> Hóa Đơn Tạm Tính
          </h3>
          <div className={styles.billBox}>
            {isEmpty ? (
              <>
                <div className={styles.billRow}>
                  <span>Tiền giờ (Chưa bắt đầu)</span>
                  <span>0Đ</span>
                </div>
                <div className={styles.billLine}></div>
                <div className={styles.billTotal}>
                  <span>CẦN THU</span>
                  <span>0Đ</span>
                </div>
              </>
            ) : loading && !sessionSummary ? (
              <div className="text-center py-4 text-sm text-gray-500">Đang tải hóa đơn...</div>
            ) : (
              <>
                <div className={styles.billRow}>
                  <span>Tiền giờ ({formatDuration(elapsed).substring(0, 5)})</span>
                  <span>{timeAmount.toLocaleString()}Đ</span>
                </div>
                {fbAmount > 0 && (
                  <div className={styles.billRow}>
                    <span>Đồ uống (F&B)</span>
                    <span>{fbAmount.toLocaleString()}Đ</span>
                  </div>
                )}
                <div className={styles.billRow}>
                  <span>Tạm tính:</span>
                  <span>{subtotal.toLocaleString()}Đ</span>
                </div>
                {depositApplied > 0 && (
                  <div className={styles.billRow} style={{ color: '#16a34a', fontWeight: 500 }}>
                    <span>✓ Trừ cọc đã thanh toán</span>
                    <span>-{depositApplied.toLocaleString()}Đ</span>
                  </div>
                )}
                <div className={styles.billLine}></div>
                <div className={styles.billTotal}>
                  <span>CẦN THU</span>
                  <span style={{ color: finalTotal === 0 ? '#22c55e' : 'inherit' }}>{finalTotal.toLocaleString()}Đ</span>
                </div>
                {refundAmount > 0 && (
                  <div style={{ marginTop: '10px', padding: '8px 12px', background: '#fff7ed', border: '1px solid #fed7aa', borderRadius: '6px', color: '#c2410c', fontWeight: 600, fontSize: '0.85rem' }}>
                    ⚠️ Hoàn trả khách: {refundAmount.toLocaleString()}Đ
                  </div>
                )}
              </>
            )}
          </div>
        </div>

      </div>

      <div className={styles.drawerFooter}>
        {/* Nút "Gia hạn" tạm ẩn (chưa có endpoint backend — Q4). */}
        {!isEmpty && pendingInvoiceId == null && (
          <div style={{ display: 'flex', gap: '8px', width: '100%', marginBottom: '8px' }}>
            <button
              onClick={() => setTransferOpen(true)}
              className="primary-btn"
              style={{ flex: 1, padding: '0.75rem', background: 'white', color: '#0f172a', border: '1px solid #cbd5e1', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem' }}
            >
              <ArrowRightLeft size={16} /> Chuyển
            </button>
          </div>
        )}

        {isEmpty ? (
          <button
            onClick={() => setStartModalOpen(true)}
            className="primary-btn"
            style={{ width: '100%', padding: '1rem', background: '#3b82f6', color: 'white', border: 'none', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem', fontSize: '1.1rem' }}
          >
            <Play size={20} fill="currentColor" /> Mở Bàn Ngay
          </button>
        ) : pendingInvoiceId != null ? (
          <button
            onClick={() => setCheckoutOpen(true)}
            className="primary-btn"
            style={{ width: '100%', padding: '1rem', background: '#22c55e', color: 'white', border: 'none', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem', fontSize: '1.1rem' }}
          >
            <Receipt size={20} /> Thu Tiền Hóa Đơn
          </button>
        ) : (
          <button
            onClick={handleEndSession}
            className="primary-btn"
            style={{ width: '100%', padding: '1rem', background: '#dc2626', color: 'white', border: 'none', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem', fontSize: '1.1rem' }}
          >
            <X size={20} /> Kết Thúc Phiên
          </button>
        )}
      </div>

      {checkoutOpen && sessionData?.sessionId && (
        <CheckoutModal
          tableName={selectedTable.tableName}
          sessionId={sessionData.sessionId}
          reviewBill={reviewBill}
          initialInvoiceId={pendingInvoiceId}
          onExit={handleCheckoutExit}
        />
      )}

      {startModalOpen && isEmpty && (
        <StartSessionModal
          table={selectedTable}
          onClose={() => setStartModalOpen(false)}
          onStarted={() => { setStartModalOpen(false); triggerRefresh(); }}
        />
      )}

      {transferOpen && sessionData && !isEmpty && (
        <TransferTableModal
          session={sessionData}
          fromTable={selectedTable}
          onClose={() => setTransferOpen(false)}
          onTransferred={() => { setTransferOpen(false); triggerRefresh(); closeDrawer(); }}
        />
      )}

    </div>
  );
}
