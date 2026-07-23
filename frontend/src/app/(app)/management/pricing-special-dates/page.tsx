"use client";

import { useState } from "react";
import { pricingSpecialDateApi } from "@/lib/api/endpoints";
import { ConfirmDialog, DataTable, Modal, PageHeader, SmartForm, StateBlock, useList, useLoad, Pagination } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { PricingSpecialDate } from "@/types";

const DAY_TYPES = [
  { value: 3, label: "Ngay le" },
  { value: 4, label: "Ngay dac biet" }
];

export default function PricingSpecialDatesPage() {
  const toast = useToast();
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 10 });
  const [editingItem, setEditingItem] = useState<PricingSpecialDate | null>(null);
  const [deletingItem, setDeletingItem] = useState<PricingSpecialDate | null>(null);
  const { data, loading, error, reload } = useLoad(() => pricingSpecialDateApi.list(params), [params]);
  const items = useList<PricingSpecialDate>(data);
  const totalCount = data && !Array.isArray(data) && "totalCount" in data
    ? Number(data.totalCount)
    : items.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / params.pageSize));

  async function deleteItem() {
    if (!deletingItem) return;
    try {
      await pricingSpecialDateApi.delete(deletingItem.pricingSpecialDateId);
      toast("Da xoa ngay dac biet.", "success");
      setDeletingItem(null);
      await reload();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Khong the xoa.", "error");
    }
  }

  const fields = [
    { name: "date" as const, label: "Ngay", type: "date", required: true },
    { name: "dayType" as const, label: "Loai ngay", options: DAY_TYPES.map((day) => ({ value: day.value.toString(), label: day.label })), required: true },
    { name: "description" as const, label: "Mo ta", required: true }
  ];

  return (
    <>
      <PageHeader title="Cau hinh ngay dac biet" description="Quan ly ngay le va ngay dac biet de ap dung gia khac." />

      <SmartForm<PricingSpecialDate>
        title="Them ngay dac biet"
        initial={{ dayType: 3 } as Partial<PricingSpecialDate>}
        fields={fields}
        onSubmit={async (value) => {
          await pricingSpecialDateApi.create(value);
          toast("Da them thanh cong.", "success");
          await reload();
        }}
      />

      <StateBlock loading={loading} error={error} empty={!loading && !items.length} />

      <DataTable
        rows={items.map((item) => ({ ...item, id: item.pricingSpecialDateId })) as unknown as Record<string, unknown>[]}
        columns={[
          { key: "date", label: "Ngay", render: (row) => new Date(String(row.date)).toLocaleDateString("vi-VN") },
          { key: "dayType", label: "Loai ngay", render: (row) => DAY_TYPES.find((day) => day.value === Number(row.dayType))?.label || String(row.dayType) },
          { key: "description", label: "Mo ta" }
        ]}
        actions={(row) => {
          const item = row as unknown as PricingSpecialDate;
          return (
            <div className="action-group">
              <button className="ghost-btn compact" onClick={() => setEditingItem(item)}>Sua</button>
              <button className="danger-btn compact" onClick={() => setDeletingItem(item)}>Xoa</button>
            </div>
          );
        }}
      />

      <Pagination 
        pageNumber={params.pageNumber}
        totalPages={totalPages}
        onChange={(pageNumber) => setParams({ ...params, pageNumber })}
      />

      {editingItem && (
        <Modal title="Sửa ngày đặc biệt" onClose={() => setEditingItem(null)}>
          <SmartForm<PricingSpecialDate>
            title=""
            initial={editingItem}
            fields={[
              { name: "date", label: "Ngày", type: "date", required: true },
              { name: "dayType", label: "Loại ngày", options: DAY_TYPES.map(d => ({ value: d.value.toString(), label: d.label })), required: true },
              { name: "description", label: "Mô tả", required: true }
            ]}
            onSubmit={async (value) => {
              await pricingSpecialDateApi.update(editingItem.pricingSpecialDateId, value);
              toast("Đã cập nhật.", "success");
              setEditingItem(null);
              await reload();
            }}
          />
        </Modal>
      )}

      {deletingItem && (
        <ConfirmDialog
          title="Xác nhận xóa"
          message={`Bạn có chắc chắn muốn xóa cấu hình ngày ${new Date(deletingItem.date).toLocaleDateString("vi-VN")}?`}
          confirmLabel="Xóa"
          danger
          onConfirm={deleteItem}
          onCancel={() => setDeletingItem(null)}
        />
      )}
    </>
  );
}
