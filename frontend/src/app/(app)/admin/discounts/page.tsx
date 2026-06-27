"use client";

import { useState } from "react";
import { Badge, ConfirmDialog, DataTable, ListControls, PageHeader, SmartForm, StateBlock, useList, useLoad, Modal, Pagination } from "@/components/ui";
import { getTotalPages } from "@/lib/api/client";
import { discountApi } from "@/lib/api/endpoints";
import { useToast } from "@/components/toast";
import type { Discount } from "@/types";

export default function DiscountsPage() {
  const toast = useToast();
  const [query, setQuery] = useState({ search: "", pageNumber: 1, pageSize: 20, isActive: "", discountType: "", appliesTo: "" });
  const [editingDiscount, setEditingDiscount] = useState<Discount | null>(null);
  const [isCreating, setIsCreating] = useState(false);
  const [statusAction, setStatusAction] = useState<Discount | null>(null);
  const { data, loading, error, reload } = useLoad(() => discountApi.list({
    Search: query.search || undefined,
    PageNumber: query.pageNumber,
    PageSize: query.pageSize,
    IsActive: query.isActive === "" ? undefined : query.isActive === "true",
    DiscountType: query.discountType || undefined,
    AppliesTo: query.appliesTo || undefined
  }), [query]);
  const rawRows = useList(data);
  const rows = rawRows.filter((item) => {
    if (query.isActive !== "" && String(item.isActive) !== query.isActive) return false;
    if (query.discountType !== "" && item.discountType !== query.discountType) return false;
    if (query.appliesTo !== "" && item.appliesTo !== query.appliesTo) return false;
    return true;
  });
  return <>
    <PageHeader title="Mã giảm giá" description="Quản lý chương trình giảm giá áp dụng cho tiền giờ chơi." action={<button className="primary-btn" onClick={() => setIsCreating(true)}>Tạo mã giảm giá</button>} />
    <ListControls
      search={query.search}
      pageNumber={query.pageNumber}
      pageSize={query.pageSize}
      onChange={(next) => setQuery((prev) => ({ ...prev, ...next }))}
      extra={
        <>
          <label>
            <span>Trạng thái</span>
            <select value={query.isActive} onChange={(e) => setQuery((prev) => ({ ...prev, isActive: e.target.value, pageNumber: 1 }))}>
              <option value="">Tất cả</option>
              <option value="true">Đang hoạt động</option>
              <option value="false">Ngừng hoạt động</option>
            </select>
          </label>
          <label>
            <span>Hình thức</span>
            <select value={query.discountType} onChange={(e) => setQuery((prev) => ({ ...prev, discountType: e.target.value, pageNumber: 1 }))}>
              <option value="">Tất cả</option>
              <option value="PERCENTAGE">Theo phần trăm</option>
              <option value="FIXED_AMOUNT">Số tiền cố định</option>
            </select>
          </label>
          <label>
            <span>Áp dụng</span>
            <select value={query.appliesTo} onChange={(e) => setQuery((prev) => ({ ...prev, appliesTo: e.target.value, pageNumber: 1 }))}>
              <option value="">Tất cả</option>
              <option value="TIME">Tiền giờ chơi</option>
              <option value="ORDER">Đồ ăn / Dịch vụ</option>
            </select>
          </label>
        </>
      }
    />
    {isCreating && (
      <Modal title="Tạo mã giảm giá" onClose={() => setIsCreating(false)}>
        <SmartForm<Discount> title="" initial={{ discountType: "PERCENTAGE", appliesTo: "TIME", isActive: true }}
          fields={[
            { name: "discountCode", label: "Mã giảm giá", required: true }, { name: "name", label: "Tên chương trình", required: true },
            { name: "discountType", label: "Hình thức giảm", required: true, options: [{ value: "PERCENTAGE", label: "Theo phần trăm" }, { value: "FIXED_AMOUNT", label: "Số tiền cố định" }] },
            { name: "value", label: "Giá trị", type: "number", required: true }, { name: "maxAmount", label: "Mức giảm tối đa", type: "number" },
            { name: "minTimeSubtotal", label: "Tiền giờ tối thiểu", type: "number" }, { name: "startsAtUtc", label: "Bắt đầu (UTC)", type: "datetime-local", required: true },
            { name: "endsAtUtc", label: "Kết thúc (UTC)", type: "datetime-local" }
          ]} onSubmit={async value => { await discountApi.create(value); setIsCreating(false); reload(); }} />
      </Modal>
    )}
    <StateBlock loading={loading && !rows.length} error={error} empty={!loading && !rows.length} />
    <DataTable rows={rows} columns={[
      { key: "discountCode", label: "Mã giảm giá" }, { key: "name", label: "Tên chương trình" }, { key: "discountType", label: "Hình thức" },
      { key: "value", label: "Giá trị" }, { key: "minTimeSubtotal", label: "Tiền giờ tối thiểu" }, { key: "appliesTo", label: "Áp dụng" },
      { key: "isActive", label: "Trạng thái", render: row => <div style={{ minWidth: "110px" }}><Badge tone={row.isActive ? "green" : "red"}>{row.isActive ? "Đang hoạt động" : "Ngừng hoạt động"}</Badge></div> }
    ]} actions={row => (
      <div style={{ display: "flex", gap: "8px", minWidth: "120px" }}>
        <button className="ghost-btn" style={{ minWidth: "55px", textAlign: "center" }} onClick={() => setEditingDiscount(row)}>Sửa</button>
        <button className="ghost-btn" style={{ minWidth: "55px", textAlign: "center" }} onClick={() => setStatusAction(row as Discount)}>{row.isActive ? "Tắt" : "Bật"}</button>
      </div>
    )} />
    <Pagination pageNumber={query.pageNumber} totalPages={getTotalPages(data, query.pageSize)} onChange={(pageNumber) => setQuery({ ...query, pageNumber })} />
    {editingDiscount && (
      <Modal title="Chỉnh sửa mã giảm giá" onClose={() => setEditingDiscount(null)}>
        <SmartForm<Discount> title="" initial={editingDiscount}
          fields={[
            { name: "discountCode", label: "Mã giảm giá", required: true }, { name: "name", label: "Tên chương trình", required: true },
            { name: "discountType", label: "Hình thức giảm", required: true, options: [{ value: "PERCENTAGE", label: "Theo phần trăm" }, { value: "FIXED_AMOUNT", label: "Số tiền cố định" }] },
            { name: "value", label: "Giá trị", type: "number", required: true }, { name: "maxAmount", label: "Mức giảm tối đa", type: "number" },
            { name: "minTimeSubtotal", label: "Tiền giờ tối thiểu", type: "number" }, { name: "startsAtUtc", label: "Bắt đầu (UTC)", type: "datetime-local", required: true },
            { name: "endsAtUtc", label: "Kết thúc (UTC)", type: "datetime-local" }
          ]} onSubmit={async value => { await discountApi.update(editingDiscount.discountId, value); setEditingDiscount(null); reload(); }} />
      </Modal>
    )}
    {statusAction ? <ConfirmDialog title={statusAction.isActive ? "Tắt mã giảm giá" : "Bật mã giảm giá"} message={`${statusAction.isActive ? "Tắt" : "Bật"} mã giảm giá “${statusAction.discountCode}”?`} confirmLabel="Xác nhận" danger={statusAction.isActive} onCancel={() => setStatusAction(null)} onConfirm={async () => {
      try {
        await discountApi.status(statusAction.discountId, !statusAction.isActive);
        toast("Đã cập nhật trạng thái mã giảm giá.", "success");
        setStatusAction(null);
        reload();
      } catch (err) {
        toast(err instanceof Error ? err.message : "Không thể cập nhật mã giảm giá.", "error");
      }
    }} /> : null}
  </>;
}
