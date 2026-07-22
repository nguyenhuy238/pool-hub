"use client";

import { FormEvent, useEffect, useState } from "react";
import { productApi } from "@/lib/api/endpoints";
import { getTotalPages } from "@/lib/api/client";
import { ConfirmDialog, DataTable, Modal, PageHeader, Pagination, SearchFilterBar, StateBlock, useDebouncedValue } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { ProductCategory } from "@/types";

export default function ProductCategoriesPage() {
  const toast = useToast();
  const [query, setQuery] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const debouncedSearch = useDebouncedValue(query.search, 350);
  const [data, setData] = useState<{ items?: ProductCategory[]; totalPages?: number; totalItems?: number; totalCount?: number; pageSize?: number } | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [editing, setEditing] = useState<ProductCategory | "new" | null>(null);
  const [detail, setDetail] = useState<ProductCategory | null>(null);
  const [deleting, setDeleting] = useState<ProductCategory | null>(null);
  const rows = data?.items ?? [];

  async function load() {
    setLoading(true);
    setError("");
    try {
      setData(await productApi.categories({ Search: debouncedSearch || undefined, PageNumber: query.pageNumber, PageSize: query.pageSize }) as typeof data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được danh mục sản phẩm.");
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
      await productApi.deleteCategory(deleting.productCategoryId);
      toast("Đã xóa danh mục sản phẩm.", "success");
      setDeleting(null);
      await load();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể xóa danh mục.", "error");
    }
  }

  return <>
    <PageHeader title="Danh mục sản phẩm" description="Quản lý nhóm sản phẩm bán tại quầy." action={<button className="primary-btn" onClick={() => setEditing("new")}>Tạo mới</button>} />
    <SearchFilterBar>
      <label><span>Tìm kiếm</span><input value={query.search} onChange={(event) => setQuery({ ...query, search: event.target.value, pageNumber: 1 })} placeholder="Tên danh mục" /></label>
      <label><span>Số dòng</span><select value={query.pageSize} onChange={(event) => setQuery({ ...query, pageSize: Number(event.target.value), pageNumber: 1 })}><option>10</option><option>20</option><option>50</option></select></label>
    </SearchFilterBar>
    <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
    {!loading && rows.length ? <>
      <DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[
        { key: "productCategoryId", label: "ID" },
        { key: "name", label: "Tên danh mục", render: (row) => <strong>{String(row.name)}</strong> }
      ]} actions={(row) => {
        const item = row as unknown as ProductCategory;
        return <div className="action-group">
          <button className="ghost-btn compact" onClick={() => setDetail(item)}>Chi tiết</button>
          <button className="ghost-btn compact" onClick={() => setEditing(item)}>Sửa</button>
          <button className="danger-btn compact" onClick={() => setDeleting(item)}>Xóa</button>
        </div>;
      }} />
      <Pagination pageNumber={query.pageNumber} totalPages={getTotalPages(data, query.pageSize)} onChange={(pageNumber) => setQuery({ ...query, pageNumber })} />
    </> : null}
    {editing ? <CategoryFormModal category={editing === "new" ? null : editing} onClose={() => setEditing(null)} onSaved={async () => { setEditing(null); await load(); }} /> : null}
    {detail ? <CategoryDetailModal category={detail} onClose={() => setDetail(null)} /> : null}
    {deleting ? <ConfirmDialog title="Xóa danh mục" message={`Xóa danh mục “${deleting.name}”? Backend sẽ từ chối nếu còn sản phẩm đang hoạt động.`} confirmLabel="Xóa" danger onCancel={() => setDeleting(null)} onConfirm={remove} /> : null}
  </>;
}

function CategoryFormModal({ category, onClose, onSaved }: { category: ProductCategory | null; onClose: () => void; onSaved: () => Promise<void> }) {
  const toast = useToast();
  const [name, setName] = useState(category?.name ?? "");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!name.trim()) return setError("Vui lòng nhập tên danh mục.");
    setSaving(true);
    setError("");
    try {
      if (category) await productApi.updateCategory(category.productCategoryId, { ...category, name: name.trim() });
      else await productApi.createCategory({ name: name.trim() });
      toast(category ? "Đã cập nhật danh mục." : "Đã tạo danh mục.", "success");
      await onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể lưu danh mục.");
    } finally {
      setSaving(false);
    }
  }
  return <Modal title={category ? "Sửa danh mục" : "Tạo danh mục"} onClose={onClose}>
    <form className="form-stack" onSubmit={submit}>
      {error ? <div className="inline-alert error">{error}</div> : null}
      <label><span>Tên danh mục</span><input value={name} onChange={(event) => setName(event.target.value)} /></label>
      <div className="modal-actions"><button type="button" className="ghost-btn" onClick={onClose}>Hủy</button><button className="primary-btn" disabled={saving}>{saving ? "Đang lưu..." : "Lưu"}</button></div>
    </form>
  </Modal>;
}

function CategoryDetailModal({ category, onClose }: { category: ProductCategory; onClose: () => void }) {
  return <Modal title={`Chi tiết danh mục — ${category.name}`} onClose={onClose}>
    <div className="audit-detail-grid">
      <div><span>ID</span><strong>{category.productCategoryId}</strong></div>
      <div><span>Tên danh mục</span><strong>{category.name}</strong></div>
      <div><span>Trạng thái</span><strong>{category.isActive === false ? "Ngừng hoạt động" : "Đang hoạt động"}</strong></div>
    </div>
    <div className="modal-actions"><button className="ghost-btn" onClick={onClose}>Đóng</button></div>
  </Modal>;
}
