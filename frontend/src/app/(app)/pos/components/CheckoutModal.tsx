"use client";

import React, { useEffect, useRef, useState } from 'react';
import { Modal } from '@/components/ui';
import { API_BASE_URL } from '@/lib/api/client';
import { sessionApi, invoiceApi } from '@/lib/api/endpoints';
import type { Invoice, PaymentMethod } from '@/types';
import { openInvoiceDisplay, openInvoicePage } from '@/lib/invoiceDisplay';
import { usePOS } from '../POSContext';

/** Số liệu bill tạm tính hiển thị ở bước "review" (tính sẵn tại drawer). */
export interface ReviewBill {
  durationLabel: string;
  timeAmount: number;
  fbAmount: number;
  subtotal: number;
  depositApplied: number;
  finalTotal: number;
  refundAmount: number;
}

export interface CheckoutExitResult {
  /** Id hóa đơn đã tạo (session đã đóng). null nếu hủy ngay ở bước review (chưa đóng phiên). */
  invoiceId: number | null;
  /** Đã thu tiền xong hay chưa. */
  paid: boolean;
}

interface CheckoutModalProps {
  tableName: string;
  sessionId: number;
  reviewBill: ReviewBill;
  /** Có giá trị khi MỞ LẠI modal cho một phiên đã đóng nhưng chưa thu tiền (safety net). */
  initialInvoiceId?: number | null;
  onExit: (result: CheckoutExitResult) => void;
}

type Step = 'review' | 'paying' | 'success';

function isBankTransferMethod(method?: PaymentMethod) {
  const str = `${method?.code || ''} ${method?.name || ''}`.toLowerCase();
  return str.includes('bank') || str.includes('chuyển khoản');
}

