"use client";

import { useState } from "react";
import { invoiceApi } from "@/lib/api/endpoints";
import { money } from "@/lib/status";
import { DataTable, ListControls, PageHeader, SmartForm, StateBlock, useList, useLoad, Modal } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { Invoice, PaymentMethod } from "@/types";

export default function InvoicesPage() {
  const toast = useToast();
  const [invoice, setInvoice] = useState<Invoice | null>(null);
  const [paymentMethodId, setPaymentMethodId] = useState<number | "">("");
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const { data, loading, error, reload } = useLoad(async () => {
    const [invoices, methods] = await Promise.all([
      invoiceApi.list({ Search: params.search, PageNumber: params.pageNumber, PageSize: params.pageSize }),
      invoiceApi.paymentMethods()
    ]);
    return { invoices, methods };
  }, [params]);
  const invoices = useList<Invoice>(data?.invoices);
  const methods = (data?.methods || []) as PaymentMethod[];

  async function loadDetail(id: number) {
    setInvoice(await invoiceApi.detail(id));
  }

  async function pay() {
    if (!invoice) return;
    if (!paymentMethodId) {
      toast("Vui lòng chọn phương thức thanh toán.", "error");
      return;
    }
    await invoiceApi.pay({ invoiceId: invoice.invoiceId, paymentMethodId: Number(paymentMethodId), amount: invoice.grandTotalAmount || 0 })
      .then(() => toast("Đã ghi nhận thanh toán.", "success"))
      .catch((err) => toast(err.message, "error"));
    await loadDetail(invoice.invoiceId);
    reload();
  }

  return (
    <>
      <PageHeader title="Hóa đơn và thanh toán" description="Tạo hóa đơn, xem chi tiết và ghi nhận thanh toán." />
      <ListControls search={params.search} pageNumber={params.pageNumber} pageSize={params.pageSize} onChange={setParams} />
      <SmartForm<{ sessionId: number }> 
        title="Tạo hóa đơn từ phiên chơi"
        initial={{}} 
        fields={[{ name: "sessionId", label: "Session ID", type: "number", required: true }]} 
        onSubmit={async (value) => { 
          const created = await invoiceApi.generate(Number(value.sessionId)); 
          setInvoice(await invoiceApi.detail(created.invoiceId)); 
          reload(); 
        }} 
      />
      <StateBlock loading={loading} error={error} empty={!loading && !invoices.length} />
      <DataTable 
        rows={invoices as unknown as Record<string, unknown>[]} 
        columns={[
          { key: "invoiceCode", label: "Mã" },
          { key: "sessionId", label: "Session" },
          { key: "grandTotalAmount", label: "Tổng tiền", render: (row) => <strong>{money(Number(row.grandTotalAmount || 0))}</strong> },
          { key: "paymentStatus", label: "Trạng thái thanh toán", render: (row) => Number(row.paymentStatus) === 3 ? <span className="badge green">Đã thanh toán</span> : <span className="badge yellow">Chưa thanh toán</span> }
        ]} 
        actions={(row) => <button className="ghost-btn" onClick={() => loadDetail(Number(row.invoiceId)).catch((err) => toast(err.message, "error"))}>Chi tiết</button>} 
      />
      {invoice ? (
        <Modal title={invoice.invoiceCode || `Hóa đơn #${invoice.invoiceId}`} onClose={() => setInvoice(null)} size="large">
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '16px' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
              {Number(invoice.paymentStatus) === 3 ? <span className="badge green" style={{ fontSize: '14px', padding: '6px 12px' }}>ĐÃ THANH TOÁN</span> : Number(invoice.status) === 3 ? <span className="badge red" style={{ fontSize: '14px', padding: '6px 12px' }}>ĐÃ HỦY</span> : <span className="badge yellow" style={{ fontSize: '14px', padding: '6px 12px' }}>CHƯA THANH TOÁN</span>}
            </div>
            <button className="ghost-btn" onClick={() => {
              invoiceApi.exportPdf(invoice.invoiceId).then(res => { 
                const printWindow = window.open("", "_blank");
                if (printWindow) {
                  printWindow.document.write(`<html><head><title>Invoice ${invoice.invoiceCode || invoice.invoiceId}</title></head><body style="font-family: Arial, sans-serif; padding: 40px; max-width: 600px; margin: 0 auto;">
                    <h1 style="text-align:center;">HÓA ĐƠN THANH TOÁN</h1>
                    <h3 style="text-align:center; color: #555;">Mã: ${invoice.invoiceCode || invoice.invoiceId}</h3>
                    <hr style="border: 1px dashed #ccc; margin: 20px 0;"/>
                    <div style="display:flex; justify-content:space-between; margin-bottom: 10px;"><span>Tiền giờ chơi:</span> <span>${money(invoice.timeSubtotalAmount)}</span></div>
                    <div style="display:flex; justify-content:space-between; margin-bottom: 10px;"><span>Dịch vụ/Sản phẩm:</span> <span>${money(invoice.productSubtotalAmount)}</span></div>
                    <div style="display:flex; justify-content:space-between; margin-bottom: 10px;"><span>Giảm giá:</span> <span>-${money(invoice.discountAmount)}</span></div>
                    <hr style="border: 1px dashed #ccc; margin: 20px 0;"/>
                    <div style="display:flex; justify-content:space-between; font-size: 20px; font-weight: bold;"><span>TỔNG CỘNG:</span> <span>${money(invoice.grandTotalAmount)}</span></div>
                    <p style="text-align:center; margin-top: 40px; font-style: italic;">Cảm ơn quý khách và hẹn gặp lại!</p>
                    <script>setTimeout(() => window.print(), 500);</script>
                  </body></html>`);
                  printWindow.document.close();
                }
              }).catch(err => toast(err.message, "error"));
            }}>In PDF / Xuất Bill</button>
          </div>
          
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px', marginBottom: '24px' }}>
            <div>
              <p style={{ margin: '4px 0' }}>Tiền giờ: {money(invoice.timeSubtotalAmount)}</p>
              <p style={{ margin: '4px 0' }}>Tiền dịch vụ/sản phẩm: {money(invoice.productSubtotalAmount)}</p>
            </div>
            <div style={{ textAlign: 'right' }}>
              <p style={{ margin: '4px 0' }}>Giảm giá: <span style={{ color: 'var(--danger)' }}>-{money(invoice.discountAmount)}</span></p>
              <p style={{ margin: '8px 0 0', fontSize: '20px' }}><strong>Cần thanh toán: {money(invoice.grandTotalAmount)}</strong></p>
            </div>
          </div>

          {Number(invoice.status) === 3 ? (
            <div className="state-card" style={{ background: '#fdeded', color: '#5f2120', border: '1px solid #f4c3c2', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              Hóa đơn này đã bị hủy.
            </div>
          ) : Number(invoice.paymentStatus) !== 3 ? (
            <div className="actions" style={{ background: 'var(--soft)', padding: '16px', borderRadius: '8px', border: '1px solid var(--line)' }}>
              <select value={paymentMethodId} onChange={(event) => setPaymentMethodId(Number(event.target.value))}>
                <option value="">-- Chọn phương thức thanh toán --</option>
                {methods.map((method) => <option key={method.paymentMethodId} value={method.paymentMethodId}>{method.name}</option>)}
              </select>
              <button className="primary-btn" onClick={pay}>Thanh toán ngay</button>
              <button className="ghost-btn" onClick={() => {
                const code = window.prompt("Nhập mã giảm giá");
                if (code) invoiceApi.discount(invoice.invoiceId, code).then(async () => { toast("Đã áp dụng discount.", "success"); await loadDetail(invoice.invoiceId); reload(); }).catch((err) => toast(err.message, "error"));
              }}>Áp dụng mã giảm giá</button>
              <button className="danger-btn" style={{ marginLeft: "auto" }} onClick={() => {
                const reason = window.prompt("Nhập lý do hủy hóa đơn:");
                if (reason) {
                  invoiceApi.cancel(invoice.invoiceId, reason).then(async () => {
                    toast("Hóa đơn đã bị hủy.", "success");
                    await loadDetail(invoice.invoiceId);
                    reload();
                  }).catch(err => toast(err.message, "error"));
                }
              }}>Hủy hóa đơn</button>
            </div>
          ) : (
            <div className="state-card" style={{ background: '#e4f7ec', color: '#187344', border: '1px solid #c2ebd5', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              Hóa đơn này đã được thanh toán hoàn tất. Không thể sửa đổi hay thanh toán thêm.
            </div>
          )}
        </Modal>
      ) : null}
    </>
  );
}
