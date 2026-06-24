"use client";

import { FormEvent, useEffect, useState } from "react";
import { venueApi } from "@/lib/api/endpoints";
import { getTotalPages } from "@/lib/api/client";
import { ConfirmDialog, DataTable, Modal, PageHeader, Pagination, SearchFilterBar, StateBlock, useDebouncedValue } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { Floor } from "@/types";

type FloorForm = { name: string; description: string; displayOrder: number };

const emptyForm: FloorForm = { name: "", description: "", displayOrder: 0 };

export default function FloorsPage() {
  const toast = useToast();
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const debouncedSearch = useDebouncedValue(params.search, 350);
  const [data, setData] = useState<{ items?: Floor[]; totalPages?: number; totalItems?: number; totalCount?: number; pageSize?: number } | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [editing, setEditing] = useState<Floor | "new" | null>(null);
  const [detail, setDetail] = useState<Floor | null>(null);
  const [deleting, setDeleting] = useState<Floor | null>(null);
  const rows = data?.items ?? [];

  async function load() {
    setLoading(true);
    setError("");
    try {
      setData(await venueApi.floors({ Search: debouncedSearch || undefined, PageNumber: params.pageNumber, PageSize: params.pageSize }) as typeof data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được danh sách tầng.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch, params.pageNumber, params.pageSize]);

  async function remove() {
    if (!deleting) return;
    try {
      await venueApi.deleteFloor(deleting.floorId);
      toast("Đã xóa tầng.", "success");
      setDeleting(null);
      await load();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể xóa tầng.", "error");
    }
  }

  return <>
    <PageHeader title="Quản lý tầng" description="Danh sách các tầng trong cơ sở." action={<button className="primary-btn" onClick={() => setEditing("new")}>Tạo mới</button>} />
    <SearchFilterBar>
      <label><span>Tìm kiếm</span><input value={params.search} onChange={(event) => setParams({ ...params, search: event.target.value, pageNumber: 1 })} placeholder="Tên hoặc mô tả" /></label>
      <label><span>Số dòng</span><select value={params.pageSize} onChange={(event) => setParams({ ...params, pageSize: Number(event.target.value), pageNumber: 1 })}><option>10</option><option>20</option><option>50</option></select></label>
    </SearchFilterBar>
    <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
    {!loading && rows.length ? <>
      <DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[
        { key: "floorId", label: "ID" },
        { key: "name", label: "Tên tầng", render: (row) => <strong>{String(row.name)}</strong> },
        { key: "description", label: "Mô tả" },
        { key: "displayOrder", label: "Thứ tự" }
      ]} actions={(row) => {
        const floor = row as unknown as Floor;
        return <div className="action-group">
          <button className="ghost-btn compact" onClick={() => setDetail(floor)}>Chi tiết</button>
          <button className="ghost-btn compact" onClick={() => setEditing(floor)}>Sửa</button>
          <button className="danger-btn compact" onClick={() => setDeleting(floor)}>Xóa</button>
        </div>;
      }} />
      <Pagination pageNumber={params.pageNumber} totalPages={getTotalPages(data, params.pageSize)} onChange={(pageNumber) => setParams({ ...params, pageNumber })} />
    </> : null}
    {editing ? <FloorFormModal floor={editing === "new" ? null : editing} onClose={() => setEditing(null)} onSaved={async () => { setEditing(null); await load(); }} /> : null}
    {detail ? <FloorDetailModal floor={detail} onClose={() => setDetail(null)} /> : null}
    {deleting ? <ConfirmDialog title="Xóa tầng" message={`Xóa tầng “${deleting.name}”? Backend sẽ từ chối nếu tầng còn khu vực đang hoạt động.`} confirmLabel="Xóa" danger onCancel={() => setDeleting(null)} onConfirm={remove} /> : null}
  </>;
}

function FloorFormModal({ floor, onClose, onSaved }: { floor: Floor | null; onClose: () => void; onSaved: () => Promise<void> }) {
  const toast = useToast();
  const [form, setForm] = useState<FloorForm>(floor ? { name: floor.name, description: floor.description ?? "", displayOrder: floor.displayOrder ?? 0 } : emptyForm);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!form.name.trim()) return setError("Vui lòng nhập tên tầng.");
    if (Number(form.displayOrder) < 0) return setError("Thứ tự hiển thị không hợp lệ.");
    setSaving(true);
    setError("");
    try {
      const payload = { ...form, name: form.name.trim(), description: form.description.trim(), displayOrder: Number(form.displayOrder), isActive: true };
      if (floor) await venueApi.updateFloor(floor.floorId, payload);
      else await venueApi.createFloor(payload);
      toast(floor ? "Đã cập nhật tầng." : "Đã tạo tầng.", "success");
      await onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể lưu tầng.");
    } finally {
      setSaving(false);
    }
  }

  return <Modal title={floor ? "Sửa tầng" : "Tạo tầng"} onClose={onClose}>
    <form className="form-stack" onSubmit={submit}>
      {error ? <div className="inline-alert error">{error}</div> : null}
      <label><span>Tên tầng</span><input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} /></label>
      <label><span>Mô tả</span><textarea rows={3} value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} /></label>
      <label><span>Thứ tự hiển thị</span><input type="number" min={0} value={form.displayOrder} onChange={(event) => setForm({ ...form, displayOrder: Number(event.target.value) })} /></label>
      <div className="modal-actions"><button type="button" className="ghost-btn" onClick={onClose}>Hủy</button><button className="primary-btn" disabled={saving}>{saving ? "Đang lưu..." : "Lưu"}</button></div>
    </form>
  </Modal>;
}

function FloorDetailModal({ floor, onClose }: { floor: Floor; onClose: () => void }) {
  return <Modal title={`Chi tiết tầng — ${floor.name}`} onClose={onClose}>
    <div className="audit-detail-grid">
      <div><span>ID</span><strong>{floor.floorId}</strong></div>
      <div><span>Thứ tự</span><strong>{floor.displayOrder ?? 0}</strong></div>
      <div><span>Trạng thái</span><strong>{floor.isActive === false ? "Ngừng hoạt động" : "Đang hoạt động"}</strong></div>
      <div className="full-field"><span>Mô tả</span><p>{floor.description || "-"}</p></div>
    </div>
    <div className="modal-actions"><button className="ghost-btn" onClick={onClose}>Đóng</button></div>
  </Modal>;
}
