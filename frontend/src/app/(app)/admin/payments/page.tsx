"use client";

import { useState } from "react";
import { Badge, DataTable, ListControls, PageHeader, SmartForm, StateBlock, useList, useLoad } from "@/components/ui";
import { paymentsApi } from "@/lib/api/endpoints";
import { money, paymentStatus } from "@/lib/status";
import type { PaymentMethod } from "@/types";

export default function PaymentsPage() {
  const [query, setQuery] = useState({ search: "", pageNumber: 1, pageSize: 20 });
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
    ]} actions={row => <button className="ghost-btn" onClick={async () => { await paymentsApi.methodStatus(row.paymentMethodId, !row.isActive); methods.reload(); }}>{row.isActive ? "Tắt" : "Bật"}</button>} />
    <h2>Lịch sử thanh toán</h2>
    <StateBlock loading={payments.loading} error={payments.error} empty={!payments.loading && !rows.length} />
    <DataTable rows={rows as Record<string, unknown>[]} columns={[
      { key: "paymentId", label: "Mã giao dịch" }, { key: "invoiceId", label: "Mã hóa đơn" }, { key: "paymentMethodId", label: "Mã phương thức" },
      { key: "amount", label: "Số tiền", render: row => <strong>{money(Number(row.amount || 0))}</strong> },
      { key: "paymentStatus", label: "Trạng thái", render: row => <Badge tone={Number(row.paymentStatus) === 4 ? "red" : Number(row.paymentStatus) === 2 ? "green" : "yellow"}>{paymentStatus[Number(row.paymentStatus)] ?? "Không xác định"}</Badge> },
      { key: "paidAtUtc", label: "Thanh toán lúc" }
    ]} actions={row => Number(row.paymentStatus) === 2 ? (
      <button className="danger-btn ghost-btn" onClick={async () => {
        const reason = window.prompt("Lý do hoàn tiền:");
        if (reason) {
          try {
            await paymentsApi.refund(Number(row.paymentId), reason);
            payments.reload();
            alert("Đã hoàn tiền thành công.");
          } catch (err: any) {
            alert(err.message);
          }
        }
      }}>Hoàn tiền</button>
    ) : null} />
  </>;
}
