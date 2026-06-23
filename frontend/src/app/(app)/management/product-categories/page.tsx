"use client";

import { CrudPage } from "@/components/crud-pages";
import { productApi } from "@/lib/api/endpoints";
import type { ProductCategory } from "@/types";

export default function ProductCategoriesPage() {
  return (
    <CrudPage<ProductCategory>
      title="Product Categories"
      idKey="productCategoryId"
      load={productApi.categories}
      create={productApi.createCategory}
      remove={productApi.deleteCategory}
      fields={[{ name: "name", label: "Tên danh mục", required: true }]}
      columns={[{ key: "productCategoryId", label: "ID" }, { key: "name", label: "Tên" }]}
    />
  );
}
