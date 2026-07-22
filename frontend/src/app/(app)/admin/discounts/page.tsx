"use client";

import { useState } from "react";
import { Badge, ConfirmDialog, DataTable, ListControls, PageHeader, SmartForm, StateBlock, useList, useLoad, Modal, Pagination } from "@/components/ui";
import { getTotalPages } from "@/lib/api/client";
import { discountApi } from "@/lib/api/endpoints";
import { useToast } from "@/components/toast";
import type { Discount } from "@/types";

export default function DiscountsPage() {
  const toast = useToast();
  const [query, setQuery] = useState({ search: "", pageNumber: 1, pageSize: 20, isActive: "", discountType: "", appliesTo: "", isVoucher: "" });
  const [editingDiscount, setEditingDiscount] = useState<Discount | null>(null);
  const [isCreating, setIsCreating] = useState(false);
  const [statusAction, setStatusAction] = useState<Discount | null>(null);
  const { data, loading, error, reload } = useLoad(() => discountApi.list({
    Search: query.search || undefined,
    PageNumber: query.pageNumber,
    PageSize: query.pageSize,
    IsActive: query.isActive === "" ? undefined : query.isActive === "true",
    IsVoucher: query.isVoucher === "" ? undefined : query.isVoucher === "true",
    DiscountType: query.discountType || undefined,
    AppliesTo: query.appliesTo || undefined
  }), [query]);
  const rawRows = useList(data);
  const rows = rawRows.filter((item) => {
    if (query.isActive !== "" && String(item.isActive) !== query.isActive) return false;
    if (query.isVoucher !== "" && String(Boolean(item.isVoucher)) !== query.isVoucher) return false;
    if (query.discountType !== "" && item.discountType !== query.discountType) return false;
    if (query.appliesTo !== "" && item.appliesTo !== query.appliesTo) return false;
    return true;
  });
  return <>
    <PageHeader title="Mã giảm giá & Gói Voucher" description="Quản lý các chương trình khuyến mãi chung và các gói Voucher đổi bằng điểm tích lũy." action={<button className="primary-btn" onClick={() => setIsCreating(true)}>+ Tạo chương trình mới</button>} />
    <ListControls
      search={query.search}
      pageNumber={query.pageNumber}
      pageSize={query.pageSize}
      onChange={(next) => setQuery((prev) => ({ ...prev, ...next }))}
      extra={
        <>
          <label>
            <span>Loại khuyến mãi</span>
            <select value={query.isVoucher} onChange={(e) => setQuery((prev) => ({ ...prev, isVoucher: e.target.value, pageNumber: 1 }))}>
              <option value="false">🎟️ Mã giảm giá thường</option>
              <option value="true">🎁 Gói Voucher đổi thưởng</option>
              <option value="">Tất cả</option>
            </select>
          </label>
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
      <Modal title="Tạo mã giảm giá / Gói Voucher" onClose={() => setIsCreating(false)}>
        <SmartForm<Discount> title="" initial={{ discountType: "PERCENTAGE", appliesTo: "TIME", isActive: true, isVoucher: false, pointsRequired: 0 }}
          fields={[
            { name: "discountCode", label: "Mã giảm giá / Voucher", required: true }, { name: "name", label: "Tên chương trình / Gói voucher", required: true },
            { name: "isVoucher", label: "Loại khuyến mãi", required: true, options: [{ value: "false", label: "🎟️ Mã giảm giá thường" }, { value: "true", label: "🎁 Gói Voucher (đổi bằng điểm)" }] },
            { name: "pointsRequired", label: "Số điểm cần đổi (nếu là Gói Voucher)", type: "number" },
            { name: "discountType", label: "Hình thức giảm", required: true, options: [{ value: "PERCENTAGE", label: "Theo phần trăm" }, { value: "FIXED_AMOUNT", label: "Số tiền cố định" }] },
            { name: "value", label: "Giá trị", type: "number", required: true }, { name: "maxAmount", label: "Mức giảm tối đa", type: "number" },
            { name: "minTimeSubtotal", label: "Tiền giờ tối thiểu", type: "number" }, { name: "startsAtUtc", label: "Bắt đầu (UTC)", type: "datetime-local", required: true },
            { name: "endsAtUtc", label: "Kết thúc (UTC)", type: "datetime-local" }
          ]} onSubmit={async value => {
            const payload = {
              ...value,
              isVoucher: String(value.isVoucher) === "true",
              pointsRequired: value.pointsRequired ? Number(value.pointsRequired) : 0
            };
            await discountApi.create(payload);
            setIsCreating(false);
            reload();
          }} />
      </Modal>
    )}
    <StateBlock loading={loading && !rows.length} error={error} empty={!loading && !rows.length} />
    <DataTable rows={rows} columns={[
      { key: "discountCode", label: "Mã code", render: row => <strong style={{ color: row.isVoucher ? '#7c3aed' : '#0284c7', fontSize: '14px' }}>{row.discountCode}</strong> },
      { key: "name", label: "Tên chương trình / Gói thưởng" },
      { key: "isVoucher", label: "Phân loại", render: row => row.isVoucher ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
          <div><Badge tone="purple">🎁 Gói Voucher đổi điểm</Badge></div>
          <span style={{ fontSize: '12px', color: '#6d28d9', fontWeight: 600 }}>Yêu cầu: {row.pointsRequired?.toLocaleString() || 0} điểm</span>
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
          <div><Badge tone="blue">🎟️ Mã giảm giá thường</Badge></div>
          <span style={{ fontSize: '12px', color: '#0369a1', fontWeight: 600 }}>Dùng trực tiếp (0 điểm)</span>
        </div>
      ) },
      { key: "discountType", label: "Hình thức" },
      { key: "value", label: "Giá trị", render: row => row.discountType === "PERCENTAGE" ? `${row.value}%` : `${row.value?.toLocaleString()} đ` },
      { key: "minTimeSubtotal", label: "Tiền giờ tối thiểu", render: row => row.minTimeSubtotal ? `${row.minTimeSubtotal?.toLocaleString()} đ` : "-" },
      { key: "appliesTo", label: "Áp dụng" },
      { key: "isActive", label: "Trạng thái", render: row => <div style={{ minWidth: "110px" }}><Badge tone={row.isActive ? "green" : "red"}>{row.isActive ? "Đang hoạt động" : "Ngừng hoạt động"}</Badge></div> }
    ]} actions={row => (
      <div style={{ display: "flex", gap: "8px", minWidth: "120px" }}>
        <button className="ghost-btn" style={{ minWidth: "55px", textAlign: "center" }} onClick={() => setEditingDiscount(row)}>Sửa</button>
        <button className="ghost-btn" style={{ minWidth: "55px", textAlign: "center" }} onClick={() => setStatusAction(row as Discount)}>{row.isActive ? "Tắt" : "Bật"}</button>
      </div>
    )} />
    <Pagination pageNumber={query.pageNumber} totalPages={getTotalPages(data, query.pageSize)} onChange={(pageNumber) => setQuery({ ...query, pageNumber })} />
    {editingDiscount && (
      <Modal title="Chỉnh sửa mã giảm giá / Gói Voucher" onClose={() => setEditingDiscount(null)}>
        <SmartForm<Discount> title="" initial={{ ...editingDiscount, isVoucher: Boolean(editingDiscount.isVoucher), pointsRequired: editingDiscount.pointsRequired || 0 }}
          fields={[
            { name: "discountCode", label: "Mã giảm giá / Voucher", required: true }, { name: "name", label: "Tên chương trình / Gói voucher", required: true },
            { name: "isVoucher", label: "Loại khuyến mãi", required: true, options: [{ value: "false", label: "🎟️ Mã giảm giá thường" }, { value: "true", label: "🎁 Gói Voucher (đổi bằng điểm)" }] },
            { name: "pointsRequired", label: "Số điểm cần đổi (nếu là Gói Voucher)", type: "number" },
            { name: "discountType", label: "Hình thức giảm", required: true, options: [{ value: "PERCENTAGE", label: "Theo phần trăm" }, { value: "FIXED_AMOUNT", label: "Số tiền cố định" }] },
            { name: "value", label: "Giá trị", type: "number", required: true }, { name: "maxAmount", label: "Mức giảm tối đa", type: "number" },
            { name: "minTimeSubtotal", label: "Tiền giờ tối thiểu", type: "number" }, { name: "startsAtUtc", label: "Bắt đầu (UTC)", type: "datetime-local", required: true },
            { name: "endsAtUtc", label: "Kết thúc (UTC)", type: "datetime-local" }
          ]} onSubmit={async value => {
            const payload = {
              ...value,
              isVoucher: String(value.isVoucher) === "true",
              pointsRequired: value.pointsRequired ? Number(value.pointsRequired) : 0
            };
            await discountApi.update(editingDiscount.discountId, payload);
            setEditingDiscount(null);
            reload();
          }} />
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
