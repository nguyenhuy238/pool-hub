"use client";

import { useState } from "react";
import { Badge, DataTable, ListControls, PageHeader, SmartForm, StateBlock, useList, useLoad } from "@/components/ui";
import { paymentsApi } from "@/lib/api/endpoints";
import type { PaymentMethod } from "@/types";

export default function PaymentsPage() {
  const [query, setQuery] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const payments = useLoad(() => paymentsApi.list({ PageNumber: query.pageNumber, PageSize: query.pageSize }), [query]);
  const methods = useLoad(() => paymentsApi.methods(), []);
  const rows = useList(payments.data);
  return <>
    <PageHeader title="Payments & Methods" description="Theo dõi thanh toán và cấu hình phương thức thanh toán." />
    <ListControls {...query} onChange={setQuery} />
    <SmartForm<PaymentMethod> title="Tạo phương thức thanh toán" initial={{ isActive: true }}
      fields={[{ name: "name", label: "Tên", required: true }, { name: "code", label: "Code", required: true }, { name: "description", label: "Mô tả" }]}
      onSubmit={async value => { await paymentsApi.createMethod(value); methods.reload(); }} />
    <h2>Phương thức thanh toán</h2>
    <StateBlock loading={methods.loading} error={methods.error} empty={!methods.loading && !methods.data?.length} />
    <DataTable rows={methods.data || []} columns={[
      { key: "code", label: "Code" }, { key: "name", label: "Tên" }, { key: "description", label: "Mô tả" },
      { key: "isActive", label: "Trạng thái", render: row => <Badge tone={row.isActive ? "green" : "red"}>{row.isActive ? "Active" : "Inactive"}</Badge> }
    ]} actions={row => <button className="ghost-btn" onClick={async () => { await paymentsApi.methodStatus(row.paymentMethodId, !row.isActive); methods.reload(); }}>{row.isActive ? "Tắt" : "Bật"}</button>} />
    <h2>Lịch sử thanh toán</h2>
    <StateBlock loading={payments.loading} error={payments.error} empty={!payments.loading && !rows.length} />
    <DataTable rows={rows as Record<string, unknown>[]} columns={[
      { key: "paymentId", label: "ID" }, { key: "invoiceId", label: "Invoice" }, { key: "paymentMethodId", label: "Method" },
      { key: "amount", label: "Số tiền", render: row => <strong>{Number(row.amount || 0).toLocaleString()}</strong> }, 
      { key: "paymentStatus", label: "Trạng thái", render: row => Number(row.paymentStatus) === 4 ? <Badge tone="red">Refunded</Badge> : Number(row.paymentStatus) === 2 ? <Badge tone="green">Completed</Badge> : <Badge tone="yellow">Pending</Badge> }, 
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
