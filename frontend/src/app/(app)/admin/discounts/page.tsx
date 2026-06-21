"use client";

import { useState } from "react";
import { Badge, DataTable, ListControls, PageHeader, SmartForm, StateBlock, useList, useLoad, Modal } from "@/components/ui";
import { discountApi } from "@/lib/api/endpoints";
import type { Discount } from "@/types";

export default function DiscountsPage() {
  const [query, setQuery] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const [editingDiscount, setEditingDiscount] = useState<Discount | null>(null);
  const [isCreating, setIsCreating] = useState(false);
  const { data, loading, error, reload } = useLoad(() => discountApi.list({ Search: query.search, PageNumber: query.pageNumber, PageSize: query.pageSize }), [query]);
  const rows = useList(data);
  return <>
    <PageHeader title="Discounts" description="Giảm giá chỉ được áp dụng trên tiền giờ chơi." action={<button className="primary-btn" onClick={() => setIsCreating(true)}>Tạo discount</button>} />
    <ListControls {...query} onChange={setQuery} />
    {isCreating && (
      <Modal title="Tạo discount" onClose={() => setIsCreating(false)}>
        <SmartForm<Discount> title="" initial={{ discountType: "PERCENTAGE", appliesTo: "TIME", isActive: true }}
          fields={[
            { name: "discountCode", label: "Code", required: true }, { name: "name", label: "Tên", required: true },
            { name: "discountType", label: "Loại", required: true, options: [{ value: "PERCENTAGE", label: "Percent" }, { value: "FIXED_AMOUNT", label: "Số tiền cố định" }] },
            { name: "value", label: "Giá trị", type: "number", required: true }, { name: "maxAmount", label: "Mức giảm tối đa", type: "number" },
            { name: "minTimeSubtotal", label: "Tiền giờ tối thiểu", type: "number" }, { name: "startsAtUtc", label: "Bắt đầu (UTC)", type: "datetime-local", required: true },
            { name: "endsAtUtc", label: "Kết thúc (UTC)", type: "datetime-local" }
          ]} onSubmit={async value => { await discountApi.create(value); setIsCreating(false); reload(); }} />
      </Modal>
    )}
    <StateBlock loading={loading && !rows.length} error={error} empty={!loading && !rows.length} />
    <DataTable rows={rows} columns={[
      { key: "discountCode", label: "Code" }, { key: "name", label: "Tên" }, { key: "discountType", label: "Loại" },
      { key: "value", label: "Giá trị" }, { key: "minTimeSubtotal", label: "Tiền giờ tối thiểu" }, { key: "appliesTo", label: "Áp dụng" },
      { key: "isActive", label: "Trạng thái", render: row => <div style={{ minWidth: "85px" }}><Badge tone={row.isActive ? "green" : "red"}>{row.isActive ? "Active" : "Inactive"}</Badge></div> }
    ]} actions={row => (
      <div style={{ display: "flex", gap: "8px", minWidth: "120px" }}>
        <button className="ghost-btn" style={{ minWidth: "55px", textAlign: "center" }} onClick={() => setEditingDiscount(row)}>Sửa</button>
        <button className="ghost-btn" style={{ minWidth: "55px", textAlign: "center" }} onClick={async () => { await discountApi.status(row.discountId, !row.isActive); reload(); }}>{row.isActive ? "Tắt" : "Bật"}</button>
      </div>
    )} />
    {editingDiscount && (
      <Modal title="Sửa discount" onClose={() => setEditingDiscount(null)}>
        <SmartForm<Discount> title="" initial={editingDiscount}
          fields={[
            { name: "discountCode", label: "Code", required: true }, { name: "name", label: "Tên", required: true },
            { name: "discountType", label: "Loại", required: true, options: [{ value: "PERCENTAGE", label: "Percent" }, { value: "FIXED_AMOUNT", label: "Số tiền cố định" }] },
            { name: "value", label: "Giá trị", type: "number", required: true }, { name: "maxAmount", label: "Mức giảm tối đa", type: "number" },
            { name: "minTimeSubtotal", label: "Tiền giờ tối thiểu", type: "number" }, { name: "startsAtUtc", label: "Bắt đầu (UTC)", type: "datetime-local", required: true },
            { name: "endsAtUtc", label: "Kết thúc (UTC)", type: "datetime-local" }
          ]} onSubmit={async value => { await discountApi.update(editingDiscount.discountId, value); setEditingDiscount(null); reload(); }} />
      </Modal>
    )}
  </>;
}
