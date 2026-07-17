"use client";

import React, { useEffect, useState } from 'react';
import { usePOS } from '../POSContext';
import { X, Receipt, Clock, ArrowRightLeft, Coffee, Plus, Minus, Play } from 'lucide-react';
import styles from '../pos.module.css';
import { API_BASE_URL } from '@/lib/api/client';
import { sessionApi, productApi, orderApi, invoiceApi } from '@/lib/api/endpoints';
import type { Session, Product, Order, Invoice, PaymentMethod } from '@/types';
import { Modal } from '@/components/ui';
import { utcTimestampMs } from '@/lib/dateTime';

function formatDuration(ms: number) {
  const totalSeconds = Math.max(0, Math.floor(ms / 1000));
  const h = Math.floor(totalSeconds / 3600).toString().padStart(2, '0');
  const m = Math.floor((totalSeconds % 3600) / 60).toString().padStart(2, '0');
  const s = (totalSeconds % 60).toString().padStart(2, '0');
  return `${h}:${m}:${s}`;
}

export function ActionDrawerColumn() {
  const { selectedTable, isDrawerOpen, setIsDrawerOpen, setSelectedTable, triggerRefresh } = usePOS();
  const [sessionData, setSessionData] = useState<Session | null>(null);
  const [products, setProducts] = useState<Product[]>([]);
  const [orders, setOrders] = useState<Order[]>([]);
  const [sessionSummary, setSessionSummary] = useState<any>(null);
  
  const [loading, setLoading] = useState(false);
  const [elapsed, setElapsed] = useState(0);

  const [checkoutModalOpen, setCheckoutModalOpen] = useState(false);
  const [invoiceData, setInvoiceData] = useState<Invoice | null>(null);
  const [paymentMethods, setPaymentMethods] = useState<PaymentMethod[]>([]);
  const [selectedPaymentMethod, setSelectedPaymentMethod] = useState<number | "">("");
  const [processingPayment, setProcessingPayment] = useState(false);

  const [isSessionEndedLocal, setIsSessionEndedLocal] = useState(false);
  const [generatedInvoiceId, setGeneratedInvoiceId] = useState<number | null>(null);

  useEffect(() => {
    // Load products once
    productApi.list().then(res => {
      const items = Array.isArray(res) ? res : (res as any).items || [];
      setProducts(items);
    }).catch(err => console.error("Failed to load products", err));
  }, []);

  // Sync elapsed time every second
  useEffect(() => {
    if (!sessionData?.startedAtUtc) return;
    if (isSessionEndedLocal) return; // Stop timer when session ended
    const startMs = utcTimestampMs(sessionData.startedAtUtc);
    
    const tick = () => setElapsed(Date.now() - startMs);
    tick(); // initial tick
    const iv = setInterval(tick, 1000);
    return () => clearInterval(iv);
  }, [sessionData?.startedAtUtc, isSessionEndedLocal]);

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
      setIsSessionEndedLocal(false);
      setGeneratedInvoiceId(null);
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

  const handleStartSession = async () => {
    try {
      await sessionApi.start({ tableId: selectedTable.tableId });
      triggerRefresh();
    } catch (err) {
      console.error("Lỗi khi mở bàn", err);
      alert("Không thể mở bàn. Vui lòng thử lại.");
    }
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

  const handleEndSession = async () => {
    if (!sessionData?.sessionId) return;
    if (confirm(`Xác nhận kết thúc phiên chơi cho ${selectedTable.tableName}?`)) {
      try {
        setProcessingPayment(true);
        const res = await sessionApi.end(sessionData.sessionId);
        
        let invId = res?.invoiceId;
        if (!invId) {
           const generated = await invoiceApi.generate(sessionData.sessionId);
           invId = generated.invoiceId;
        }
        
        setGeneratedInvoiceId(invId);
        setIsSessionEndedLocal(true);
        // Do NOT triggerRefresh here so that we keep the active session view
      } catch (err) {
        console.error("Failed to end session", err);
        alert("Lỗi khi kết thúc phiên!");
      } finally {
        setProcessingPayment(false);
      }
    }
  };

  const handleCheckout = async () => {
    if (!generatedInvoiceId) return;
    try {
      setProcessingPayment(true);
      
      const [inv, methodsRes] = await Promise.all([
         invoiceApi.detail(generatedInvoiceId),
         invoiceApi.paymentMethods()
      ]);
      
      const methods = Array.isArray(methodsRes) ? methodsRes : ((methodsRes as any).items || []);
      
      setInvoiceData(inv);
      const invRemainingAmount = inv?.remainingAmount ?? (inv?.grandTotalAmount || 0);
      setPaymentMethods(methods as PaymentMethod[]);
      if (methods && methods.length > 0) {
         setSelectedPaymentMethod(methods[0].paymentMethodId);
      }
      setCheckoutModalOpen(true);
    } catch (err) {
      console.error("Failed to open checkout", err);
      alert("Lỗi tải thông tin thanh toán!");
    } finally {
      setProcessingPayment(false);
    }
  };

  const handleConfirmPayment = async () => {
    if (!invoiceData) return;
    if (!selectedPaymentMethod) {
      alert("Vui lòng chọn phương thức thanh toán.");
      return;
    }
    
    try {
      setProcessingPayment(true);
      const invRemainingAmount = invoiceData.remainingAmount ?? invoiceData.grandTotalAmount ?? 0;
      if (invRemainingAmount > 0 && selectedPaymentMethod) {
        await invoiceApi.pay({
          invoiceId: invoiceData.invoiceId,
          paymentMethodId: Number(selectedPaymentMethod),
          amount: invRemainingAmount
        });
      }
      // Nếu remaining = 0, invoice đã được auto-paid khi generate — chỉ cần đóng modal
      alert("Thanh toán thành công!");
      setCheckoutModalOpen(false);
      triggerRefresh();
      closeDrawer();
    } catch (err) {
      console.error("Payment failed", err);
      alert("Lỗi khi xác nhận thanh toán!");
    } finally {
      setProcessingPayment(false);
    }
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
        {!isEmpty && !isSessionEndedLocal && (
          <div style={{ display: 'flex', gap: '8px', width: '100%', marginBottom: '8px' }}>
            <button className="primary-btn" style={{ flex: 1, padding: '0.75rem', background: 'white', color: '#0f172a', border: '1px solid #cbd5e1', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem' }}>
              <ArrowRightLeft size={16} /> Chuyển
            </button>
            <button className="primary-btn" style={{ flex: 1, padding: '0.75rem', background: '#eff6ff', color: '#2563eb', border: '1px solid #bfdbfe', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem' }}>
              <Clock size={16} /> Gia hạn
            </button>
          </div>
        )}

        {isEmpty ? (
          <button 
            onClick={handleStartSession}
            className="primary-btn" 
            style={{ width: '100%', padding: '1rem', background: '#3b82f6', color: 'white', border: 'none', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem', fontSize: '1.1rem' }}
          >
            <Play size={20} fill="currentColor" /> Mở Bàn Ngay
          </button>
        ) : !isSessionEndedLocal ? (
          <button 
            onClick={handleEndSession}
            disabled={processingPayment}
            className="primary-btn" 
            style={{ width: '100%', padding: '1rem', background: '#dc2626', color: 'white', border: 'none', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem', fontSize: '1.1rem', opacity: processingPayment ? 0.7 : 1 }}
          >
            <X size={20} /> {processingPayment ? "Đang xử lý..." : "Kết Thúc Phiên"}
          </button>
        ) : (
          <div style={{ display: 'flex', gap: '8px', width: '100%' }}>
            <button 
              disabled
              className="primary-btn" 
              style={{ flex: 1, padding: '1rem', background: '#e2e8f0', color: '#94a3b8', border: 'none', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem', fontSize: '1.1rem', cursor: 'not-allowed' }}
            >
               Tiếp tục phiên
            </button>
            <button 
              onClick={handleCheckout}
              disabled={processingPayment}
              className="primary-btn" 
              style={{ flex: 2, padding: '1rem', background: '#22c55e', color: 'white', border: 'none', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem', fontSize: '1.1rem', opacity: processingPayment ? 0.7 : 1 }}
            >
              <Receipt size={20} /> {processingPayment ? "Đang xử lý..." : "Thanh Toán"}
            </button>
          </div>
        )}
      </div>

      {checkoutModalOpen && invoiceData && (() => {
        const invRemainingAmount = invoiceData.remainingAmount ?? invoiceData.grandTotalAmount ?? 0;
        return (
        <Modal title="Thanh Toán Hóa Đơn" onClose={() => setCheckoutModalOpen(false)} size="medium">
          <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
            <div style={{ textAlign: 'center', marginBottom: '8px' }}>
              <h3 style={{ fontSize: '1.25rem', margin: 0 }}>Hóa Đơn Tổng Hợp</h3>
              <p style={{ color: '#64748b', fontSize: '0.9rem', margin: '4px 0 0' }}>Bàn: {selectedTable.tableName} - Mã HĐ: {invoiceData.invoiceCode || `#${invoiceData.invoiceId}`}</p>
            </div>
            
            <div style={{ background: '#f8fafc', padding: '16px', borderRadius: '8px', border: '1px solid #e2e8f0' }}>
               <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
                 {Object.values((invoiceData.lines || []).reduce((acc: any, l: any) => {
                    const key = l.lineType === 'TIME' ? `TIME_${l.description}` : l.description;
                    if (!acc[key]) {
                      acc[key] = { ...l };
                    } else {
                      acc[key].quantity = Number(acc[key].quantity) + Number(l.quantity);
                      acc[key].lineTotalAmount = Number(acc[key].lineTotalAmount) + Number(l.lineTotalAmount);
                    }
                    return acc;
                 }, {})).map((l: any, i: number) => (
                    <li key={i} style={{ display: 'flex', justifyContent: 'space-between', padding: '8px 0', borderBottom: '1px solid #e2e8f0' }}>
                       <div>
                         <span style={{ fontWeight: 500 }}>{l.description}</span>
                         <div style={{ fontSize: '0.8rem', color: '#64748b' }}>SL: {l.lineType === 'TIME' ? `${Math.round(Number(l.quantity) * 60)} phút` : l.quantity}</div>
                       </div>
                       <div style={{ fontWeight: 500 }}>{(l.lineTotalAmount || 0).toLocaleString()}Đ</div>
                    </li>
                 ))}
               </ul>
               
               {/* Deposit breakdown */}
               {(invoiceData.depositAppliedAmount ?? 0) > 0 && (
                 <>
                   <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: '12px', paddingTop: '8px', borderTop: '1px dashed #e2e8f0', fontSize: '0.9rem', color: '#64748b' }}>
                     <span>Tạm tính:</span>
                     <span>{(invoiceData.grandTotalAmount || 0).toLocaleString()}Đ</span>
                   </div>
                   <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.95rem', fontWeight: 600, color: '#16a34a', marginTop: '4px' }}>
                     <span>✓ Trừ cọc đã thanh toán:</span>
                     <span>-{((invoiceData.depositAppliedAmount ?? 0) + (invoiceData.depositRefundAmount ?? 0)).toLocaleString()}Đ</span>
                   </div>
                 </>
               )}
               
               <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: '16px', paddingTop: '16px', borderTop: '2px dashed #cbd5e1', fontSize: '1.2rem', fontWeight: 'bold' }}>
                  <span>TỔNG CẦN THU:</span>
                  <span style={{ color: invRemainingAmount === 0 ? '#22c55e' : '#2563eb' }}>{(invRemainingAmount).toLocaleString()}Đ</span>
               </div>
               
               {(invoiceData.depositRefundAmount ?? 0) > 0 && (
                 <div style={{ marginTop: '10px', padding: '8px 12px', background: '#fff7ed', border: '1px solid #fed7aa', borderRadius: '6px', color: '#c2410c', fontWeight: 600 }}>
                   ⚠️ Cần hoàn trả khách: {(invoiceData.depositRefundAmount ?? 0).toLocaleString()}Đ
                 </div>
               )}
            </div>

            {invRemainingAmount > 0 && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                <label style={{ fontWeight: 500 }}>Phương thức thanh toán:</label>
                <select 
                  value={selectedPaymentMethod} 
                  onChange={e => setSelectedPaymentMethod(Number(e.target.value))}
                  style={{ padding: '10px', borderRadius: '6px', border: '1px solid #cbd5e1', width: '100%', fontSize: '1rem', outline: 'none' }}
                >
                  {paymentMethods.map(m => (
                    <option key={m.paymentMethodId} value={m.paymentMethodId}>{m.name}</option>
                  ))}
                </select>
              </div>
            )}

            {invRemainingAmount > 0 && (() => {
              const method = paymentMethods.find(m => m.paymentMethodId === selectedPaymentMethod);
              const methodStr = (method?.code || '') + ' ' + (method?.name || '');
              const isBankTransfer = methodStr.toLowerCase().includes('bank') || methodStr.toLowerCase().includes('chuyển khoản');
              if (isBankTransfer) {
                return (
                  <div style={{ marginTop: '8px', textAlign: 'center' }}>
                    <p style={{ fontSize: '0.9rem', color: '#475569', marginBottom: '8px', fontWeight: 500 }}>Quét mã QR để thanh toán nhanh</p>
                    <img 
                      src={`${API_BASE_URL}/api/invoices/${invoiceData.invoiceId}/qr-code?amt=${invoiceData.grandTotalAmount || 0}&t=${Date.now()}`} 
                      alt="QR Code" 
                      style={{ width: '220px', height: '220px', objectFit: 'contain', border: '1px solid #e2e8f0', borderRadius: '8px', padding: '8px', background: 'white' }}
                    />
                  </div>
                );
              }
              return null;
            })()}

            <div style={{ display: 'flex', gap: '12px', marginTop: '16px' }}>
              <button onClick={() => setCheckoutModalOpen(false)} style={{ flex: 1, padding: '12px', background: '#e2e8f0', color: '#475569', border: 'none', borderRadius: '6px', fontWeight: 600, cursor: 'pointer' }}>Hủy Bỏ</button>
              <button onClick={handleConfirmPayment} disabled={processingPayment} style={{ flex: 2, padding: '12px', background: '#22c55e', color: 'white', border: 'none', borderRadius: '6px', fontWeight: 600, cursor: 'pointer' }}>
                {processingPayment ? "Đang xử lý..." : invRemainingAmount === 0 ? "Hoàn Tất Miễn Thu" : "Xác Nhận Thu Tiền"}
              </button>
            </div>
          </div>
        </Modal>
        );
      })()}

    </div>
  );
}
