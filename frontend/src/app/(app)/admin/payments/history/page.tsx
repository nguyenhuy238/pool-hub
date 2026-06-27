"use client";

import { useState } from "react";
import { Badge, DataTable, ListControls, Modal, PageHeader, StateBlock, useList, useLoad, Pagination } from "@/components/ui";
import { getTotalPages } from "@/lib/api/client";
import { paymentsApi } from "@/lib/api/endpoints";
import { money, paymentStatus } from "@/lib/status";
import { useToast } from "@/components/toast";

export default function PaymentHistoryPage() {
  const toast = useToast();
  const [query, setQuery] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const [refundPaymentId, setRefundPaymentId] = useState<number | null>(null);
  const [refundReason, setRefundReason] = useState("");
  const payments = useLoad(() => paymentsApi.list({ PageNumber: query.pageNumber, PageSize: query.pageSize }), [query]);
  const rows = useList(payments.data);

  return <>
    <PageHeader title="Lịch sử giao dịch" description="Theo dõi tất cả các giao dịch thanh toán hóa đơn trong hệ thống." />
    <ListControls {...query} onChange={setQuery} />
    <StateBlock loading={payments.loading} error={payments.error} empty={!payments.loading && !rows.length} />
    <DataTable rows={rows as Record<string, unknown>[]} columns={[
      { key: "paymentId", label: "Mã giao dịch" }, 
      { key: "invoiceId", label: "Mã hóa đơn" }, 
      { key: "paymentMethodId", label: "Mã phương thức" },
      { key: "amount", label: "Số tiền", render: row => <strong>{money(Number(row.amount || 0))}</strong> },
      { key: "paymentStatus", label: "Trạng thái", render: row => <Badge tone={Number(row.paymentStatus) === 4 ? "red" : Number(row.paymentStatus) === 2 ? "green" : "yellow"}>{paymentStatus[Number(row.paymentStatus)] ?? "Không xác định"}</Badge> },
      { key: "paidAtUtc", label: "Thanh toán lúc" }
    ]} actions={row => Number(row.paymentStatus) === 2 ? (
      <button className="danger-btn ghost-btn" onClick={() => setRefundPaymentId(Number(row.paymentId))}>Hoàn tiền</button>
    ) : null} />
    <Pagination pageNumber={query.pageNumber} totalPages={getTotalPages(payments.data, query.pageSize)} onChange={(pageNumber) => setQuery({ ...query, pageNumber })} />
    
    {refundPaymentId ? <Modal title="Hoàn tiền giao dịch" onClose={() => setRefundPaymentId(null)}>
      <div className="form-stack">
        <label><span>Lý do hoàn tiền</span><textarea rows={4} value={refundReason} onChange={(event) => setRefundReason(event.target.value)} /></label>
        <div className="modal-actions">
          <button className="ghost-btn" onClick={() => setRefundPaymentId(null)}>Hủy</button>
          <button className="danger-btn" onClick={async () => {
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
          }}>Xác nhận hoàn tiền</button>
        </div>
      </div>
    </Modal> : null}
  </>;
}