export function CheckoutModal({ tableName, sessionId, reviewBill, initialInvoiceId, onExit }: CheckoutModalProps) {
  const { refreshTrigger } = usePOS();

  const [step, setStep] = useState<Step>(initialInvoiceId ? 'paying' : 'review');
  const [invoiceId, setInvoiceId] = useState<number | null>(initialInvoiceId ?? null);
  const [invoiceData, setInvoiceData] = useState<Invoice | null>(null);
  const [paymentMethods, setPaymentMethods] = useState<PaymentMethod[]>([]);
  const [selectedPaymentMethod, setSelectedPaymentMethod] = useState<number | "">("");
  const [processing, setProcessing] = useState(false);
  const [loadingInvoice, setLoadingInvoice] = useState(false);

  // Refs để callback trong setTimeout/effect luôn đọc giá trị mới nhất mà không cần đưa vào deps.
  const exitRef = useRef(onExit);
  exitRef.current = onExit;
  const invoiceIdRef = useRef(invoiceId);
  invoiceIdRef.current = invoiceId;
  // Đảm bảo màn hình hóa đơn cho khách chỉ mở một lần (tránh mở 2 cửa sổ ở luồng tiền mặt).
  const receiptOpenedRef = useRef(false);

  // Tải chi tiết hóa đơn + phương thức thanh toán cho bước "paying".
  const loadInvoiceAndMethods = async (invId: number) => {
    setLoadingInvoice(true);
    try {
      const [inv, methodsRes] = await Promise.all([
        invoiceApi.detail(invId),
        invoiceApi.paymentMethods(),
      ]);
      const methods = Array.isArray(methodsRes) ? methodsRes : ((methodsRes as { items?: PaymentMethod[] }).items || []);
      setInvoiceData(inv);
      setPaymentMethods(methods as PaymentMethod[]);
      if (methods.length > 0) setSelectedPaymentMethod(methods[0].paymentMethodId);
    } finally {
      setLoadingInvoice(false);
    }
  };

  // Khi mở lại modal cho phiên đã đóng (safety net) -> nạp ngay hóa đơn.
  useEffect(() => {
    if (initialInvoiceId) {
      loadInvoiceAndMethods(initialInvoiceId).catch((err) => {
        console.error('Failed to load invoice for checkout', err);
        alert('Không tải được thông tin hóa đơn!');
      });
    }
    // Chỉ chạy một lần lúc mount.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Bước review -> đóng phiên thật -> nạp hóa đơn -> sang bước thu tiền. Trả về invoiceId để nút mở trang.
  const closeSessionAndLoad = async (): Promise<number | null> => {
    try {
      setProcessing(true);
      const res = await sessionApi.end(sessionId);
      let invId: number | undefined = res?.invoiceId;
      if (!invId) {
        const generated = await invoiceApi.generate(sessionId);
        invId = generated.invoiceId;
      }
      setInvoiceId(invId ?? null);
      await loadInvoiceAndMethods(invId!);
      setStep('paying');
      return invId ?? null;
    } catch (err) {
      console.error('Failed to close session', err);
      alert('Lỗi khi kết thúc phiên!');
      return null;
    } finally {
      setProcessing(false);
    }
  };

  // Lựa chọn 1: chốt phiên rồi thu tiền ngay tại POS.
  const handleConfirmClose = () => { void closeSessionAndLoad(); };

  // Lựa chọn 2: chốt phiên rồi mở thẳng trang Quản lý Hóa đơn để thu tiền y hệt.
  const handleConfirmCloseAndOpenInvoice = async () => {
    // Mở tab đồng bộ ngay khi bấm (tránh bị chặn popup), điền URL sau khi có invoiceId.
    const win = typeof window !== 'undefined' ? window.open('', 'poolhub-invoice-page') : null;
    const invId = await closeSessionAndLoad();
    if (invId != null) {
      if (win) win.location.href = `/operation/invoices?invoiceId=${invId}`;
      else openInvoicePage(invId);
    } else {
      win?.close();
    }
  };

  // Bước thu tiền -> gọi API thanh toán (thu đủ) -> success.
  const handleConfirmPayment = async () => {
    if (!invoiceData || invoiceId == null) return;
    const remaining = invoiceData.remainingAmount ?? invoiceData.grandTotalAmount ?? 0;
    if (remaining > 0 && !selectedPaymentMethod) {
      alert('Vui lòng chọn phương thức thanh toán.');
      return;
    }
    // Mở màn hình hóa đơn cho khách ĐỒNG BỘ ngay đầu click để tránh bị chặn popup.
    openInvoiceDisplay(invoiceId);
    receiptOpenedRef.current = true;
    try {
      setProcessing(true);
      if (remaining > 0) {
        await invoiceApi.pay({
          invoiceId,
          paymentMethodId: Number(selectedPaymentMethod),
          amount: remaining, // Thu đủ một lần (không cho thanh toán từng phần — Q5).
        });
      }
      setStep('success');
    } catch (err) {
      console.error('Payment failed', err);
      alert('Lỗi khi xác nhận thanh toán!');
    } finally {
      setProcessing(false);
    }
  };

  // Tự động xác nhận khi thanh toán QR thành công (đẩy về qua SignalR -> refreshTrigger).
  useEffect(() => {
    if (step !== 'paying' || invoiceId == null) return;
    invoiceApi.detail(invoiceId)
      .then((inv) => {
        setInvoiceData(inv);
        const remaining = Number(inv.remainingAmount ?? ((inv.grandTotalAmount || 0) - (inv.paidAmount || 0)));
        if (Number(inv.paymentStatus) === 3 || (remaining <= 0 && Number(inv.status) !== 3)) {
          setStep('success');
        }
      })
      .catch((err) => console.error('Failed to refresh invoice data', err));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [refreshTrigger]);

  // Vào bước success -> mở màn hình hóa đơn cho khách nếu luồng QR chưa mở (best-effort),
  // rồi chờ ~1.5s và báo cho parent (refresh + đóng drawer).
  useEffect(() => {
    if (step !== 'success') return;
    if (!receiptOpenedRef.current && invoiceIdRef.current != null) {
      openInvoiceDisplay(invoiceIdRef.current);
      receiptOpenedRef.current = true;
    }
    const t = setTimeout(() => exitRef.current({ invoiceId: invoiceIdRef.current, paid: true }), 1500);
    return () => clearTimeout(t);
  }, [step]);

  // Hủy: ở review -> chưa đóng phiên (invoiceId null). Ở paying -> đã đóng, trả invoiceId để mở lại sau.
  const handleCancel = () => {
    if (processing) return;
    onExit({ invoiceId, paid: false });
  };

  // ----- Bước THÀNH CÔNG -----
  if (step === 'success') {
    return (
      <Modal title="" onClose={handleCancel} size="medium">
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', padding: '40px 20px', gap: '16px' }}>
          <div style={{ width: '80px', height: '80px', borderRadius: '50%', background: '#dcfce7', color: '#22c55e', display: 'flex', alignItems: 'center', justifyContent: 'center', marginBottom: '16px', boxShadow: '0 4px 6px -1px rgba(34, 197, 94, 0.2)' }}>
            <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="20 6 9 17 4 12"></polyline>
            </svg>
          </div>
          <h2 style={{ color: '#16a34a', fontSize: '1.5rem', margin: 0 }}>Thanh Toán Thành Công!</h2>
          <p style={{ color: '#64748b', fontSize: '1rem', margin: 0, textAlign: 'center' }}>Hóa đơn đã được xác nhận và tự động đóng.</p>
        </div>
      </Modal>
    );
  }

  // ----- Bước REVIEW (xem lại bill tạm tính, chưa đóng phiên) -----
  if (step === 'review') {
    const b = reviewBill;
    return (
      <Modal title="Xác Nhận Kết Thúc Phiên" onClose={handleCancel} size="medium">
        <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <div style={{ textAlign: 'center' }}>
            <h3 style={{ fontSize: '1.25rem', margin: 0 }}>Bàn: {tableName}</h3>
            <p style={{ color: '#64748b', fontSize: '0.9rem', margin: '4px 0 0' }}>Kiểm tra lại tạm tính trước khi chốt phiên và thu tiền.</p>
          </div>

          <div style={{ background: '#f8fafc', padding: '16px', borderRadius: '8px', border: '1px solid #e2e8f0', display: 'flex', flexDirection: 'column', gap: '8px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.9rem' }}>
              <span>Tiền giờ ({b.durationLabel})</span>
              <span>{b.timeAmount.toLocaleString()}Đ</span>
            </div>
            {b.fbAmount > 0 && (
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.9rem' }}>
                <span>Đồ uống (F&amp;B)</span>
                <span>{b.fbAmount.toLocaleString()}Đ</span>
              </div>
            )}
            <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.9rem' }}>
              <span>Tạm tính</span>
              <span>{b.subtotal.toLocaleString()}Đ</span>
            </div>
            {b.depositApplied > 0 && (
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.9rem', color: '#16a34a', fontWeight: 500 }}>
                <span>✓ Trừ cọc đã thanh toán</span>
                <span>-{b.depositApplied.toLocaleString()}Đ</span>
              </div>
            )}
            <div style={{ borderTop: '2px dashed #cbd5e1', marginTop: '4px', paddingTop: '12px', display: 'flex', justifyContent: 'space-between', fontSize: '1.2rem', fontWeight: 'bold' }}>
              <span>DỰ KIẾN CẦN THU</span>
              <span style={{ color: b.finalTotal === 0 ? '#22c55e' : '#2563eb' }}>{b.finalTotal.toLocaleString()}Đ</span>
            </div>
            {b.refundAmount > 0 && (
              <div style={{ marginTop: '6px', padding: '8px 12px', background: '#fff7ed', border: '1px solid #fed7aa', borderRadius: '6px', color: '#c2410c', fontWeight: 600, fontSize: '0.85rem' }}>
                ⚠️ Dự kiến hoàn trả khách: {b.refundAmount.toLocaleString()}Đ
              </div>
            )}
          </div>

          <p style={{ fontSize: '0.8rem', color: '#94a3b8', margin: 0, textAlign: 'center' }}>
            Số liệu cuối cùng sẽ được chốt lại theo máy chủ sau khi kết thúc phiên.
          </p>

          {/* Hai lựa chọn thu tiền sau khi chốt phiên. */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', marginTop: '8px' }}>
            <button onClick={handleConfirmClose} disabled={processing} style={{ padding: '12px', background: '#22c55e', color: 'white', border: 'none', borderRadius: '6px', fontWeight: 600, cursor: 'pointer', opacity: processing ? 0.7 : 1 }}>
              {processing ? 'Đang chốt phiên...' : 'Chốt & Thu tiền tại POS'}
            </button>
            <button onClick={handleConfirmCloseAndOpenInvoice} disabled={processing} style={{ padding: '12px', background: 'white', color: '#2563eb', border: '1px solid #bfdbfe', borderRadius: '6px', fontWeight: 600, cursor: 'pointer', opacity: processing ? 0.7 : 1, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem' }}>
              🧾 Chốt &amp; Mở trang hóa đơn
            </button>
            <button onClick={handleCancel} disabled={processing} style={{ padding: '10px', background: 'transparent', color: '#64748b', border: 'none', borderRadius: '6px', fontWeight: 600, cursor: 'pointer' }}>Quay lại</button>
          </div>
        </div>
      </Modal>
    );
  }

  // ----- Bước PAYING (đã đóng phiên, thu tiền) -----
  const remaining = invoiceData?.remainingAmount ?? invoiceData?.grandTotalAmount ?? 0;
  const selectedMethod = paymentMethods.find((m) => m.paymentMethodId === selectedPaymentMethod);
  const bankTransfer = isBankTransferMethod(selectedMethod);

  return (
    <Modal title="Thanh Toán Hóa Đơn" onClose={handleCancel} size="medium">
      {loadingInvoice || !invoiceData ? (
        <div className="text-center py-8 text-gray-500">Đang tải hóa đơn...</div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <div style={{ textAlign: 'center', marginBottom: '8px' }}>
            <h3 style={{ fontSize: '1.25rem', margin: 0 }}>Hóa Đơn Tổng Hợp</h3>
            <p style={{ color: '#64748b', fontSize: '0.9rem', margin: '4px 0 0' }}>Bàn: {tableName} - Mã HĐ: {invoiceData.invoiceCode || `#${invoiceData.invoiceId}`}</p>
          </div>

          <div style={{ background: '#f8fafc', padding: '16px', borderRadius: '8px', border: '1px solid #e2e8f0' }}>
            <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
              {Object.values((invoiceData.lines || []).reduce((acc: Record<string, any>, l: any) => {
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
              <span style={{ color: remaining === 0 ? '#22c55e' : '#2563eb' }}>{remaining.toLocaleString()}Đ</span>
            </div>

            {(invoiceData.depositRefundAmount ?? 0) > 0 && (
              <div style={{ marginTop: '10px', padding: '8px 12px', background: '#fff7ed', border: '1px solid #fed7aa', borderRadius: '6px', color: '#c2410c', fontWeight: 600 }}>
                ⚠️ Cần hoàn trả khách: {(invoiceData.depositRefundAmount ?? 0).toLocaleString()}Đ
              </div>
            )}
          </div>

          {remaining > 0 && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
              <label style={{ fontWeight: 500 }}>Phương thức thanh toán:</label>
              <select
                value={selectedPaymentMethod}
                onChange={(e) => setSelectedPaymentMethod(Number(e.target.value))}
                style={{ padding: '10px', borderRadius: '6px', border: '1px solid #cbd5e1', width: '100%', fontSize: '1rem', outline: 'none' }}
              >
                {paymentMethods.map((m) => (
                  <option key={m.paymentMethodId} value={m.paymentMethodId}>{m.name}</option>
                ))}
              </select>
            </div>
          )}

          {remaining > 0 && bankTransfer && (
            <div style={{ marginTop: '8px', textAlign: 'center' }}>
              <p style={{ fontSize: '0.9rem', color: '#475569', marginBottom: '8px', fontWeight: 500 }}>Quét mã QR để thanh toán nhanh</p>
              <img
                src={`${API_BASE_URL}/api/invoices/${invoiceData.invoiceId}/qr-code?amt=${invoiceData.grandTotalAmount || 0}&t=${refreshTrigger}`}
                alt="QR Code"
                style={{ width: '220px', height: '220px', objectFit: 'contain', border: '1px solid #e2e8f0', borderRadius: '8px', padding: '8px', background: 'white' }}
              />
              <p style={{ fontSize: '0.85rem', color: '#16a34a', marginTop: '12px', fontWeight: 500, animation: 'pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite' }}>
                ⏳ Hệ thống sẽ tự động xác nhận khi thanh toán thành công...
              </p>
            </div>
          )}

          {/* Mở đúng trang Quản lý Hóa đơn để thu tiền y hệt (giảm giá, QR, in bill...). POS vẫn tự
              phát hiện khi hóa đơn được thanh toán ở trang đó (qua SignalR) và đóng lại. */}
          {invoiceId != null && (
            <button
              onClick={() => openInvoicePage(invoiceId)}
              style={{ padding: '10px', background: 'white', color: '#2563eb', border: '1px solid #bfdbfe', borderRadius: '6px', fontWeight: 600, cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem' }}
            >
              🧾 Mở trang hóa đơn để thu tiền
            </button>
          )}

          <div style={{ display: 'flex', gap: '12px', marginTop: '4px' }}>
            <button onClick={handleCancel} disabled={processing} style={{ flex: 1, padding: '12px', background: '#e2e8f0', color: '#475569', border: 'none', borderRadius: '6px', fontWeight: 600, cursor: 'pointer' }}>Hủy Bỏ</button>

            {/* Chuyển khoản: ẩn nút xác nhận, chờ hệ thống tự confirm qua QR. */}
            {!(bankTransfer && remaining > 0) && (
              <button onClick={handleConfirmPayment} disabled={processing} style={{ flex: 2, padding: '12px', background: '#22c55e', color: 'white', border: 'none', borderRadius: '6px', fontWeight: 600, cursor: 'pointer', opacity: processing ? 0.7 : 1 }}>
                {processing ? 'Đang xử lý...' : remaining === 0 ? 'Hoàn Tất Miễn Thu' : 'Xác Nhận Thu Tiền'}
              </button>
            )}
          </div>
        </div>
      )}
    </Modal>
  );
}
