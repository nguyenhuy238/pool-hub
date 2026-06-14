"use client";

import { useState } from "react";
import { customerApi } from "@/lib/api/endpoints";
import { Badge, DataTable, ListControls, PageHeader, SmartForm, StateBlock, useList, useLoad } from "@/components/ui";
import type { Customer } from "@/types";

export default function CustomersPage() {
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const { data, loading, error, reload } = useLoad(
    () => customerApi.list({ Search: params.search, PageNumber: params.pageNumber, PageSize: params.pageSize }),
    [params]
  );
  const rows = useList<Customer>(data);

  return (
    <>
      <PageHeader title="Customers" description="Quản lý khách hàng, số điện thoại và trạng thái." />
      <ListControls search={params.search} pageNumber={params.pageNumber} pageSize={params.pageSize} onChange={setParams} />
      <SmartForm<Customer>
        title="Tạo khách hàng"
        initial={{ status: true }}
        fields={[
          { name: "fullName", label: "Họ tên", required: true },
          { name: "phoneNumber", label: "Số điện thoại", required: true },
          { name: "email", label: "Email", type: "email" },
          { name: "note", label: "Ghi chú" }
        ]}
        onSubmit={async (value) => {
          await customerApi.create(value);
          reload();
        }}
      />
      <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
      <DataTable
        rows={rows as unknown as Record<string, unknown>[]}
        columns={[
          { key: "fullName", label: "Tên" },
          { key: "phoneNumber", label: "Điện thoại" },
          { key: "email", label: "Email", render: (row) => String(row.email || "-") },
          { key: "status", label: "Trạng thái", render: (row) => <Badge tone={row.status ? "green" : "neutral"}>{row.status ? "Active" : "Inactive"}</Badge> }
        ]}
        actions={(row) => (
          <button className="ghost-btn" onClick={() => customerApi.updateStatus(Number(row.customerId), !row.status).then(() => reload())}>
            Toggle
          </button>
        )}
      />
    </>
  );
}
