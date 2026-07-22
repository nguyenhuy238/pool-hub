"use client";

import { useState } from "react";
import { Badge, ConfirmDialog, DataTable, ListControls, PageHeader, SmartForm, StateBlock, useList, useLoad, Pagination } from "@/components/ui";
import { getTotalPages } from "@/lib/api/client";
import { inventoryApi } from "@/lib/api/endpoints";
import { productApi } from "@/lib/api/endpoints";
import { useToast } from "@/components/toast";
import type { InventoryTransaction } from "@/types";

type Adjustment = InventoryTransaction & { unitCost?: number; note?: string };
type AdjustmentPayload = { productId: number; quantity: number; transactionType: number; unitCost?: number; note?: string };

export default function InventoryPage() {
  const toast = useToast();
  const [query, setQuery] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const [pendingAdjustment, setPendingAdjustment] = useState<AdjustmentPayload | null>(null);
  const { data, loading, error, reload } = useLoad(() => inventoryApi.list({ Search: query.search, PageNumber: query.pageNumber, PageSize: query.pageSize }), [query]);
  const products = useLoad(() => productApi.list({ PageNumber: 1, PageSize: 100 }), []);
  const rows = useList(data);
  const productRows = useList(products.data);
  return <>
    <PageHeader title="Quản lý tồn kho" description="Theo dõi và thực hiện nhập, xuất, điều chỉnh số lượng hàng hóa." />
    <ListControls {...query} onChange={setQuery} />
    <SmartForm<Adjustment> title="Điều chỉnh tồn kho" initial={{ transactionType: 1 }}
      fields={[
        { name: "productId", label: "Sản phẩm", required: true, options: productRows.map(item => ({ value: String(item.productId), label: `${item.sku} - ${item.name}` })) },
        { name: "transactionType", label: "Loại", required: true, options: [{ value: "1", label: "Nhập kho" }, { value: "2", label: "Xuất kho" }, { value: "3", label: "Điều chỉnh giảm" }] },
        { name: "quantity", label: "Số lượng", type: "number", required: true }, { name: "unitCost", label: "Giá vốn", type: "number" },
        { name: "note", label: "Ghi chú" }
      ]} onSubmit={async value => {
        const payload = { productId: Number(value.productId), quantity: Number(value.quantity), transactionType: Number(value.transactionType), unitCost: value.unitCost ? Number(value.unitCost) : undefined, note: value.note };
        if (!payload.productId) throw new Error("Vui lòng chọn sản phẩm.");
        if (!payload.transactionType) throw new Error("Vui lòng chọn loại giao dịch.");
        if (payload.quantity <= 0) throw new Error("Số lượng phải lớn hơn 0.");
        if (payload.unitCost !== undefined && payload.unitCost < 0) throw new Error("Giá vốn không được âm.");
        setPendingAdjustment(payload);
      }} />
    <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
    <DataTable rows={rows} columns={[
      { key: "productName", label: "Sản phẩm" }, { key: "transactionType", label: "Loại", render: row => <Badge tone={row.transactionType === 1 ? "green" : "yellow"}>{row.transactionType === 1 ? "Nhập" : row.transactionType === 2 ? "Xuất" : row.transactionType === 3 ? "Điều chỉnh" : row.transactionType === 4 ? "Bán hàng" : "Hoàn kho"}</Badge> },
      { key: "quantity", label: "Số lượng" }, { key: "unitCost", label: "Giá vốn" }, { key: "note", label: "Ghi chú" }, { key: "createdAtUtc", label: "Thời gian" }
    ]} />
    <Pagination pageNumber={query.pageNumber} totalPages={getTotalPages(data, query.pageSize)} onChange={(pageNumber) => setQuery({ ...query, pageNumber })} />
    {pendingAdjustment ? <ConfirmDialog title="Xác nhận điều chỉnh tồn kho" message="Ghi nhận giao dịch tồn kho này? Backend sẽ kiểm tra nghiệp vụ tồn kho âm nếu có." confirmLabel="Ghi nhận" danger={pendingAdjustment.transactionType !== 1} onCancel={() => setPendingAdjustment(null)} onConfirm={async () => {
      try {
        await inventoryApi.adjust(pendingAdjustment);
        toast("Đã ghi nhận điều chỉnh tồn kho.", "success");
        setPendingAdjustment(null);
        reload();
      } catch (err) {
        toast(err instanceof Error ? err.message : "Không thể điều chỉnh tồn kho.", "error");
      }
    }} /> : null}
  </>;
}
