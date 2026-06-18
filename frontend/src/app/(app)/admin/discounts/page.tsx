"use client";

import { useState } from "react";
import { Badge, DataTable, ListControls, PageHeader, SmartForm, StateBlock, useList, useLoad } from "@/components/ui";
import { discountApi } from "@/lib/api/endpoints";
import type { Discount } from "@/types";

export default function DiscountsPage() {
  const [query, setQuery] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const { data, loading, error, reload } = useLoad(() => discountApi.list({ Search: query.search, PageNumber: query.pageNumber, PageSize: query.pageSize }), [query]);
  const rows = useList(data);
  return <>
    <PageHeader title="Discounts" description="Giảm giá chỉ được áp dụng trên tiền giờ chơi." />
    <ListControls {...query} onChange={setQuery} />
    <SmartForm<Discount> title="Tạo discount" initial={{ discountType: "PERCENTAGE", appliesTo: "TIME", isActive: true }}
      fields={[
        { name: "discountCode", label: "Code", required: true }, { name: "name", label: "Tên", required: true },
        { name: "discountType", label: "Loại", required: true, options: [{ value: "PERCENTAGE", label: "Percent" }, { value: "FIXED_AMOUNT", label: "Số tiền cố định" }] },
        { name: "value", label: "Giá trị", type: "number", required: true }, { name: "maxAmount", label: "Mức giảm tối đa", type: "number" },
        { name: "minTimeSubtotal", label: "Tiền giờ tối thiểu", type: "number" }, { name: "startsAtUtc", label: "Bắt đầu (UTC)", type: "datetime-local", required: true },
        { name: "endsAtUtc", label: "Kết thúc (UTC)", type: "datetime-local" }
      ]} onSubmit={async value => { await discountApi.create(value); reload(); }} />
    <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
    <DataTable rows={rows} columns={[
      { key: "discountCode", label: "Code" }, { key: "name", label: "Tên" }, { key: "discountType", label: "Loại" },
      { key: "value", label: "Giá trị" }, { key: "appliesTo", label: "Áp dụng" },
      { key: "isActive", label: "Trạng thái", render: row => <Badge tone={row.isActive ? "green" : "red"}>{row.isActive ? "Active" : "Inactive"}</Badge> }
    ]} actions={row => <button className="ghost-btn" onClick={async () => { await discountApi.status(row.discountId, !row.isActive); reload(); }}>{row.isActive ? "Tắt" : "Bật"}</button>} />
  </>;
}
