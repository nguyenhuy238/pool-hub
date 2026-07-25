"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { API_BASE_URL } from "@/lib/api/client";
import { invoiceApi } from "@/lib/api/endpoints";
import { dateTime, money } from "@/lib/status";
import type { Invoice, InvoiceLine } from "@/types";

type InvoiceWithIssuedAt = Invoice & { issuedAtUtc?: string };

const invoicePaymentStatus: Record<number, string> = {
  1: "Chưa thanh toán",
  2: "Chưa thanh toán",
  3: "Đã thanh toán"
};

function numberValue(value: unknown) {
  const result = Number(value);
  return Number.isFinite(result) ? result : 0;
}

function lineTypeLabel(line: InvoiceLine) {
  return String(line.lineType || "").toUpperCase() === "TIME" ? "Tiền giờ" : "Dịch vụ";
}

function lineQuantityLabel(line: InvoiceLine) {
  if (String(line.lineType || "").toUpperCase() === "TIME") {
    return `${Math.round(numberValue(line.quantity) * 60).toLocaleString("vi-VN")} phút`;
  }
  return numberValue(line.quantity).toLocaleString("vi-VN");
}

function lineUnitPriceLabel(line: InvoiceLine) {
  const value = money(numberValue(line.unitPrice));
  return String(line.lineType || "").toUpperCase() === "TIME" ? `${value}/giờ` : value;
}

/**
 * A deliberately standalone invoice view. It is outside the authenticated
 * application layout so a POS operator can open it in a second monitor/window
 * without exposing the admin navigation.
 *
 * The invoice detail request still uses the operator's auth cookie. The QR
 * image endpoint is anonymous, so the customer can scan it from this view.
 */
