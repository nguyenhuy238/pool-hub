"use client";

import { FormEvent, useEffect, useState } from "react";
import { venueApi } from "@/lib/api/endpoints";
import { getTotalPages } from "@/lib/api/client";
import { ConfirmDialog, DataTable, Modal, PageHeader, Pagination, SearchFilterBar, StateBlock, useDebouncedValue } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { Floor, Zone } from "@/types";

type ZoneForm = { floorId: number; name: string; description: string; displayOrder: number };

const emptyForm: ZoneForm = { floorId: 0, name: "", description: "", displayOrder: 0 };

export default function ZonesPage() {
  const toast = useToast();
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const debouncedSearch = useDebouncedValue(params.search, 350);
  const [data, setData] = useState<{ zones: { items?: Zone[]; totalPages?: number; totalItems?: number; totalCount?: number; pageSize?: number } | null; floors: Floor[] }>({ zones: null, floors: [] });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [editing, setEditing] = useState<Zone | "new" | null>(null);
  const [detail, setDetail] = useState<Zone | null>(null);
  const [deleting, setDeleting] = useState<Zone | null>(null);
  const rows = data.zones?.items ?? [];

  async function load() {
    setLoading(true);
    setError("");
    try {
      const [zones, floors] = await Promise.all([
        venueApi.zones({ Search: debouncedSearch || undefined, PageNumber: params.pageNumber, PageSize: params.pageSize }),
        venueApi.floors({ PageSize: 100 })
      ]);
      setData({ zones: zones as typeof data.zones, floors: Array.isArray(floors) ? floors : floors.items ?? [] });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được danh sách khu vực.");
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
      await venueApi.deleteZone(deleting.zoneId);
      toast("Đã xóa khu vực.", "success");
      setDeleting(null);
      await load();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể xóa khu vực.", "error");
    }
  }

  const floorName = (floorId?: number) => data.floors.find((floor) => floor.floorId === Number(floorId))?.name ?? `#${floorId ?? "-"}`;

  return <>
    <PageHeader title="Quản lý khu vực" description="Danh sách các khu vực trực thuộc từng tầng." action={<button className="primary-btn" onClick={() => setEditing("new")}>Tạo mới</button>} />
    <SearchFilterBar>
      <label><span>Tìm kiếm</span><input value={params.search} onChange={(event) => setParams({ ...params, search: event.target.value, pageNumber: 1 })} placeholder="Tên hoặc mô tả" /></label>
      <label><span>Số dòng</span><select value={params.pageSize} onChange={(event) => setParams({ ...params, pageSize: Number(event.target.value), pageNumber: 1 })}><option>10</option><option>20</option><option>50</option></select></label>
    </SearchFilterBar>
    <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
    {!loading && rows.length ? <>
      <DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[
        { key: "zoneId", label: "ID" },
        { key: "floorId", label: "Tầng", render: (row) => floorName(Number(row.floorId)) },
        { key: "name", label: "Tên khu vực", render: (row) => <strong>{String(row.name)}</strong> },
        { key: "description", label: "Mô tả" },
        { key: "displayOrder", label: "Thứ tự" }
      ]} actions={(row) => {
        const zone = row as unknown as Zone;
        return <div className="action-group">
          <button className="ghost-btn compact" onClick={() => setDetail(zone)}>Chi tiết</button>
          <button className="ghost-btn compact" onClick={() => setEditing(zone)}>Sửa</button>
          <button className="danger-btn compact" onClick={() => setDeleting(zone)}>Xóa</button>
        </div>;
      }} />
      <Pagination pageNumber={params.pageNumber} totalPages={getTotalPages(data.zones, params.pageSize)} onChange={(pageNumber) => setParams({ ...params, pageNumber })} />
    </> : null}
    {editing ? <ZoneFormModal zone={editing === "new" ? null : editing} floors={data.floors} onClose={() => setEditing(null)} onSaved={async () => { setEditing(null); await load(); }} /> : null}
    {detail ? <ZoneDetailModal zone={detail} floorName={floorName(detail.floorId)} onClose={() => setDetail(null)} /> : null}
    {deleting ? <ConfirmDialog title="Xóa khu vực" message={`Xóa khu vực “${deleting.name}”? Backend sẽ từ chối nếu khu vực còn bàn đang hoạt động.`} confirmLabel="Xóa" danger onCancel={() => setDeleting(null)} onConfirm={remove} /> : null}
  </>;
}

function ZoneFormModal({ zone, floors, onClose, onSaved }: { zone: Zone | null; floors: Floor[]; onClose: () => void; onSaved: () => Promise<void> }) {
  const toast = useToast();
  const [form, setForm] = useState<ZoneForm>(zone ? { floorId: zone.floorId, name: zone.name, description: zone.description ?? "", displayOrder: zone.displayOrder ?? 0 } : { ...emptyForm, floorId: floors[0]?.floorId ?? 0 });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!form.floorId) return setError("Vui lòng chọn tầng.");
    if (!form.name.trim()) return setError("Vui lòng nhập tên khu vực.");
    if (Number(form.displayOrder) < 0) return setError("Thứ tự hiển thị không hợp lệ.");
    setSaving(true);
    setError("");
    try {
      const payload = { ...form, name: form.name.trim(), description: form.description.trim(), displayOrder: Number(form.displayOrder), isActive: true };
      if (zone) await venueApi.updateZone(zone.zoneId, payload);
      else await venueApi.createZone(payload);
      toast(zone ? "Đã cập nhật khu vực." : "Đã tạo khu vực.", "success");
      await onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể lưu khu vực.");
    } finally {
      setSaving(false);
    }
  }

  return <Modal title={zone ? "Sửa khu vực" : "Tạo khu vực"} onClose={onClose}>
    <form className="form-stack" onSubmit={submit}>
      {error ? <div className="inline-alert error">{error}</div> : null}
      <label><span>Tầng</span><select value={form.floorId || ""} onChange={(event) => setForm({ ...form, floorId: Number(event.target.value) })}><option value="">Chọn tầng</option>{floors.map((floor) => <option key={floor.floorId} value={floor.floorId}>{floor.name}</option>)}</select></label>
      <label><span>Tên khu vực</span><input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} /></label>
      <label><span>Mô tả</span><textarea rows={3} value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} /></label>
      <label><span>Thứ tự hiển thị</span><input type="number" min={0} value={form.displayOrder} onChange={(event) => setForm({ ...form, displayOrder: Number(event.target.value) })} /></label>
      <div className="modal-actions"><button type="button" className="ghost-btn" onClick={onClose}>Hủy</button><button className="primary-btn" disabled={saving}>{saving ? "Đang lưu..." : "Lưu"}</button></div>
    </form>
  </Modal>;
}

function ZoneDetailModal({ zone, floorName, onClose }: { zone: Zone; floorName: string; onClose: () => void }) {
  return <Modal title={`Chi tiết khu vực — ${zone.name}`} onClose={onClose}>
    <div className="audit-detail-grid">
      <div><span>ID</span><strong>{zone.zoneId}</strong></div>
      <div><span>Tầng</span><strong>{floorName}</strong></div>
      <div><span>Thứ tự</span><strong>{zone.displayOrder ?? 0}</strong></div>
      <div><span>Trạng thái</span><strong>{zone.isActive === false ? "Ngừng hoạt động" : "Đang hoạt động"}</strong></div>
      <div className="full-field"><span>Mô tả</span><p>{zone.description || "-"}</p></div>
    </div>
    <div className="modal-actions"><button className="ghost-btn" onClick={onClose}>Đóng</button></div>
  </Modal>;
}
