"use client";

import { useState } from "react";
import { pricingSpecialDateApi } from "@/lib/api/endpoints";
import { ConfirmDialog, DataTable, Modal, PageHeader, Pagination, SmartForm, StateBlock, useList, useLoad } from "@/components/ui";
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
  const totalItems = data && typeof data === "object" && "totalCount" in data ? Number(data.totalCount) : items.length;

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
        totalPages={Math.max(1, Math.ceil(totalItems / params.pageSize))}
        onChange={(pageNumber) => setParams({ ...params, pageNumber })}
      />

      {editingItem ? (
        <Modal title="Sua ngay dac biet" onClose={() => setEditingItem(null)}>
          <SmartForm<PricingSpecialDate>
            title=""
            initial={editingItem}
            fields={fields}
            onSubmit={async (value) => {
              await pricingSpecialDateApi.update(editingItem.pricingSpecialDateId, value);
              toast("Da cap nhat.", "success");
              setEditingItem(null);
              await reload();
            }}
          />
        </Modal>
      ) : null}

      {deletingItem ? (
        <ConfirmDialog
          title="Xac nhan xoa"
          message={`Ban co chac chan muon xoa cau hinh ngay ${new Date(deletingItem.date).toLocaleDateString("vi-VN")}?`}
          danger
          confirmLabel="Xoa"
          onConfirm={deleteItem}
          onCancel={() => setDeletingItem(null)}
        />
      ) : null}
    </>
  );
}
