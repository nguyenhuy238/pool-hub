"use client";

import { useCallback, useEffect, useState } from "react";
import { invoiceApi, sessionApi } from "@/lib/api/endpoints";
import { getTotalPages, API_BASE_URL } from "@/lib/api/client";
import { customerReviewsApi } from "@/lib/api/customerReviewsApi";
import { money, dateTime } from "@/lib/status";
import { ConfirmDialog, DataTable, ListControls, PageHeader, StateBlock, useList, useLoad, Modal, Pagination, SearchableSelect } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { Invoice, PaymentMethod, ReviewInvitationLink, Session } from "@/types";

export default function InvoicesPage() {
  const toast = useToast();
  const [selectedSessionId, setSelectedSessionId] = useState("");
  const [creatingInvoice, setCreatingInvoice] = useState(false);
  const [invoice, setInvoice] = useState<Invoice | null>(null);
  const [reviewInvitation, setReviewInvitation] = useState<ReviewInvitationLink | null>(null);
  const [paymentMethodId, setPaymentMethodId] = useState<number | "">("");
  const [confirmPayment, setConfirmPayment] = useState(false);
  const [cancelReason, setCancelReason] = useState("");
  const [cancelOpen, setCancelOpen] = useState(false);
  const [discountOpen, setDiscountOpen] = useState(false);
  const [discountCode, setDiscountCode] = useState("");
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20, paymentStatus: "" });
  const { data, loading, error, reload } = useLoad(async () => {
    const [invoices, methods, sessions, allInvoices] = await Promise.all([
      invoiceApi.list({ Search: params.search, PageNumber: params.pageNumber, PageSize: params.pageSize, PaymentStatus: params.paymentStatus ? Number(params.paymentStatus) : undefined }),
      invoiceApi.paymentMethods(),
      sessionApi.list({ PageSize: 100 }),
      invoiceApi.list({ PageSize: 1000 })
    ]);
    return { invoices, methods, sessions, allInvoices };
  }, [params]);
  const invoices = useList<Invoice>(data?.invoices);
  const methods = (data?.methods || []) as PaymentMethod[];
  const sessions = useList<Session>(data?.sessions);
  const allInvoices = useList<Invoice>(data?.allInvoices);

  const paidSessionIds = new Set(
    allInvoices
      .filter((inv) => Number(inv.paymentStatus) === 3)
      .map((inv) => Number(inv.sessionId))
  );
  const unpaidSessions = sessions.filter((s) => !paidSessionIds.has(Number(s.sessionId)));

  const sessionOptions = unpaidSessions.map((s) => {
    const rawTable = s.tableName?.trim() || "";
    const tableStr = rawTable ? (/^(bàn|table)/i.test(rawTable) ? rawTable : `Bàn ${rawTable}`) : "Bàn";
    const codeStr = s.sessionCode || `#${s.sessionId}`;
    const labelStr = Number(s.status) === 1
      ? `${tableStr} - ${codeStr} - Đang chơi ${s.durationMinutes ?? 0} phút`
      : `${tableStr} - ${codeStr} - Đã kết thúc - ${dateTime(String(s.endedAtUtc || s.startedAtUtc))}`;
    return { value: String(s.sessionId), label: labelStr };
  });

  const loadDetail = useCallback(async (id: number) => {
    setInvoice(await invoiceApi.detail(id));
    setReviewInvitation(null);
  }, []);

  useEffect(() => {
    const invoiceId = Number(new URLSearchParams(window.location.search).get("invoiceId"));
    if (!Number.isFinite(invoiceId) || invoiceId <= 0) return;

    loadDetail(invoiceId).catch((err) => toast(err.message || "Không tải được hóa đơn.", "error"));
  }, [loadDetail, toast]);

  async function pay() {
    if (!invoice) return;
    if (!paymentMethodId) {
      toast("Vui lòng chọn phương thức thanh toán.", "error");
      return;
    }
    try {
      const payment = await invoiceApi.pay({ invoiceId: invoice.invoiceId, paymentMethodId: Number(paymentMethodId), amount: invoice.grandTotalAmount || 0 });
      toast("Đã ghi nhận thanh toán.", "success");
      const detail = await invoiceApi.detail(invoice.invoiceId);
      setReviewInvitation(payment.reviewInvitation || null);
      setInvoice({ ...detail, reviewInvitation: payment.reviewInvitation });
      await reload();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể ghi nhận thanh toán.", "error");
      await loadDetail(invoice.invoiceId);
    }
  }

  async function cancelInvoice() {
    if (!invoice || !cancelReason.trim()) {
      toast("Vui lòng nhập lý do hủy hóa đơn.", "error");
      return;
    }
    await invoiceApi.cancel(invoice.invoiceId, cancelReason.trim())
      .then(async () => {
        toast("Hóa đơn đã bị hủy.", "success");
        setCancelOpen(false);
        setCancelReason("");
        await loadDetail(invoice.invoiceId);
        reload();
      })
      .catch(err => toast(err.message, "error"));
  }

  async function applyDiscount() {
    if (!invoice || !discountCode.trim()) {
      toast("Vui lòng nhập mã giảm giá.", "error");
      return;
    }
    await invoiceApi.discount(invoice.invoiceId, discountCode.trim())
      .then(async () => {
        toast("Đã áp dụng mã giảm giá.", "success");
        setDiscountOpen(false);
        setDiscountCode("");
        await loadDetail(invoice.invoiceId);
        reload();
      })
      .catch(err => toast(err.message, "error"));
  }

  async function removeDiscount() {
    if (!invoice) return;
    await invoiceApi.removeDiscount(invoice.invoiceId)
      .then(async () => {
        toast("Đã gỡ bỏ mã giảm giá.", "success");
        await loadDetail(invoice.invoiceId);
        reload();
      })
      .catch(err => toast(err.message, "error"));
  }

  return (
    <>
      <PageHeader title="Hóa đơn và thanh toán" description="Tạo hóa đơn, xem chi tiết và ghi nhận thanh toán." />
      <ListControls
        search={params.search}
        pageNumber={params.pageNumber}
        pageSize={params.pageSize}
        onChange={(next) => setParams((prev) => ({ ...prev, ...next }))}
        extra={
          <label>
            <span>Trạng thái</span>
            <select value={params.paymentStatus} onChange={(e) => setParams((prev) => ({ ...prev, paymentStatus: e.target.value, pageNumber: 1 }))}>
              <option value="">Tất cả</option>
              <option value="1">Chưa thanh toán</option>
              <option value="2">Đã thanh toán</option>
            </select>
          </label>
        }
      />
      <form className="card" style={{ display: "flex", flexWrap: "wrap", gap: "16px", alignItems: "flex-end", marginBottom: "18px" }} onSubmit={async (e) => {
        e.preventDefault();
        if (!selectedSessionId) {
          toast("Vui lòng chọn phiên chơi.", "error");
          return;
        }
        setCreatingInvoice(true);
        try {
          const created = await invoiceApi.generate(Number(selectedSessionId));
          setInvoice(await invoiceApi.detail(created.invoiceId));
          reload();
          toast("Thao tác thành công.", "success");
        } catch (err) {
          const message = err instanceof Error ? err.message : "Thao tác thất bại.";
          toast(message, "error");
        } finally {
          setCreatingInvoice(false);
        }
      }}>
        <h2 style={{ width: "100%", margin: "0 0 4px" }}>Tạo hóa đơn từ phiên chơi</h2>
        <div style={{ display: "flex", flexDirection: "column", gap: "6px", width: "500px", maxWidth: "100%" }}>
          <span>Chọn phiên chơi</span>
          <SearchableSelect
            options={sessionOptions}
            value={selectedSessionId}
            onChange={setSelectedSessionId}
            placeholder="-- Chọn hoặc tìm kiếm phiên chơi --"
            disabled={creatingInvoice}
          />
        </div>
        <div>
          <button type="submit" className="primary-btn" disabled={creatingInvoice || !selectedSessionId}>
            {creatingInvoice ? "Đang tạo hóa đơn..." : "Tạo hóa đơn"}
          </button>
        </div>
      </form>
      <StateBlock loading={loading} error={error} empty={!loading && !invoices.length} />
      <DataTable
        rows={invoices as unknown as Record<string, unknown>[]}
        columns={[
          { key: "invoiceCode", label: "Mã" },
          { key: "sessionId", label: "Phiên chơi" },
          { key: "grandTotalAmount", label: "Tổng tiền", render: (row) => <strong>{money(Number(row.grandTotalAmount || 0))}</strong> },
          { key: "paymentStatus", label: "Trạng thái thanh toán", render: (row) => Number(row.paymentStatus) === 3 ? <span className="badge green">Đã thanh toán</span> : <span className="badge yellow">Chưa thanh toán</span> }
        ]}
        actions={(row) => <button className="ghost-btn" onClick={() => loadDetail(Number(row.invoiceId)).catch((err) => toast(err.message, "error"))}>Chi tiết</button>}
      />
      <Pagination
        pageNumber={params.pageNumber}
        totalPages={getTotalPages(data?.invoices, params.pageSize)}
        onChange={(page) => setParams((prev) => ({ ...prev, pageNumber: page }))}
      />
      {invoice ? (
        <Modal title={invoice.invoiceCode || `Hóa đơn #${invoice.invoiceId}`} onClose={() => setInvoice(null)} size="large">
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '16px', paddingBottom: '16px', borderBottom: '1px solid var(--line)' }}>
            <div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '6px' }}>
                <span style={{ fontSize: '18px', fontWeight: 'bold' }}>{invoice.invoiceCode || `Hóa đơn #${invoice.invoiceId}`}</span>
                {Number(invoice.paymentStatus) === 3 ? <span className="badge green" style={{ fontSize: '13px', padding: '4px 10px' }}>ĐÃ THANH TOÁN</span> : Number(invoice.status) === 3 ? <span className="badge red" style={{ fontSize: '13px', padding: '4px 10px' }}>ĐÃ HỦY</span> : <span className="badge yellow" style={{ fontSize: '13px', padding: '4px 10px' }}>CHƯA THANH TOÁN</span>}
              </div>
              <p style={{ fontSize: '13px', color: 'var(--muted)', margin: 0 }}>Mã phiên chơi: #{invoice.sessionId}</p>
            </div>
            <button className="ghost-btn" onClick={() => {
              const invitation = reviewInvitation || invoice.reviewInvitation;
              const linesHtml = (invoice.lines || []).map(l => `
                <tr>
                  <td style="padding: 8px; border-bottom: 1px solid #eee;">${l.lineType === 'TIME' ? 'Tiền giờ bàn' : 'Dịch vụ/Sản phẩm'}</td>
                  <td style="padding: 8px; border-bottom: 1px solid #eee;">${l.description}</td>
                  <td style="padding: 8px; border-bottom: 1px solid #eee; text-align: center;">${l.lineType === 'TIME' ? `${Math.round(Number(l.quantity) * 60)} phút` : l.quantity}</td>
                  <td style="padding: 8px; border-bottom: 1px solid #eee; text-align: right;">${l.lineType === 'TIME' ? `${money(l.unitPrice)}/giờ` : money(l.unitPrice)}</td>
                  <td style="padding: 8px; border-bottom: 1px solid #eee; text-align: right;">${money(l.lineTotalAmount)}</td>
                </tr>
              `).join('');

              const printWindow = window.open("", "_blank");
              if (printWindow) {
                printWindow.document.write(`<html><head><title>Hoa don ${invoice.invoiceCode || invoice.invoiceId}</title></head><body style="font-family: Arial, sans-serif; padding: 30px; max-width: 700px; margin: 0 auto; color: #333;">
                    <h1 style="text-align:center; margin-bottom: 4px;">HÓA ĐƠN THANH TOÁN</h1>
                    <h3 style="text-align:center; color: #666; margin-top: 0; font-weight: normal;">Mã: ${invoice.invoiceCode || invoice.invoiceId} (Phiên: #${invoice.sessionId})</h3>
                    <hr style="border: 1px dashed #ccc; margin: 20px 0;"/>
                    <table style="width: 100%; border-collapse: collapse; margin-bottom: 20px; font-size: 14px;">
                      <thead>
                        <tr style="background: #f8f9fa; text-align: left;">
                          <th style="padding: 8px; border-bottom: 2px solid #ddd;">Loại</th>
                          <th style="padding: 8px; border-bottom: 2px solid #ddd;">Diễn giải</th>
                          <th style="padding: 8px; border-bottom: 2px solid #ddd; text-align: center;">SL</th>
                          <th style="padding: 8px; border-bottom: 2px solid #ddd; text-align: right;">Đơn giá</th>
                          <th style="padding: 8px; border-bottom: 2px solid #ddd; text-align: right;">Thành tiền</th>
                        </tr>
                      </thead>
                      <tbody>${linesHtml || '<tr><td colspan="5" style="text-align:center; padding: 12px;">Không có dữ liệu chi tiết</td></tr>'}</tbody>
                    </table>
                    <hr style="border: 1px dashed #ccc; margin: 20px 0;"/>
                    <div style="display:flex; justify-content:space-between; margin-bottom: 8px; font-size: 15px;"><span>Tổng tiền giờ chơi:</span> <span>${money(invoice.timeSubtotalAmount || 0)}</span></div>
                    <div style="display:flex; justify-content:space-between; margin-bottom: 8px; font-size: 15px;"><span>Tổng tiền dịch vụ/sản phẩm:</span> <span>${money(invoice.productSubtotalAmount || 0)}</span></div>
                    ${(invoice.discountAmount || 0) > 0 ? `<div style="display:flex; justify-content:space-between; margin-bottom: 8px; font-size: 15px; color: #d9534f;"><span>Giảm giá:</span> <span>-${money(invoice.discountAmount || 0)}</span></div>` : ''}
                    <hr style="border: 1px solid #333; margin: 16px 0;"/>
                    <div style="display:flex; justify-content:space-between; font-size: 20px; font-weight: bold;"><span>TỔNG THANH TOÁN:</span> <span>${money(invoice.grandTotalAmount || 0)}</span></div>
                    ${invitation?.reviewUrl ? `<div style="margin: 26px auto 0; text-align:center; padding: 16px; border: 1px solid #ddd; border-radius: 10px; max-width: 360px;">
                      <strong>Mời quý khách đánh giá trải nghiệm</strong>
                      <p style="font-size:12px; color:#666; word-break:break-all;">${invitation.reviewUrl}</p>
                      <img src="https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=${encodeURIComponent(invitation.reviewUrl)}" alt="QR đánh giá" style="width:150px;height:150px;" />
                    </div>` : ''}
                    <p style="text-align:center; margin-top: 40px; font-style: italic; color: #777;">Cảm ơn quý khách và hẹn gặp lại!</p>
                    <script>setTimeout(() => window.print(), 500);</script>
                  </body></html>`);
                printWindow.document.close();
              }
            }}>🖨️ In bill chi tiết</button>
          </div>

          <div style={{ marginBottom: '24px' }}>
            <h4 style={{ margin: '0 0 12px', fontSize: '15px', fontWeight: 600 }}>Chi tiết mục tính tiền</h4>
            <div style={{ border: '1px solid var(--line)', borderRadius: '8px', overflow: 'hidden' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
                <thead style={{ background: 'var(--soft)', borderBottom: '1px solid var(--line)', textAlign: 'left' }}>
                  <tr>
                    <th style={{ padding: '10px 14px' }}>Loại</th>
                    <th style={{ padding: '10px 14px' }}>Diễn giải</th>
                    <th style={{ padding: '10px 14px', textAlign: 'center' }}>SL</th>
                    <th style={{ padding: '10px 14px', textAlign: 'right' }}>Đơn giá</th>
                    <th style={{ padding: '10px 14px', textAlign: 'right' }}>Thành tiền</th>
                  </tr>
                </thead>
                <tbody>
                  {(invoice.lines && invoice.lines.length > 0) ? invoice.lines.map((l) => (
                    <tr key={l.invoiceLineId} style={{ borderBottom: '1px solid var(--line)' }}>
                      <td style={{ padding: '10px 14px' }}>
                        <span className={`badge ${l.lineType === 'TIME' ? 'blue' : 'purple'}`} style={{ fontSize: '12px' }}>
                          {l.lineType === 'TIME' ? 'Tiền giờ' : 'Dịch vụ'}
                        </span>
                      </td>
                      <td style={{ padding: '10px 14px', fontWeight: 500 }}>{l.description}</td>
                      <td style={{ padding: '10px 14px', textAlign: 'center' }}>{l.lineType === 'TIME' ? `${Math.round(Number(l.quantity) * 60)} phút` : l.quantity}</td>
                      <td style={{ padding: '10px 14px', textAlign: 'right', color: 'var(--muted)' }}>{l.lineType === 'TIME' ? `${money(l.unitPrice)}/giờ` : money(l.unitPrice)}</td>
                      <td style={{ padding: '10px 14px', textAlign: 'right', fontWeight: 600 }}>{money(l.lineTotalAmount)}</td>
                    </tr>
                  )) : (
                    <tr>
                      <td colSpan={5} style={{ padding: '24px', textAlign: 'center', color: 'var(--muted)' }}>Không có chi tiết dòng nào được tải</td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>

          {(invoice.discounts && invoice.discounts.length > 0) ? (
            <div style={{ marginBottom: '24px', background: '#fff9e6', border: '1px solid #ffe0b2', padding: '12px 16px', borderRadius: '8px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '6px' }}>
                <span style={{ fontWeight: 600, fontSize: '14px', color: '#b76e00' }}>🎟️ Khuyến mãi đã áp dụng:</span>
                {invoice.status !== 3 && invoice.paymentStatus !== 2 && (
                  <button type="button" className="ghost-btn" style={{ fontSize: '12px', padding: '2px 8px', color: '#d9534f', cursor: 'pointer' }} onClick={removeDiscount}>
                    🗑️ Gỡ mã
                  </button>
                )}
              </div>
              {invoice.discounts.map(d => (
                <div key={d.invoiceDiscountId} style={{ display: 'flex', justifyContent: 'space-between', fontSize: '14px', color: '#8c5400' }}>
                  <span>{d.descriptionSnapshot || `Mã giảm giá #${d.discountId}`}</span>
                  <span style={{ fontWeight: 'bold' }}>-{money(d.amountApplied)}</span>
                </div>
              ))}
            </div>
          ) : null}

          <div style={{ background: 'var(--soft)', padding: '16px 20px', borderRadius: '8px', border: '1px solid var(--line)', marginBottom: '24px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px', fontSize: '14px', color: 'var(--muted)' }}>
              <span>Tiền giờ chơi:</span>
              <span style={{ fontWeight: 500, color: 'var(--text)' }}>{money(invoice.timeSubtotalAmount || 0)}</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px', fontSize: '14px', color: 'var(--muted)' }}>
              <span>Tiền dịch vụ / sản phẩm:</span>
              <span style={{ fontWeight: 500, color: 'var(--text)' }}>{money(invoice.productSubtotalAmount || 0)}</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '12px', fontSize: '14px', color: 'var(--muted)' }}>
              <span>Giảm giá:</span>
              <span style={{ fontWeight: 600, color: 'var(--danger)' }}>-{money(invoice.discountAmount || 0)}</span>
            </div>
            <div style={{ borderTop: '1px dashed var(--line)', paddingTop: '12px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <span style={{ fontSize: '16px', fontWeight: 'bold' }}>TỔNG THANH TOÁN:</span>
              <span style={{ fontSize: '22px', fontWeight: 'bold', color: 'var(--brand)' }}>{money(invoice.grandTotalAmount || 0)}</span>
            </div>
          </div>

          {Number(invoice.status) === 3 ? (
            <div className="state-card" style={{ background: '#fdeded', color: '#5f2120', border: '1px solid #f4c3c2', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              Hóa đơn này đã bị hủy.
            </div>
          ) : Number(invoice.paymentStatus) !== 3 ? (
            <div style={{ background: 'var(--soft)', padding: '16px', borderRadius: '12px', border: '1px solid var(--line)', display: 'flex', flexDirection: 'column', gap: '14px' }}>
              <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap', alignItems: 'center' }}>
                <select style={{ flex: '1 1 220px' }} value={paymentMethodId} onChange={(event) => setPaymentMethodId(Number(event.target.value))}>
                  <option value="">-- Chọn phương thức thanh toán --</option>
                  {methods.map((method) => <option key={method.paymentMethodId} value={method.paymentMethodId}>{method.name}</option>)}
                </select>
                <button className="primary-btn" onClick={() => setConfirmPayment(true)}>Thanh toán ngay</button>
                <button className="ghost-btn" onClick={() => setDiscountOpen(true)}>Áp dụng mã giảm giá</button>
                <button className="danger-btn" style={{ marginLeft: "auto" }} onClick={() => setCancelOpen(true)}>Hủy hóa đơn</button>
              </div>

              {(() => {
                const selected = methods.find(m => m.paymentMethodId === Number(paymentMethodId));
                const isBank = selected && (selected.code === "BANK" || selected.name.toLowerCase().includes("chuyển khoản") || selected.name.toLowerCase().includes("bank") || selected.name.toLowerCase().includes("qr") || selected.name.toLowerCase().includes("chuyen khuan"));
                if (!isBank) return null;

                const amount = invoice.grandTotalAmount || 0;
                const addInfo = `HD${invoice.invoiceId}`;
                let accountNo = "989420048989";
                let bankCode = "MB";
                let accountName = "TRAN CONG DINH";

                try {
                  if (selected.description && selected.description.startsWith("{")) {
                    const parsed = JSON.parse(selected.description);
                    if (parsed.vietqr || parsed.accountNo) {
                      if (parsed.accountNo) accountNo = parsed.accountNo;
                      if (parsed.bankCode) bankCode = parsed.bankCode;
                      if (parsed.accountName) accountName = parsed.accountName;
                    }
                  }
                } catch { }

                const qrUrl = `${API_BASE_URL}/api/invoices/${invoice.invoiceId}/qr-code?amt=${amount}&t=${Date.now()}`;

                return (
                  <div style={{ background: '#f8fbfa', border: '1.5px solid #0f5d4b', borderRadius: '12px', padding: '20px', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '14px' }}>
                    <div style={{ fontWeight: 700, fontSize: '15px', color: '#0f5d4b', display: 'flex', alignItems: 'center', gap: '6px' }}>
                      <span>⚡</span> QUÉT MÃ VIETQR ĐỂ THANH TOÁN TỰ ĐỘNG
                    </div>

                    <div style={{ background: 'white', padding: '12px', borderRadius: '12px', boxShadow: '0 4px 16px rgba(0,0,0,0.08)', border: '1px solid var(--line)' }}>
                      <img src={qrUrl} alt="VietQR Thanh Toán" style={{ width: '100%', maxWidth: '300px', display: 'block', borderRadius: '8px' }} />
                    </div>

                    <div style={{ width: '100%', fontSize: '13px', background: 'white', padding: '14px', borderRadius: '8px', border: '1px solid var(--line)', display: 'grid', gap: '8px' }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ color: 'var(--muted)' }}>Ngân hàng:</span>
                        <strong>{bankCode}</strong>
                      </div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ color: 'var(--muted)' }}>Số tài khoản:</span>
                        <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                          <strong style={{ color: 'var(--ink)', fontSize: '15px' }}>{accountNo}</strong>
                          <button type="button" className="ghost-btn compact" style={{ padding: '2px 8px', fontSize: '12px' }} onClick={() => { navigator.clipboard.writeText(accountNo); toast("Đã sao chép số tài khoản!", "success"); }}>📋 Sao chép</button>
                        </div>
                      </div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ color: 'var(--muted)' }}>Chủ tài khoản:</span>
                        <strong>{accountName}</strong>
                      </div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ color: 'var(--muted)' }}>Số tiền thanh toán:</span>
                        <strong style={{ color: '#0f5d4b', fontSize: '16px' }}>{money(amount)}</strong>
                      </div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderTop: '1px dashed var(--line)', paddingTop: '8px', marginTop: '4px' }}>
                        <span style={{ color: 'var(--muted)' }}>Nội dung chuyển khoản:</span>
                        <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                          <strong style={{ color: '#d9534f', fontSize: '15px' }}>{addInfo}</strong>
                          <button type="button" className="ghost-btn compact" style={{ padding: '2px 8px', fontSize: '12px' }} onClick={() => { navigator.clipboard.writeText(addInfo); toast("Đã sao chép nội dung!", "success"); }}>📋 Sao chép</button>
                        </div>
                      </div>
                    </div>

                    <p style={{ fontSize: '12px', color: 'var(--muted)', margin: 0, textAlign: 'center' }}>
                      💡 Khách hàng mở ứng dụng Ngân hàng hoặc Momo/ZaloPay quét mã trên. Số tiền và nội dung sẽ được tự động điền chính xác tuyệt đối.
                    </p>
                  </div>
                );
              })()}
            </div>
          ) : (
            <>
              <div className="state-card" style={{ background: '#e4f7ec', color: '#187344', border: '1px solid #c2ebd5', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                Hóa đơn này đã được thanh toán hoàn tất. Không thể sửa đổi hay thanh toán thêm.
              </div>
              <ReviewInvitationPanel
                invoiceId={invoice.invoiceId}
                invitation={reviewInvitation || invoice.reviewInvitation || null}
                onCreated={(value) => {
                  setReviewInvitation(value);
                  setInvoice((current) => current ? { ...current, reviewInvitation: value } : current);
                }}
              />
            </>
          )}
        </Modal>
      ) : null}
      {confirmPayment && invoice ? <ConfirmDialog title="Ghi nhận thanh toán" message={`Xác nhận thanh toán ${money(invoice.grandTotalAmount || 0)} cho hóa đơn này?`} confirmLabel="Thanh toán" onCancel={() => setConfirmPayment(false)} onConfirm={async () => { setConfirmPayment(false); await pay(); }} /> : null}
      {discountOpen && invoice ? <Modal title="Áp dụng mã giảm giá" onClose={() => setDiscountOpen(false)}>
        <div className="form-stack">
          <label><span>Mã giảm giá</span><input value={discountCode} onChange={(event) => setDiscountCode(event.target.value)} placeholder="Nhập mã giảm giá..." autoFocus /></label>
          <div className="modal-actions"><button className="ghost-btn" onClick={() => setDiscountOpen(false)}>Hủy</button><button className="primary-btn" onClick={applyDiscount}>Áp dụng</button></div>
        </div>
      </Modal> : null}
      {cancelOpen && invoice ? <Modal title="Hủy hóa đơn" onClose={() => setCancelOpen(false)}>
        <div className="form-stack">
          <label><span>Lý do hủy</span><textarea rows={4} value={cancelReason} onChange={(event) => setCancelReason(event.target.value)} /></label>
          <div className="modal-actions"><button className="ghost-btn" onClick={() => setCancelOpen(false)}>Hủy</button><button className="danger-btn" onClick={cancelInvoice}>Xác nhận hủy hóa đơn</button></div>
        </div>
      </Modal> : null}
    </>
  );
}