export default function InvoiceDisplayPage({ params }: { params: { id: string } }) {
  const invoiceId = Number(params.id);
  const [invoice, setInvoice] = useState<InvoiceWithIssuedAt | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  const load = useCallback(async (showLoading = false) => {
    if (!Number.isInteger(invoiceId) || invoiceId <= 0) {
      setError("Mã hóa đơn không hợp lệ.");
      setLoading(false);
      return;
    }

    if (showLoading) setLoading(true);
    try {
      const detail = await invoiceApi.detail(invoiceId);
      setInvoice(detail as InvoiceWithIssuedAt);
      setError("");
      setLastUpdated(new Date());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể tải hóa đơn.");
    } finally {
      setLoading(false);
    }
  }, [invoiceId]);

  useEffect(() => {
    void load(true);
  }, [load]);

  // Keep the customer-facing display in sync while a bank transfer is pending.
  useEffect(() => {
    if (!invoice) return;
    const remaining = numberValue(invoice.remainingAmount ?? numberValue(invoice.grandTotalAmount) - numberValue(invoice.paidAmount));
    const isFinal = Number(invoice.paymentStatus) === 3 || Number(invoice.status) === 3 || remaining <= 0;
    if (isFinal) return;

    const timer = window.setInterval(() => void load(), 2000);
    return () => window.clearInterval(timer);
  }, [invoice, load]);

  const remaining = useMemo(() => {
    if (!invoice) return 0;
    return Math.max(
      0,
      numberValue(invoice.remainingAmount ?? numberValue(invoice.grandTotalAmount) - numberValue(invoice.paidAmount))
    );
  }, [invoice]);
  const qrUrl = useMemo(() => {
    if (!invoice || remaining <= 0) return "";
    // Keep the URL stable while the amount/status is unchanged. The display
    // polls every two seconds; using Date.now() here would force a fresh QR
    // request (and potentially a new PayOS order) on every poll.
    return `${API_BASE_URL}/api/invoices/${invoice.invoiceId}/qr-code?amt=${Math.round(remaining)}&status=${Number(invoice.paymentStatus || 0)}`;
  }, [invoice, remaining]);

  const status = invoice
    ? Number(invoice.status) === 3
      ? { label: "Đã hủy", tone: "cancelled" }
      : { label: invoicePaymentStatus[Number(invoice.paymentStatus)] || "Chờ thanh toán", tone: Number(invoice.paymentStatus) === 3 || remaining <= 0 ? "paid" : "pending" }
    : null;

  if (loading && !invoice) {
    return <main className="invoice-display-page"><div className="invoice-display-card invoice-display-state">Đang tải hóa đơn…</div></main>;
  }

  if (!invoice) {
    return (
      <main className="invoice-display-page">
        <div className="invoice-display-card invoice-display-state">
          <h1>Không tải được hóa đơn</h1>
          <p>{error || "Hóa đơn không tồn tại hoặc phiên đăng nhập đã hết hạn."}</p>
          <button className="invoice-display-button" onClick={() => void load(true)}>Thử lại</button>
        </div>
      </main>
    );
  }

  const lines = invoice.lines || [];
  const timeSubtotal = numberValue(invoice.timeSubtotalAmount);
  const productSubtotal = numberValue(invoice.productSubtotalAmount);
  const subtotal = numberValue(invoice.subtotalAmount ?? timeSubtotal + productSubtotal);
  const discount = numberValue(invoice.discountAmount);
  const deposit = numberValue(invoice.depositAppliedAmount);
  const paid = numberValue(invoice.paidAmount);
  const total = numberValue(invoice.grandTotalAmount);

  return (
    <main className="invoice-display-page">
      <div className="invoice-display-card">
        <header className="invoice-display-header">
          <div>
            <p className="invoice-display-eyebrow">HÓA ĐƠN THANH TOÁN</p>
            <h1>{invoice.invoiceCode || `INV-${invoice.invoiceId}`}</h1>
            <p className="invoice-display-meta">
              Phiên chơi #{invoice.sessionId}
              {invoice.issuedAtUtc ? ` · ${dateTime(invoice.issuedAtUtc)}` : ""}
            </p>
          </div>
          {status ? <span className={`invoice-display-status ${status.tone}`}>{status.label}</span> : null}
        </header>

        <section className="invoice-display-section">
          <h2>Chi tiết tính tiền</h2>
          <div className="invoice-display-table-wrap">
            <table className="invoice-display-table">
              <thead>
                <tr><th>Loại</th><th>Diễn giải</th><th className="number">SL</th><th className="number">Đơn giá</th><th className="number">Thành tiền</th></tr>
              </thead>
              <tbody>
                {lines.length ? lines.map((line) => (
                  <tr key={line.invoiceLineId}>
                    <td><span className={`invoice-display-line-type ${String(line.lineType).toUpperCase() === "TIME" ? "time" : "service"}`}>{lineTypeLabel(line)}</span></td>
                    <td>{line.description}</td>
                    <td className="number">{lineQuantityLabel(line)}</td>
                    <td className="number">{lineUnitPriceLabel(line)}</td>
                    <td className="number strong">{money(numberValue(line.lineTotalAmount))}</td>
                  </tr>
                )) : (
                  <tr><td colSpan={5} className="invoice-display-empty">Chưa có dòng tính tiền.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </section>

        <section className="invoice-display-total">
          <div><span>Tiền giờ chơi</span><strong>{money(timeSubtotal)}</strong></div>
          <div><span>Tiền dịch vụ / sản phẩm</span><strong>{money(productSubtotal)}</strong></div>
          {subtotal !== timeSubtotal + productSubtotal ? <div><span>Tạm tính</span><strong>{money(subtotal)}</strong></div> : null}
          {discount > 0 ? <div className="discount"><span>Giảm giá</span><strong>-{money(discount)}</strong></div> : null}
          {deposit > 0 ? <div className="deposit"><span>Đã đặt cọc</span><strong>-{money(deposit)}</strong></div> : null}
          <div className="invoice-display-total-main"><span>TỔNG THANH TOÁN</span><strong>{money(total)}</strong></div>
          <div><span>Đã thanh toán</span><strong>{money(paid)}</strong></div>
          <div className="invoice-display-remaining"><span>Còn phải trả</span><strong>{money(remaining)}</strong></div>
        </section>

        {remaining > 0 && Number(invoice.status) !== 3 ? (
          <section className="invoice-display-payment">
            <div>
              <h2>Quét mã để thanh toán</h2>
              <p>Khách hàng mở ứng dụng ngân hàng và quét mã QR bên cạnh.</p>
              <p className="invoice-display-payment-amount">Số tiền cần trả: <strong>{money(remaining)}</strong></p>
              {lastUpdated ? <p className="invoice-display-updated">Tự động cập nhật · {lastUpdated.toLocaleTimeString("vi-VN")}</p> : null}
            </div>
            <img
              className="invoice-display-qr"
              src={qrUrl}
              alt={`Mã QR thanh toán ${invoice.invoiceCode || invoice.invoiceId}`}
            />
          </section>
        ) : (
          <section className="invoice-display-paid">
            <span aria-hidden="true">✓</span>
            <div><strong>{Number(invoice.status) === 3 ? "Hóa đơn đã hủy" : "Hóa đơn đã thanh toán đủ"}</strong><p>Cảm ơn quý khách.</p></div>
          </section>
        )}

        {error ? <p className="invoice-display-error">{error}</p> : null}
      </div>
      <style jsx>{`
        .invoice-display-page { min-height: 100vh; background: #eef3f1; padding: 24px; color: #172b27; }
        .invoice-display-card { max-width: 980px; margin: 0 auto; background: #fff; border: 1px solid #d7e3df; border-radius: 16px; box-shadow: 0 12px 34px rgba(17, 58, 47, .1); overflow: hidden; }
        .invoice-display-header { display:flex; align-items:flex-start; justify-content:space-between; gap:20px; padding:26px 30px 22px; border-bottom:1px solid #d7e3df; }
        .invoice-display-eyebrow { margin:0 0 7px; color:#647a73; font-size:12px; letter-spacing:.12em; font-weight:700; }
        h1 { margin:0; font-size:27px; letter-spacing:.02em; }
        h2 { margin:0 0 14px; font-size:16px; }
        .invoice-display-meta { margin:7px 0 0; color:#6b7f78; font-size:13px; }
        .invoice-display-status { border-radius:999px; padding:8px 13px; font-size:13px; font-weight:700; white-space:nowrap; }
        .invoice-display-status.pending { background:#fff1cc; color:#8b5c00; }
        .invoice-display-status.paid { background:#dcf8e8; color:#087442; }
        .invoice-display-status.cancelled { background:#ffe1e1; color:#a22c2c; }
        .invoice-display-section { padding:24px 30px 0; }
        .invoice-display-table-wrap { border:1px solid #d7e3df; border-radius:10px; overflow:auto; }
        .invoice-display-table { width:100%; border-collapse:collapse; min-width:650px; font-size:14px; }
        .invoice-display-table th { background:#f5f9f7; color:#60756d; font-size:12px; text-transform:uppercase; letter-spacing:.02em; text-align:left; }
        .invoice-display-table th, .invoice-display-table td { padding:13px 14px; border-bottom:1px solid #e4ece9; }
        .invoice-display-table tbody tr:last-child td { border-bottom:0; }
        .invoice-display-table .number { text-align:right; white-space:nowrap; }
        .invoice-display-table .strong { font-weight:700; color:#183b32; }
        .invoice-display-line-type { display:inline-block; border-radius:999px; padding:5px 9px; font-size:12px; font-weight:700; }
        .invoice-display-line-type.time { color:#2067b1; background:#e5f0ff; }
        .invoice-display-line-type.service { color:#6c38a7; background:#f0e5ff; }
        .invoice-display-empty { text-align:center; color:#748780; padding:24px !important; }
        .invoice-display-total { margin:24px 30px 0; padding:18px 20px; border:1px solid #d7e3df; border-radius:10px; background:#f5f9f7; }
        .invoice-display-total > div { display:flex; justify-content:space-between; gap:20px; padding:5px 0; font-size:14px; }
        .invoice-display-total-main { margin-top:9px; padding-top:13px !important; border-top:1px dashed #c9d8d3; font-size:18px !important; font-weight:800; }
        .invoice-display-total-main strong { color:#08705b; font-size:23px; }
        .invoice-display-remaining { font-size:16px !important; font-weight:700; }
        .invoice-display-remaining strong { font-size:18px; }
        .invoice-display-total .discount strong { color:#bb3030; }
        .invoice-display-total .deposit strong { color:#08705b; }
        .invoice-display-payment { display:flex; align-items:center; justify-content:space-between; gap:24px; margin:24px 30px 26px; padding:20px; border:1px solid #a9efc4; border-radius:12px; background:#effff4; }
        .invoice-display-payment p { margin:6px 0; color:#517066; font-size:13px; }
        .invoice-display-payment-amount { color:#175f40 !important; font-size:15px !important; }
        .invoice-display-updated { font-size:11px !important; color:#719086 !important; }
        .invoice-display-qr { display:block; flex:0 0 auto; width:190px; height:190px; margin:0; object-fit:contain; background:#fff; border:1px solid #d3e2dc; border-radius:8px; padding:8px; }
        .invoice-display-paid { display:flex; align-items:center; gap:14px; margin:24px 30px 26px; padding:18px 20px; border-radius:10px; background:#effff4; color:#087442; }
        .invoice-display-paid > span { display:grid; place-items:center; width:38px; height:38px; border-radius:50%; background:#c8f5d9; font-size:25px; font-weight:800; }
        .invoice-display-paid p { margin:4px 0 0; color:#55766a; font-size:13px; }
        .invoice-display-error { margin:16px 30px 0; color:#a22c2c; font-size:13px; }
        .invoice-display-button { border:0; border-radius:8px; padding:10px 16px; background:#287bea; color:white; cursor:pointer; font-weight:700; }
        .invoice-display-state { min-height:220px; display:grid; place-items:center; align-content:center; gap:12px; padding:30px; text-align:center; }
        .invoice-display-state h1 { font-size:21px; }
        .invoice-display-state p { max-width:450px; margin:0; color:#657a72; }
        @media (max-width: 680px) {
          .invoice-display-page { padding:0; }
          .invoice-display-card { border-radius:0; border-left:0; border-right:0; }
          .invoice-display-header, .invoice-display-section { padding-left:16px; padding-right:16px; }
          .invoice-display-header { flex-direction:column; }
          .invoice-display-total, .invoice-display-payment, .invoice-display-paid { margin-left:16px; margin-right:16px; }
          .invoice-display-payment { flex-direction:column; align-items:stretch; text-align:center; }
          .invoice-display-qr { align-self:center; }
        }
        @media print {
          .invoice-display-page { background:#fff; padding:0; }
          .invoice-display-card { border:0; box-shadow:none; max-width:none; }
        }
      `}</style>
    </main>
  );
}
