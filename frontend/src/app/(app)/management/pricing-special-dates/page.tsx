"use client";
import { useState } from "react";
import { pricingSpecialDateApi } from "@/lib/api/endpoints";
import { DataTable, PageHeader, SmartForm, StateBlock, useList, useLoad, Pagination } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { PricingSpecialDate } from "@/types";

const DAY_TYPES = [
  { value: 3, label: "Ngày lễ" },
  { value: 4, label: "Ngày đặc biệt" }
];

export default function PricingSpecialDatesPage() {
  const toast = useToast();
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 10 });
  const [editingItem, setEditingItem] = useState<PricingSpecialDate | null>(null);
  const [deletingItem, setDeletingItem] = useState<PricingSpecialDate | null>(null);
  const { data, loading, error, reload } = useLoad(() => pricingSpecialDateApi.list(params), [params]);
  const items = useList<PricingSpecialDate>(data);

  async function deleteItem() {
    if (!deletingItem) return;
    try {
      await pricingSpecialDateApi.delete(deletingItem.pricingSpecialDateId);
      toast("Đã xóa ngày đặc biệt.", "success");
      setDeletingItem(null);
      await reload();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể xóa.", "error");
    }
  }

  return (
    <>
      <PageHeader title="Cấu hình ngày đặc biệt" description="Quản lý ngày lễ, ngày đặc biệt để áp dụng giá khác." />

      <SmartForm<PricingSpecialDate>
        title="Thêm ngày đặc biệt"
        initial={{ dayType: 3 } as any}
        fields={[
          { name: "date", label: "Ngày", type: "date", required: true },
          { name: "dayType", label: "Loại ngày", options: DAY_TYPES.map(d => ({ value: d.value.toString(), label: d.label })), required: true },
          { name: "description", label: "Mô tả", required: true }
        ]}
        onSubmit={async (value) => {
          await pricingSpecialDateApi.create(value);
          toast("Đã thêm thành công.", "success");
          reload();
        }}
      />

      <StateBlock loading={loading} error={error} empty={!loading && !items.length} />

      <DataTable
        rows={items.map(p => ({ ...p, id: p.pricingSpecialDateId })) as unknown as Record<string, unknown>[]}
        columns={[
          { key: "date", label: "Ngày", render: (row) => new Date(String(row.date)).toLocaleDateString("vi-VN") },
          { key: "dayType", label: "Loại ngày", render: (row) => DAY_TYPES.find(d => d.value === Number(row.dayType))?.label || String(row.dayType) },
          { key: "description", label: "Mô tả" }
        ]}
        actions={(row) => {
          const item = row as unknown as PricingSpecialDate;
          return <div className="action-group">
            <button className="ghost-btn compact" onClick={() => setEditingItem(item)}>Sửa</button>
            <button className="danger-btn compact" onClick={() => setDeletingItem(item)}>Xóa</button>
          </div>;
        }}
      />

      <Pagination 
        pageNumber={params.pageNumber} 
        pageSize={params.pageSize} 
        totalCount={data && "totalCount" in data ? (data.totalCount as number) : items.length} 
        onPageChange={p => setParams({ ...params, pageNumber: p })} 
      />

      {editingItem && (
        <SmartForm<PricingSpecialDate>
          title="Sửa ngày đặc biệt"
          isModal
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
            reload();
          }}
          onCancel={() => setEditingItem(null)}
        />
      )}

      {deletingItem && (
        <SmartForm
          title="Xác nhận xóa"
          isModal
          initial={{}}
          fields={[]}
          onSubmit={deleteItem}
          onCancel={() => setDeletingItem(null)}
        >
          <p>Bạn có chắc chắn muốn xóa cấu hình ngày <b>{new Date(deletingItem.date).toLocaleDateString("vi-VN")}</b>?</p>
        </SmartForm>
      )}
    </>
  );
}
