"use client";

import { useState } from "react";
import { Badge, DataTable, ListControls, PageHeader, SmartForm, StateBlock, useList, useLoad } from "@/components/ui";
import { inventoryApi } from "@/lib/api/endpoints";
import { productApi } from "@/lib/api/endpoints";
import type { InventoryTransaction } from "@/types";

type Adjustment = InventoryTransaction & { unitCost?: number; note?: string };

export default function InventoryPage() {
  const [query, setQuery] = useState({ search: "", pageNumber: 1, pageSize: 20 });
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
      ]} onSubmit={async value => { await inventoryApi.adjust({ productId: Number(value.productId), quantity: Number(value.quantity), transactionType: Number(value.transactionType), unitCost: value.unitCost, note: value.note }); reload(); }} />
    <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
    <DataTable rows={rows} columns={[
      { key: "productName", label: "Sản phẩm" }, { key: "transactionType", label: "Loại", render: row => <Badge tone={row.transactionType === 1 ? "green" : "yellow"}>{row.transactionType === 1 ? "Nhập" : row.transactionType === 2 ? "Xuất" : row.transactionType === 3 ? "Điều chỉnh" : row.transactionType === 4 ? "Bán hàng" : "Hoàn kho"}</Badge> },
      { key: "quantity", label: "Số lượng" }, { key: "unitCost", label: "Giá vốn" }, { key: "note", label: "Ghi chú" }, { key: "createdAtUtc", label: "Thời gian" }
    ]} />
  </>;
}
