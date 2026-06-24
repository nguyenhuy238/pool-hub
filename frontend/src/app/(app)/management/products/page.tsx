"use client";

import { FormEvent, useEffect, useState } from "react";
import { productApi } from "@/lib/api/endpoints";
import { getTotalPages } from "@/lib/api/client";
import { money } from "@/lib/status";
import { Badge, ConfirmDialog, DataTable, Modal, PageHeader, Pagination, SearchFilterBar, StateBlock, useDebouncedValue } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { Product, ProductCategory } from "@/types";

type ProductForm = { productCategoryId: number; name: string; sku: string; unitPrice: number; stockQuantity: number };

const emptyForm: ProductForm = { productCategoryId: 0, name: "", sku: "", unitPrice: 0, stockQuantity: 0 };

export default function ProductsPage() {
  const toast = useToast();
  const [query, setQuery] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const debouncedSearch = useDebouncedValue(query.search, 350);
  const [data, setData] = useState<{ products: { items?: Product[]; totalPages?: number; totalItems?: number; totalCount?: number; pageSize?: number } | null; categories: ProductCategory[] }>({ products: null, categories: [] });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [editing, setEditing] = useState<Product | "new" | null>(null);
  const [detail, setDetail] = useState<Product | null>(null);
  const [deleting, setDeleting] = useState<Product | null>(null);
  const rows = data.products?.items ?? [];

  async function load() {
    setLoading(true);
    setError("");
    try {
      const [products, categories] = await Promise.all([
        productApi.list({ Search: debouncedSearch || undefined, PageNumber: query.pageNumber, PageSize: query.pageSize }),
        productApi.categories({ PageSize: 200 })
      ]);
      setData({
        products: products as typeof data.products,
        categories: Array.isArray(categories) ? categories : categories.items ?? []
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được sản phẩm.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch, query.pageNumber, query.pageSize]);

  async function remove() {
    if (!deleting) return;
    try {
      await productApi.delete(deleting.productId);
      toast("Đã xóa sản phẩm.", "success");
      setDeleting(null);
      await load();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể xóa sản phẩm.", "error");
    }
  }

  const categoryName = (id?: number) => data.categories.find((item) => item.productCategoryId === Number(id))?.name ?? `#${id ?? "-"}`;

  return <>
    <PageHeader title="Quản lý sản phẩm" description="Quản lý sản phẩm, mã SKU, giá bán và tồn kho." action={<button className="primary-btn" onClick={() => setEditing("new")}>Tạo mới</button>} />
    <SearchFilterBar>
      <label><span>Tìm kiếm</span><input value={query.search} onChange={(event) => setQuery({ ...query, search: event.target.value, pageNumber: 1 })} placeholder="Tên hoặc SKU" /></label>
      <label><span>Số dòng</span><select value={query.pageSize} onChange={(event) => setQuery({ ...query, pageSize: Number(event.target.value), pageNumber: 1 })}><option>10</option><option>20</option><option>50</option></select></label>
    </SearchFilterBar>
    <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
    {!loading && rows.length ? <>
      <DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[
        { key: "name", label: "Tên sản phẩm", render: (row) => <strong>{String(row.name)}</strong> },
        { key: "sku", label: "SKU" },
        { key: "productCategoryId", label: "Danh mục", render: (row) => categoryName(Number(row.productCategoryId)) },
        { key: "unitPrice", label: "Giá", render: (row) => money(Number(row.unitPrice)) },
        { key: "stockQuantity", label: "Kho", render: (row) => Number(row.stockQuantity) <= 5 ? <Badge tone="red">{String(row.stockQuantity)} sắp hết</Badge> : String(row.stockQuantity) }
      ]} actions={(row) => {
        const product = row as unknown as Product;
        return <div className="action-group">
          <button className="ghost-btn compact" onClick={() => setDetail(product)}>Chi tiết</button>
          <button className="ghost-btn compact" onClick={() => setEditing(product)}>Sửa</button>
          <button className="danger-btn compact" onClick={() => setDeleting(product)}>Xóa</button>
        </div>;
      }} />
      <Pagination pageNumber={query.pageNumber} totalPages={getTotalPages(data.products, query.pageSize)} onChange={(pageNumber) => setQuery({ ...query, pageNumber })} />
    </> : null}
    {editing ? <ProductFormModal product={editing === "new" ? null : editing} categories={data.categories} onClose={() => setEditing(null)} onSaved={async () => { setEditing(null); await load(); }} /> : null}
    {detail ? <ProductDetailModal product={detail} categoryName={categoryName(detail.productCategoryId)} onClose={() => setDetail(null)} /> : null}
    {deleting ? <ConfirmDialog title="Xóa sản phẩm" message={`Xóa sản phẩm “${deleting.name}”? Dữ liệu sẽ được ngừng hoạt động thay vì xóa cứng.`} confirmLabel="Xóa" danger onCancel={() => setDeleting(null)} onConfirm={remove} /> : null}
  </>;
}

function ProductFormModal({ product, categories, onClose, onSaved }: { product: Product | null; categories: ProductCategory[]; onClose: () => void; onSaved: () => Promise<void> }) {
  const toast = useToast();
  const [form, setForm] = useState<ProductForm>(product ? {
    productCategoryId: product.productCategoryId,
    name: product.name,
    sku: product.sku,
    unitPrice: product.unitPrice,
    stockQuantity: product.stockQuantity
  } : { ...emptyForm, productCategoryId: categories[0]?.productCategoryId ?? 0 });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!form.productCategoryId) return setError("Vui lòng chọn danh mục.");
    if (!form.name.trim()) return setError("Vui lòng nhập tên sản phẩm.");
    if (!form.sku.trim()) return setError("Vui lòng nhập SKU.");
    if (Number(form.unitPrice) <= 0) return setError("Giá bán phải lớn hơn 0.");
    if (Number(form.stockQuantity) < 0) return setError("Tồn kho không được âm.");
    setSaving(true);
    setError("");
    try {
      const payload = {
        ...form,
        productCategoryId: Number(form.productCategoryId),
        name: form.name.trim(),
        sku: form.sku.trim(),
        unitPrice: Number(form.unitPrice),
        stockQuantity: Number(form.stockQuantity)
      };
      if (product) await productApi.update(product.productId, payload);
      else await productApi.create(payload);
      toast(product ? "Đã cập nhật sản phẩm." : "Đã tạo sản phẩm.", "success");
      await onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể lưu sản phẩm.");
    } finally {
      setSaving(false);
    }
  }

  return <Modal title={product ? "Sửa sản phẩm" : "Tạo sản phẩm"} onClose={onClose} size="large">
    <form className="form-grid modal-form" onSubmit={submit}>
      {error ? <div className="inline-alert error full-field">{error}</div> : null}
      <label><span>Danh mục</span><select value={form.productCategoryId || ""} onChange={(event) => setForm({ ...form, productCategoryId: Number(event.target.value) })}><option value="">Chọn danh mục</option>{categories.map((category) => <option key={category.productCategoryId} value={category.productCategoryId}>{category.name}</option>)}</select></label>
      <label><span>Tên sản phẩm</span><input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} /></label>
      <label><span>SKU</span><input value={form.sku} onChange={(event) => setForm({ ...form, sku: event.target.value })} /></label>
      <label><span>Giá bán</span><input type="number" min={1} value={form.unitPrice} onChange={(event) => setForm({ ...form, unitPrice: Number(event.target.value) })} /></label>
      <label><span>Tồn kho</span><input type="number" min={0} value={form.stockQuantity} onChange={(event) => setForm({ ...form, stockQuantity: Number(event.target.value) })} /></label>
      <div className="modal-actions full-field"><button type="button" className="ghost-btn" onClick={onClose}>Hủy</button><button className="primary-btn" disabled={saving}>{saving ? "Đang lưu..." : "Lưu"}</button></div>
    </form>
  </Modal>;
}

function ProductDetailModal({ product, categoryName, onClose }: { product: Product; categoryName: string; onClose: () => void }) {
  return <Modal title={`Chi tiết sản phẩm — ${product.name}`} onClose={onClose}>
    <div className="audit-detail-grid">
      <div><span>ID</span><strong>{product.productId}</strong></div>
      <div><span>SKU</span><strong>{product.sku}</strong></div>
      <div><span>Danh mục</span><strong>{categoryName}</strong></div>
      <div><span>Giá bán</span><strong>{money(product.unitPrice)}</strong></div>
      <div><span>Tồn kho</span><strong>{product.stockQuantity}</strong></div>
    </div>
    <div className="modal-actions"><button className="ghost-btn" onClick={onClose}>Đóng</button></div>
  </Modal>;
}
