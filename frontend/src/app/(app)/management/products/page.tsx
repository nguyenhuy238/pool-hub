"use client";

import { useState } from "react";
import { productApi } from "@/lib/api/endpoints";
import { money } from "@/lib/status";
import { Badge, DataTable, ListControls, PageHeader, SmartForm, StateBlock, useList, useLoad } from "@/components/ui";
import type { Product } from "@/types";

export default function ProductsPage() {
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const { data, loading, error, reload } = useLoad(() => productApi.list({ Search: params.search, PageNumber: params.pageNumber, PageSize: params.pageSize }), [params]);
  const rows = useList<Product>(data);
  return (
    <>
      <PageHeader title="Quản lý sản phẩm" description="Quản lý sản phẩm, mã SKU, giá bán và tồn kho." />
      <ListControls search={params.search} pageNumber={params.pageNumber} pageSize={params.pageSize} onChange={setParams} />
      <SmartForm<Product> title="Tạo sản phẩm" initial={{ stockQuantity: 0 }} fields={[{ name: "name", label: "Tên sản phẩm", required: true }, { name: "sku", label: "Mã SKU", required: true }, { name: "productCategoryId", label: "Mã danh mục", type: "number", required: true }, { name: "unitPrice", label: "Giá bán", type: "number", required: true }, { name: "stockQuantity", label: "Số lượng tồn", type: "number", required: true }]} onSubmit={async (value) => { await productApi.create(value); reload(); }} />
      <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
      <DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[{ key: "name", label: "Tên" }, { key: "sku", label: "SKU" }, { key: "unitPrice", label: "Giá", render: (row) => money(Number(row.unitPrice)) }, { key: "stockQuantity", label: "Kho", render: (row) => Number(row.stockQuantity) <= 5 ? <Badge tone="red">{String(row.stockQuantity)} low</Badge> : String(row.stockQuantity) }]} actions={(row) => <button className="danger-btn" onClick={() => productApi.delete(Number(row.productId)).then(() => reload())}>Delete</button>} />
    </>
  );
}
