"use client";

import { FormEvent, useEffect, useState } from "react";
import { venueApi } from "@/lib/api/endpoints";
import { getTotalPages } from "@/lib/api/client";
import { ConfirmDialog, DataTable, Modal, PageHeader, Pagination, SearchFilterBar, StateBlock, useDebouncedValue } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { TableType } from "@/types";

type TableTypeForm = { name: string; code: string; description: string; defaultCapacity: number };

const emptyForm: TableTypeForm = { name: "", code: "", description: "", defaultCapacity: 4 };

export default function TableTypesPage() {
  const toast = useToast();
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const debouncedSearch = useDebouncedValue(params.search, 350);
  const [data, setData] = useState<{ items?: TableType[]; totalPages?: number; totalItems?: number; totalCount?: number; pageSize?: number } | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [editing, setEditing] = useState<TableType | "new" | null>(null);
  const [detail, setDetail] = useState<TableType | null>(null);
  const [deleting, setDeleting] = useState<TableType | null>(null);
  const rows = data?.items ?? [];

  async function load() {
    setLoading(true);
    setError("");
    try {
      setData(await venueApi.tableTypes({ Search: debouncedSearch || undefined, PageNumber: params.pageNumber, PageSize: params.pageSize }) as typeof data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được danh sách loại bàn.");
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
      await venueApi.deleteTableType(deleting.tableTypeId);
      toast("Đã xóa loại bàn.", "success");
      setDeleting(null);
      await load();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể xóa loại bàn.", "error");
    }
  }

  return <>
    <PageHeader title="Quản lý loại bàn" description="Danh sách các loại bàn như bida lỗ, bida phăng và snooker." action={<button className="primary-btn" onClick={() => setEditing("new")}>Tạo mới</button>} />
    <SearchFilterBar>
      <label><span>Tìm kiếm</span><input value={params.search} onChange={(event) => setParams({ ...params, search: event.target.value, pageNumber: 1 })} placeholder="Tên, mã hoặc mô tả" /></label>
      <label><span>Số dòng</span><select value={params.pageSize} onChange={(event) => setParams({ ...params, pageSize: Number(event.target.value), pageNumber: 1 })}><option>10</option><option>20</option><option>50</option></select></label>
    </SearchFilterBar>
    <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
    {!loading && rows.length ? <>
      <DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[
        { key: "tableTypeId", label: "ID" },
        { key: "code", label: "Mã", render: (row) => <strong>{String(row.code)}</strong> },
        { key: "name", label: "Tên loại bàn" },
        { key: "description", label: "Mô tả" },
        { key: "defaultCapacity", label: "Sức chứa" }
      ]} actions={(row) => {
        const tableType = row as unknown as TableType;
        return <div className="action-group">
          <button className="ghost-btn compact" onClick={() => setDetail(tableType)}>Chi tiết</button>
          <button className="ghost-btn compact" onClick={() => setEditing(tableType)}>Sửa</button>
          <button className="danger-btn compact" onClick={() => setDeleting(tableType)}>Xóa</button>
        </div>;
      }} />
      <Pagination pageNumber={params.pageNumber} totalPages={getTotalPages(data, params.pageSize)} onChange={(pageNumber) => setParams({ ...params, pageNumber })} />
    </> : null}
    {editing ? <TableTypeFormModal tableType={editing === "new" ? null : editing} onClose={() => setEditing(null)} onSaved={async () => { setEditing(null); await load(); }} /> : null}
    {detail ? <TableTypeDetailModal tableType={detail} onClose={() => setDetail(null)} /> : null}
    {deleting ? <ConfirmDialog title="Xóa loại bàn" message={`Xóa loại bàn “${deleting.name}”? Backend sẽ từ chối nếu loại bàn đang được bàn hoặc quy tắc giá sử dụng.`} confirmLabel="Xóa" danger onCancel={() => setDeleting(null)} onConfirm={remove} /> : null}
  </>;
}

function TableTypeFormModal({ tableType, onClose, onSaved }: { tableType: TableType | null; onClose: () => void; onSaved: () => Promise<void> }) {
  const toast = useToast();
  const [form, setForm] = useState<TableTypeForm>(tableType ? { name: tableType.name, code: tableType.code ?? "", description: tableType.description ?? "", defaultCapacity: tableType.defaultCapacity ?? 4 } : emptyForm);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!form.name.trim()) return setError("Vui lòng nhập tên loại bàn.");
    if (!form.code.trim()) return setError("Vui lòng nhập mã loại bàn.");
    if (Number(form.defaultCapacity) <= 0) return setError("Sức chứa phải lớn hơn 0.");
    setSaving(true);
    setError("");
    try {
      const payload = { ...form, name: form.name.trim(), code: form.code.trim(), description: form.description.trim(), defaultCapacity: Number(form.defaultCapacity), isActive: true };
      if (tableType) await venueApi.updateTableType(tableType.tableTypeId, payload);
      else await venueApi.createTableType(payload);
      toast(tableType ? "Đã cập nhật loại bàn." : "Đã tạo loại bàn.", "success");
      await onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể lưu loại bàn.");
    } finally {
      setSaving(false);
    }
  }

  return <Modal title={tableType ? "Sửa loại bàn" : "Tạo loại bàn"} onClose={onClose}>
    <form className="form-stack" onSubmit={submit}>
      {error ? <div className="inline-alert error">{error}</div> : null}
      <label><span>Tên loại bàn</span><input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} /></label>
      <label><span>Mã code</span><input value={form.code} onChange={(event) => setForm({ ...form, code: event.target.value })} /></label>
      <label><span>Mô tả</span><textarea rows={3} value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} /></label>
      <label><span>Sức chứa mặc định</span><input type="number" min={1} value={form.defaultCapacity} onChange={(event) => setForm({ ...form, defaultCapacity: Number(event.target.value) })} /></label>
      <div className="modal-actions"><button type="button" className="ghost-btn" onClick={onClose}>Hủy</button><button className="primary-btn" disabled={saving}>{saving ? "Đang lưu..." : "Lưu"}</button></div>
    </form>
  </Modal>;
}

function TableTypeDetailModal({ tableType, onClose }: { tableType: TableType; onClose: () => void }) {
  return <Modal title={`Chi tiết loại bàn — ${tableType.name}`} onClose={onClose}>
    <div className="audit-detail-grid">
      <div><span>ID</span><strong>{tableType.tableTypeId}</strong></div>
      <div><span>Mã</span><strong>{tableType.code || "-"}</strong></div>
      <div><span>Sức chứa</span><strong>{tableType.defaultCapacity ?? "-"}</strong></div>
      <div><span>Trạng thái</span><strong>{tableType.isActive === false ? "Ngừng hoạt động" : "Đang hoạt động"}</strong></div>
      <div className="full-field"><span>Mô tả</span><p>{tableType.description || "-"}</p></div>
    </div>
    <div className="modal-actions"><button className="ghost-btn" onClick={onClose}>Đóng</button></div>
  </Modal>;
}
