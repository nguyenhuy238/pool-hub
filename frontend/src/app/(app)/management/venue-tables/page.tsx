"use client";

import { FormEvent, useEffect, useState } from "react";
import { venueApi } from "@/lib/api/endpoints";
import { getTotalPages } from "@/lib/api/client";
import { label, tableStatus } from "@/lib/status";
import { Badge, ConfirmDialog, DataTable, Modal, PageHeader, Pagination, SearchFilterBar, StateBlock, useDebouncedValue } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { TableType, VenueTable, Zone } from "@/types";

type TableForm = { zoneId: number; tableTypeId: number; tableCode: string; tableName: string; capacity: number; operationalStatus: number };

const emptyForm: TableForm = { zoneId: 0, tableTypeId: 0, tableCode: "", tableName: "", capacity: 4, operationalStatus: 1 };

const STATUS_OPTIONS = [
  { value: 1, label: "Sẵn sàng" },
  { value: 2, label: "Đang có khách" },
  { value: 3, label: "Đã đặt trước" },
  { value: 4, label: "Bảo trì" },
  { value: 5, label: "Ngừng hoạt động" }
];

export default function VenueTablesPage() {
  const toast = useToast();
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const debouncedSearch = useDebouncedValue(params.search, 350);
  const [data, setData] = useState<{ tables: { items?: VenueTable[]; totalPages?: number; totalItems?: number; totalCount?: number; pageSize?: number } | null; zones: Zone[]; tableTypes: TableType[] }>({ tables: null, zones: [], tableTypes: [] });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [editing, setEditing] = useState<VenueTable | "new" | null>(null);
  const [detail, setDetail] = useState<VenueTable | null>(null);
  const [deleting, setDeleting] = useState<VenueTable | null>(null);
  const rows = data.tables?.items ?? [];

  async function load() {
    setLoading(true);
    setError("");
    try {
      const [tables, zones, tableTypes] = await Promise.all([
        venueApi.tables({ Search: debouncedSearch || undefined, PageNumber: params.pageNumber, PageSize: params.pageSize }),
        venueApi.zones({ PageSize: 200 }),
        venueApi.tableTypes({ PageSize: 200 })
      ]);
      setData({
        tables: tables as typeof data.tables,
        zones: Array.isArray(zones) ? zones : zones.items ?? [],
        tableTypes: Array.isArray(tableTypes) ? tableTypes : tableTypes.items ?? []
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được danh sách bàn chơi.");
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
      await venueApi.deleteTable(deleting.tableId);
      toast("Đã xóa bàn chơi.", "success");
      setDeleting(null);
      await load();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể xóa bàn chơi.", "error");
    }
  }

  const zoneName = (id?: number) => data.zones.find((zone) => zone.zoneId === Number(id))?.name ?? `#${id ?? "-"}`;
  const typeName = (id?: number) => data.tableTypes.find((type) => type.tableTypeId === Number(id))?.name ?? `#${id ?? "-"}`;

  return <>
    <PageHeader title="Quản lý bàn chơi" description="Danh sách các bàn chơi trong cơ sở." action={<button className="primary-btn" onClick={() => setEditing("new")}>Tạo mới</button>} />
    <SearchFilterBar>
      <label><span>Tìm kiếm</span><input value={params.search} onChange={(event) => setParams({ ...params, search: event.target.value, pageNumber: 1 })} placeholder="Mã hoặc tên bàn" /></label>
      <label><span>Số dòng</span><select value={params.pageSize} onChange={(event) => setParams({ ...params, pageSize: Number(event.target.value), pageNumber: 1 })}><option>10</option><option>20</option><option>50</option></select></label>
    </SearchFilterBar>
    <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
    {!loading && rows.length ? <>
      <DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[
        { key: "tableCode", label: "Mã bàn", render: (row) => <strong>{String(row.tableCode)}</strong> },
        { key: "tableName", label: "Tên bàn" },
        { key: "zoneId", label: "Khu vực", render: (row) => zoneName(Number(row.zoneId)) },
        { key: "tableTypeId", label: "Loại bàn", render: (row) => typeName(Number(row.tableTypeId)) },
        { key: "capacity", label: "Sức chứa" },
        { key: "operationalStatus", label: "Trạng thái", render: (row) => <Badge tone={Number(row.operationalStatus) === 1 ? "green" : Number(row.operationalStatus) === 2 ? "blue" : Number(row.operationalStatus) === 4 ? "red" : "yellow"}>{label(tableStatus, Number(row.operationalStatus))}</Badge> }
      ]} actions={(row) => {
        const table = row as unknown as VenueTable;
        return <div className="action-group">
          <button className="ghost-btn compact" onClick={() => setDetail(table)}>Chi tiết</button>
          <button className="ghost-btn compact" onClick={() => setEditing(table)}>Sửa</button>
          <button className="danger-btn compact" onClick={() => setDeleting(table)}>Xóa</button>
        </div>;
      }} />
      <Pagination pageNumber={params.pageNumber} totalPages={getTotalPages(data.tables, params.pageSize)} onChange={(pageNumber) => setParams({ ...params, pageNumber })} />
    </> : null}
    {editing ? <TableFormModal table={editing === "new" ? null : editing} zones={data.zones} tableTypes={data.tableTypes} onClose={() => setEditing(null)} onSaved={async () => { setEditing(null); await load(); }} /> : null}
    {detail ? <TableDetailModal table={detail} zoneName={zoneName(detail.zoneId)} typeName={typeName(detail.tableTypeId)} onClose={() => setDetail(null)} /> : null}
    {deleting ? <ConfirmDialog title="Xóa bàn chơi" message={`Xóa bàn “${deleting.tableName}”? Backend sẽ từ chối nếu bàn có phiên chơi hoặc đặt bàn còn hiệu lực.`} confirmLabel="Xóa" danger onCancel={() => setDeleting(null)} onConfirm={remove} /> : null}
  </>;
}

export function TableFormModal({ table, zones, tableTypes, onClose, onSaved }: { table: VenueTable | null; zones: Zone[]; tableTypes: TableType[]; onClose: () => void; onSaved: () => Promise<void> }) {
  const toast = useToast();
  const [form, setForm] = useState<TableForm>(table ? {
    zoneId: table.zoneId,
    tableTypeId: table.tableTypeId,
    tableCode: table.tableCode,
    tableName: table.tableName,
    capacity: table.capacity,
    operationalStatus: table.operationalStatus
  } : { ...emptyForm, zoneId: zones[0]?.zoneId ?? 0, tableTypeId: tableTypes[0]?.tableTypeId ?? 0 });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!form.zoneId) return setError("Vui lòng chọn khu vực.");
    if (!form.tableTypeId) return setError("Vui lòng chọn loại bàn.");
    if (!form.tableCode.trim()) return setError("Vui lòng nhập mã bàn.");
    if (!form.tableName.trim()) return setError("Vui lòng nhập tên bàn.");
    if (Number(form.capacity) <= 0) return setError("Sức chứa phải lớn hơn 0.");
    setSaving(true);
    setError("");
    try {
      const payload = {
        ...form,
        tableCode: form.tableCode.trim(),
        tableName: form.tableName.trim(),
        capacity: Number(form.capacity),
        operationalStatus: Number(form.operationalStatus),
        isActive: true
      };
      if (table) await venueApi.updateTable(table.tableId, payload);
      else await venueApi.createTable(payload);
      toast(table ? "Đã cập nhật bàn chơi." : "Đã tạo bàn chơi.", "success");
      await onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể lưu bàn chơi.");
    } finally {
      setSaving(false);
    }
  }

  return <Modal title={table ? "Sửa bàn chơi" : "Tạo bàn chơi"} onClose={onClose} size="large">
    <form className="form-grid modal-form" onSubmit={submit}>
      {error ? <div className="inline-alert error full-field">{error}</div> : null}
      <label><span>Khu vực</span><select value={form.zoneId || ""} onChange={(event) => setForm({ ...form, zoneId: Number(event.target.value) })}><option value="">Chọn khu vực</option>{zones.map((zone) => <option key={zone.zoneId} value={zone.zoneId}>{zone.name}</option>)}</select></label>
      <label><span>Loại bàn</span><select value={form.tableTypeId || ""} onChange={(event) => setForm({ ...form, tableTypeId: Number(event.target.value) })}><option value="">Chọn loại bàn</option>{tableTypes.map((type) => <option key={type.tableTypeId} value={type.tableTypeId}>{type.name}</option>)}</select></label>
      <label><span>Mã bàn</span><input value={form.tableCode} onChange={(event) => setForm({ ...form, tableCode: event.target.value })} /></label>
      <label><span>Tên bàn</span><input value={form.tableName} onChange={(event) => setForm({ ...form, tableName: event.target.value })} /></label>
      <label><span>Sức chứa</span><input type="number" min={1} value={form.capacity} onChange={(event) => setForm({ ...form, capacity: Number(event.target.value) })} /></label>
      <label><span>Trạng thái</span><select value={form.operationalStatus} onChange={(event) => setForm({ ...form, operationalStatus: Number(event.target.value) })}>{STATUS_OPTIONS.map((status) => <option key={status.value} value={status.value}>{status.label}</option>)}</select></label>
      <div className="modal-actions full-field"><button type="button" className="ghost-btn" onClick={onClose}>Hủy</button><button className="primary-btn" disabled={saving}>{saving ? "Đang lưu..." : "Lưu"}</button></div>
    </form>
  </Modal>;
}

function TableDetailModal({ table, zoneName, typeName, onClose }: { table: VenueTable; zoneName: string; typeName: string; onClose: () => void }) {
  return <Modal title={`Chi tiết bàn — ${table.tableName}`} onClose={onClose}>
    <div className="audit-detail-grid">
      <div><span>Mã bàn</span><strong>{table.tableCode}</strong></div>
      <div><span>Tên bàn</span><strong>{table.tableName}</strong></div>
      <div><span>Khu vực</span><strong>{zoneName}</strong></div>
      <div><span>Loại bàn</span><strong>{typeName}</strong></div>
      <div><span>Sức chứa</span><strong>{table.capacity}</strong></div>
      <div><span>Trạng thái</span><strong>{label(tableStatus, table.operationalStatus)}</strong></div>
      <div><span>Kích hoạt</span><strong>{table.isActive === false ? "Ngừng hoạt động" : "Đang hoạt động"}</strong></div>
    </div>
    <div className="modal-actions"><button className="ghost-btn" onClick={onClose}>Đóng</button></div>
  </Modal>;
}
