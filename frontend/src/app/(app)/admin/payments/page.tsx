"use client";

import { useState } from "react";
import { Badge, ConfirmDialog, DataTable, ListControls, Modal, PageHeader, SmartForm, StateBlock, useList, useLoad } from "@/components/ui";
import { paymentsApi } from "@/lib/api/endpoints";
import { money, paymentStatus } from "@/lib/status";
import { useToast } from "@/components/toast";
import type { PaymentMethod } from "@/types";

export default function PaymentsPage() {
  const toast = useToast();
  const [query, setQuery] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const [methodAction, setMethodAction] = useState<PaymentMethod | null>(null);
  const [refundPaymentId, setRefundPaymentId] = useState<number | null>(null);
  const [refundReason, setRefundReason] = useState("");
  const payments = useLoad(() => paymentsApi.list({ PageNumber: query.pageNumber, PageSize: query.pageSize }), [query]);
  const methods = useLoad(() => paymentsApi.methods(), []);
  const rows = useList(payments.data);
  return <>
    <PageHeader title="Thanh toán và phương thức thanh toán" description="Theo dõi giao dịch, hoàn tiền và cấu hình phương thức thanh toán." />
    <ListControls {...query} onChange={setQuery} />
    <SmartForm<PaymentMethod> title="Tạo phương thức thanh toán" initial={{ isActive: true }}
      fields={[{ name: "name", label: "Tên phương thức", required: true }, { name: "code", label: "Mã phương thức", required: true }, { name: "description", label: "Mô tả" }]}
      onSubmit={async value => { await paymentsApi.createMethod(value); methods.reload(); }} />
    <h2>Phương thức thanh toán</h2>
    <StateBlock loading={methods.loading} error={methods.error} empty={!methods.loading && !methods.data?.length} />
    <DataTable rows={methods.data || []} columns={[
      { key: "code", label: "Mã" }, { key: "name", label: "Tên phương thức" }, { key: "description", label: "Mô tả" },
      { key: "isActive", label: "Trạng thái", render: row => <Badge tone={row.isActive ? "green" : "red"}>{row.isActive ? "Đang hoạt động" : "Ngừng hoạt động"}</Badge> }
    ]} actions={row => <button className="ghost-btn" onClick={() => setMethodAction(row as PaymentMethod)}>{row.isActive ? "Tắt" : "Bật"}</button>} />
    <h2>Lịch sử thanh toán</h2>
    <StateBlock loading={payments.loading} error={payments.error} empty={!payments.loading && !rows.length} />
    <DataTable rows={rows as Record<string, unknown>[]} columns={[
      { key: "paymentId", label: "Mã giao dịch" }, { key: "invoiceId", label: "Mã hóa đơn" }, { key: "paymentMethodId", label: "Mã phương thức" },
      { key: "amount", label: "Số tiền", render: row => <strong>{money(Number(row.amount || 0))}</strong> },
      { key: "paymentStatus", label: "Trạng thái", render: row => <Badge tone={Number(row.paymentStatus) === 4 ? "red" : Number(row.paymentStatus) === 2 ? "green" : "yellow"}>{paymentStatus[Number(row.paymentStatus)] ?? "Không xác định"}</Badge> },
      { key: "paidAtUtc", label: "Thanh toán lúc" }
    ]} actions={row => Number(row.paymentStatus) === 2 ? (
      <button className="danger-btn ghost-btn" onClick={() => setRefundPaymentId(Number(row.paymentId))}>Hoàn tiền</button>
    ) : null} />
    {methodAction ? <ConfirmDialog title={methodAction.isActive ? "Tắt phương thức thanh toán" : "Bật phương thức thanh toán"} message={`${methodAction.isActive ? "Tắt" : "Bật"} phương thức “${methodAction.name}”?`} confirmLabel="Xác nhận" danger={methodAction.isActive} onCancel={() => setMethodAction(null)} onConfirm={async () => {
      try {
        await paymentsApi.methodStatus(methodAction.paymentMethodId, !methodAction.isActive);
        toast("Đã cập nhật phương thức thanh toán.", "success");
        setMethodAction(null);
        methods.reload();
      } catch (err) {
        toast(err instanceof Error ? err.message : "Không thể cập nhật phương thức.", "error");
      }
    }} /> : null}
    {refundPaymentId ? <Modal title="Hoàn tiền giao dịch" onClose={() => setRefundPaymentId(null)}>
      <div className="form-stack">
        <label><span>Lý do hoàn tiền</span><textarea rows={4} value={refundReason} onChange={(event) => setRefundReason(event.target.value)} /></label>
        <div className="modal-actions"><button className="ghost-btn" onClick={() => setRefundPaymentId(null)}>Hủy</button><button className="danger-btn" onClick={async () => {
          if (!refundReason.trim()) return toast("Vui lòng nhập lý do hoàn tiền.", "error");
          try {
            await paymentsApi.refund(refundPaymentId, refundReason.trim());
            toast("Đã hoàn tiền thành công.", "success");
            setRefundPaymentId(null);
            setRefundReason("");
            payments.reload();
          } catch (err) {
            toast(err instanceof Error ? err.message : "Không thể hoàn tiền.", "error");
          }
        }}>Xác nhận hoàn tiền</button></div>
      </div>
    </Modal> : null}
  </>;
}