function ReviewInvitationPanel({ invoiceId, invitation, onCreated }: {
  invoiceId: number;
  invitation: ReviewInvitationLink | null;
  onCreated: (value: ReviewInvitationLink) => void;
}) {
  const toast = useToast();
  const [creating, setCreating] = useState(false);

  async function createInvitation() {
    setCreating(true);
    try {
      const value = await customerReviewsApi.createInvitationForInvoice(invoiceId);
      onCreated(value);
      toast("Đã tạo link đánh giá.", "success");
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không tạo được link đánh giá.", "error");
    } finally {
      setCreating(false);
    }
  }

  if (!invitation) {
    return (
      <div className="card" style={{ marginTop: 16 }}>
        <h3 style={{ marginTop: 0 }}>Mời khách đánh giá</h3>
        <p className="muted-text">Tạo link đánh giá dùng một lần cho hóa đơn đã thanh toán này.</p>
        <button className="primary-btn" onClick={createInvitation} disabled={creating}>{creating ? "Đang tạo..." : "Tạo link đánh giá"}</button>
      </div>
    );
  }

  const qrUrl = `https://api.qrserver.com/v1/create-qr-code/?size=220x220&data=${encodeURIComponent(invitation.reviewUrl)}`;
  return (
    <div className="card" style={{ marginTop: 16, display: "grid", gridTemplateColumns: "180px 1fr", gap: 18, alignItems: "center" }}>
      <img src={qrUrl} alt="QR đánh giá" style={{ width: 180, height: 180, borderRadius: 8, border: "1px solid var(--line)", padding: 8, background: "white" }} />
      <div>
        <h3 style={{ marginTop: 0 }}>Mời khách đánh giá</h3>
        <p className="muted-text">Link đánh giá dùng một lần, hết hạn lúc {new Date(invitation.expiresAtUtc).toLocaleString("vi-VN")}.</p>
        <div style={{ wordBreak: "break-all", padding: 10, border: "1px solid var(--line)", borderRadius: 8, background: "var(--soft)", marginBottom: 12 }}>{invitation.reviewUrl}</div>
        <div className="actions">
          <button className="secondary-btn" onClick={() => navigator.clipboard.writeText(invitation.reviewUrl).then(() => toast("Đã sao chép link đánh giá.", "success"))}>Copy link</button>
          <a className="primary-btn" href={invitation.reviewUrl} target="_blank">Mở form đánh giá</a>
        </div>
      </div>
    </div>
  );
}
