"use client";

import { useState } from "react";
import { invoiceApi } from "@/lib/api/endpoints";
import { money } from "@/lib/status";
import { DataTable, ListControls, PageHeader, SmartForm, StateBlock, useList, useLoad } from "@/components/ui";
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
      <PageHeader title="Invoice & Payment" description="Generate invoice, xem chi tiết và ghi nhận thanh toán." />
      <ListControls search={params.search} pageNumber={params.pageNumber} pageSize={params.pageSize} onChange={setParams} />
      <SmartForm<{ sessionId: number }> title="Tạo hóa đơn từ session" initial={{}} fields={[{ name: "sessionId", label: "Session ID", type: "number", required: true }]} onSubmit={async (value) => { const created = await invoiceApi.generate(Number(value.sessionId)); setInvoice(await invoiceApi.detail(created.invoiceId)); reload(); }} />
      <StateBlock loading={loading} error={error} empty={!loading && !invoices.length} />
      <DataTable rows={invoices as unknown as Record<string, unknown>[]} columns={[
        { key: "invoiceCode", label: "Mã" },
        { key: "sessionId", label: "Session" },
        { key: "grandTotalAmount", label: "Tổng", render: (row) => money(Number(row.grandTotalAmount || 0)) }
      ]} actions={(row) => <button className="ghost-btn" onClick={() => loadDetail(Number(row.invoiceId)).catch((err) => toast(err.message, "error"))}>Chi tiết</button>} />
      {invoice ? <div className="card">
        <h2>{invoice.invoiceCode || `Invoice #${invoice.invoiceId}`}</h2>
        <p>Tiền bàn: {money(invoice.timeSubtotalAmount)}</p>
        <p>Tiền sản phẩm: {money(invoice.productSubtotalAmount)}</p>
        <p>Giảm giá: {money(invoice.discountAmount)}</p>
        <p><strong>Tổng tiền: {money(invoice.grandTotalAmount)}</strong></p>
        <div className="actions">
          <select value={paymentMethodId} onChange={(event) => setPaymentMethodId(Number(event.target.value))}>
            <option value="">Phương thức thanh toán</option>
            {methods.map((method) => <option key={method.paymentMethodId} value={method.paymentMethodId}>{method.name}</option>)}
          </select>
          <button className="primary-btn" onClick={pay}>Pay</button>
          <button className="ghost-btn" onClick={() => {
            const code = window.prompt("Nhập mã giảm giá");
            if (code) invoiceApi.discount(invoice.invoiceId, code).then(async () => { toast("Đã áp dụng discount.", "success"); await loadDetail(invoice.invoiceId); reload(); }).catch((err) => toast(err.message, "error"));
          }}>Apply discount</button>
        </div>
      </div> : null}
    </>
  );
}
