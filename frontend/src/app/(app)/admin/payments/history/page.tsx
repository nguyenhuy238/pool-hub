"use client";

import { useState } from "react";
import { Badge, DataTable, ListControls, Modal, PageHeader, StateBlock, useList, useLoad, Pagination } from "@/components/ui";
import { getTotalPages } from "@/lib/api/client";
import { paymentsApi } from "@/lib/api/endpoints";
import { money, paymentStatus } from "@/lib/status";
import { formatVietnamDateTimeWithSeconds } from "@/lib/dateTime";
import { useToast } from "@/components/toast";

export default function PaymentHistoryPage() {
  const toast = useToast();
  const [query, setQuery] = useState({ search: "", pageNumber: 1, pageSize: 20, paymentStatus: "" });
  const [refundPaymentId, setRefundPaymentId] = useState<number | null>(null);
  const [refundReason, setRefundReason] = useState("");
  const [selectedPayment, setSelectedPayment] = useState<Record<string, any> | null>(null);
  const payments = useLoad(() => paymentsApi.list({ PageNumber: query.pageNumber, PageSize: query.pageSize, PaymentStatus: query.paymentStatus ? Number(query.paymentStatus) : undefined }), [query]);
  const rows = useList(payments.data);

  return <>
    <PageHeader title="Lịch sử giao dịch" description="Theo dõi tất cả các giao dịch thanh toán hóa đơn trong hệ thống." />
    <ListControls
      search={query.search}
      pageNumber={query.pageNumber}
      pageSize={query.pageSize}
      onChange={(next) => setQuery((prev) => ({ ...prev, ...next }))}
      extra={
        <label>
          <span>Trạng thái</span>
          <select value={query.paymentStatus} onChange={(e) => setQuery((prev) => ({ ...prev, paymentStatus: e.target.value, pageNumber: 1 }))}>
            <option value="">Tất cả</option>
            <option value="1">Chờ thanh toán</option>
            <option value="2">Đã hoàn tất</option>
            <option value="3">Thất bại</option>
            <option value="4">Đã hoàn tiền</option>
          </select>
        </label>
      }
    />
    <StateBlock loading={payments.loading} error={payments.error} empty={!payments.loading && !rows.length} />
    <DataTable rows={rows as Record<string, unknown>[]} columns={[
      { key: "transactionCode", label: "Mã giao dịch", render: row => <span>{String(row.transactionCode || `TXN-${row.paymentId}`)}</span> }, 
      { key: "invoiceCode", label: "Mã hóa đơn", render: row => <span>{String(row.invoiceCode || `INV-${row.invoiceId}`)}</span> }, 
      { key: "paymentMethodName", label: "Phương thức", render: row => <span>{String(row.paymentMethodName || (Number(row.paymentMethodId) === 1 ? "Tiền mặt" : Number(row.paymentMethodId) === 2 ? "Chuyển khoản" : "Khác"))}</span> },
      { key: "amount", label: "Số tiền", render: row => <strong>{money(Number(row.amount || 0))}</strong> },
      { key: "paymentStatus", label: "Trạng thái", render: row => <Badge tone={Number(row.paymentStatus) === 4 ? "red" : Number(row.paymentStatus) === 2 ? "green" : "yellow"}>{paymentStatus[Number(row.paymentStatus)] ?? "Không xác định"}</Badge> },
      { key: "paidAtUtc", label: "Thanh toán lúc", render: row => <span>{row.paidAtUtc ? formatVietnamDateTimeWithSeconds(String(row.paidAtUtc)) : "-"}</span> }
    ]} actions={row => (
      <div style={{ display: "flex", gap: "8px" }}>
        <button className="ghost-btn" onClick={() => setSelectedPayment(row)}>Xem chi tiết</button>
        {Number(row.paymentStatus) === 2 ? (
          <button className="danger-btn ghost-btn" onClick={() => setRefundPaymentId(Number(row.paymentId))}>Hoàn tiền</button>
        ) : null}
      </div>
    )} />
    <Pagination pageNumber={query.pageNumber} totalPages={getTotalPages(payments.data, query.pageSize)} onChange={(pageNumber) => setQuery({ ...query, pageNumber })} />
    
    {selectedPayment ? (
      <Modal title="Chi tiết giao dịch" onClose={() => setSelectedPayment(null)}>
        <div className="form-stack">
          <div style={{ display: "grid", gridTemplateColumns: "140px 1fr", gap: "12px", fontSize: "14px", lineHeight: "1.6" }}>
            <strong>Mã giao dịch:</strong> <span>{String(selectedPayment.transactionCode || `TXN-${selectedPayment.paymentId}`)}</span>
            <strong>Mã hóa đơn:</strong> <span>{String(selectedPayment.invoiceCode || `INV-${selectedPayment.invoiceId}`)}</span>
            <strong>Phương thức:</strong> <span>{String(selectedPayment.paymentMethodName || (Number(selectedPayment.paymentMethodId) === 1 ? "Tiền mặt" : Number(selectedPayment.paymentMethodId) === 2 ? "Chuyển khoản" : "Khác"))}</span>
            <strong>Số tiền:</strong> <strong>{money(Number(selectedPayment.amount || 0))}</strong>
            <strong>Trạng thái:</strong> <div><Badge tone={Number(selectedPayment.paymentStatus) === 4 ? "red" : Number(selectedPayment.paymentStatus) === 2 ? "green" : "yellow"}>{paymentStatus[Number(selectedPayment.paymentStatus)] ?? "Không xác định"}</Badge></div>
            <strong>Thanh toán lúc:</strong> <span>{selectedPayment.paidAtUtc ? formatVietnamDateTimeWithSeconds(String(selectedPayment.paidAtUtc)) : "-"}</span>
            {selectedPayment.note && !String(selectedPayment.note).startsWith("Refunded:") ? (
              <>
                <strong>Ghi chú:</strong> <span>{String(selectedPayment.note)}</span>
              </>
            ) : null}
            {Number(selectedPayment.paymentStatus) === 4 ? (
              <>
                <strong style={{ color: "#dc2626" }}>Lý do hoàn tiền:</strong>
                <span style={{ color: "#dc2626", fontWeight: 600 }}>
                  {String(selectedPayment.refundReason || (typeof selectedPayment.note === "string" && selectedPayment.note.includes("Refunded: ") ? selectedPayment.note.split("Refunded: ")[1] : selectedPayment.note || "Không ghi rõ"))}
                </span>
              </>
            ) : null}
          </div>
          <div className="modal-actions" style={{ marginTop: "16px" }}>
            <button className="primary-btn" onClick={() => setSelectedPayment(null)}>Đóng</button>
          </div>
        </div>
      </Modal>
    ) : null}

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
