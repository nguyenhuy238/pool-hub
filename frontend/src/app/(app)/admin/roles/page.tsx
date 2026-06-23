"use client";

import { FormEvent, useEffect, useState } from "react";
import { useAuth } from "@/components/auth-provider";
import { RoleGuard } from "@/components/guards";
import { useToast } from "@/components/toast";
import { Badge, ConfirmDialog, DataTable, Modal, PageHeader, Pagination, StateBlock } from "@/components/ui";
import { ROLES } from "@/lib/auth/constants";
import { dateTime } from "@/lib/status";
import { roleService, type Permission, type RolePayload } from "@/services/role-service";
import type { PagedResult, Role } from "@/types";

export default function RolesPage() {
  const { hasRole } = useAuth();
  const toast = useToast();
  const isAdmin = hasRole(ROLES.ADMIN);
  const [query, setQuery] = useState({ keyword: "", pageNumber: 1, pageSize: 10 });
  const [result, setResult] = useState<PagedResult<Role> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [editing, setEditing] = useState<Role | "new" | null>(null);
  const [deleting, setDeleting] = useState<Role | null>(null);
  const [permissionRole, setPermissionRole] = useState<Role | null>(null);

  async function load() {
    setLoading(true);
    setError("");
    try { setResult(await roleService.getRoles(query)); }
    catch (err) { setError(err instanceof Error ? err.message : "Không tải được danh sách role."); }
    finally { setLoading(false); }
  }

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 250);
    return () => window.clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [query.keyword, query.pageNumber, query.pageSize]);

  async function remove() {
    if (!deleting?.roleId) return;
    try {
      await roleService.deleteRole(deleting.roleId);
      toast("Đã xóa role.", "success");
      setDeleting(null);
      await load();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể xóa role.", "error");
    }
  }

  const rows = result?.items ?? [];
  return <>
    <PageHeader title="Roles" description="Quản lý nhóm quyền truy cập hệ thống."
      action={<RoleGuard roles={[ROLES.ADMIN]}><button className="primary-btn" onClick={() => setEditing("new")}>+ Tạo role</button></RoleGuard>} />
    <div className="card filter-grid compact-filters">
      <label><span>Tìm kiếm</span><input placeholder="Tên hoặc mô tả" value={query.keyword} onChange={(e) => setQuery({ ...query, keyword: e.target.value, pageNumber: 1 })} /></label>
      <label><span>Số dòng</span><select value={query.pageSize} onChange={(e) => setQuery({ ...query, pageSize: Number(e.target.value), pageNumber: 1 })}><option>10</option><option>20</option><option>50</option></select></label>
    </div>
    <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
    {!loading && rows.length ? <>
      <DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[
        { key: "name", label: "Tên", render: (row) => <strong>{String(row.name)}</strong> },
        { key: "description", label: "Mô tả" },
        { key: "isSystem", label: "Loại", render: (row) => <Badge tone={row.isSystem ? "purple" : "neutral"}>{row.isSystem ? "System" : "Custom"}</Badge> },
        { key: "userCount", label: "Người dùng" },
        { key: "createdAtUtc", label: "Ngày tạo", render: (row) => dateTime(String(row.createdAtUtc ?? "")) },
        { key: "updatedAtUtc", label: "Cập nhật", render: (row) => dateTime(String(row.updatedAtUtc ?? "")) }
      ]} actions={isAdmin ? (row) => {
        const role = row as unknown as Role;
        return <div className="action-group"><button className="ghost-btn compact" onClick={() => setPermissionRole(role)}>Quyền</button><button className="ghost-btn compact" onClick={() => setEditing(role)}>Sửa</button><button className="danger-btn compact" disabled={role.isSystem} onClick={() => setDeleting(role)}>Xóa</button></div>;
      } : undefined} />
      <Pagination pageNumber={result?.pageNumber ?? 1} totalPages={result?.totalPages ?? 1} onChange={(pageNumber) => setQuery({ ...query, pageNumber })} />
    </> : null}
    {editing ? <RoleFormModal role={editing === "new" ? null : editing} onClose={() => setEditing(null)} onSaved={async () => { setEditing(null); await load(); }} /> : null}
    {permissionRole ? <RolePermissionsModal role={permissionRole} onClose={() => setPermissionRole(null)} onSaved={async () => { setPermissionRole(null); await load(); }} /> : null}
    {deleting ? <ConfirmDialog title="Xóa role" message={`Xóa role “${deleting.name}”? Role đang được gán sẽ bị backend từ chối.`} confirmLabel="Xóa role" danger onCancel={() => setDeleting(null)} onConfirm={remove} /> : null}
  </>;
}

function RolePermissionsModal({ role, onClose, onSaved }: { role: Role; onClose: () => void; onSaved: () => Promise<void> }) {
  const toast = useToast();
  const [permissions, setPermissions] = useState<Permission[]>([]);
  const [selected, setSelected] = useState<number[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    roleService.getPermissions()
      .then((items) => {
        setPermissions(items);
        const codes = new Set(role.permissionCodes ?? []);
        setSelected(items.filter((item) => codes.has(item.code)).map((item) => item.permissionId));
      })
      .finally(() => setLoading(false));
  }, [role.permissionCodes]);

  async function save() {
    if (!role.roleId) return;
    setSaving(true);
    try {
      await roleService.setPermissions(role.roleId, selected);
      toast("Đã cập nhật permission cho role.", "success");
      await onSaved();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể cập nhật permission.", "error");
    } finally {
      setSaving(false);
    }
  }

  const groups = permissions.reduce<Record<string, Permission[]>>((result, permission) => {
    (result[permission.group] ??= []).push(permission);
    return result;
  }, {});

  return <Modal title={`Permissions — ${role.name}`} onClose={onClose} size="large">
    <StateBlock loading={loading} />
    {!loading ? <div className="form-stack">
      {Object.entries(groups).map(([group, items]) => <div className="card" key={group}>
        <strong>{group}</strong>
        <div className="form-grid" style={{ marginTop: 12 }}>
          {items.map((permission) => <label className="check-row" key={permission.permissionId}>
            <input type="checkbox" checked={selected.includes(permission.permissionId)} onChange={(event) =>
              setSelected((current) => event.target.checked
                ? [...current, permission.permissionId]
                : current.filter((id) => id !== permission.permissionId))}
            />
            <span>{permission.code}</span>
          </label>)}
        </div>
      </div>)}
      <div className="modal-actions"><button className="ghost-btn" onClick={onClose}>Hủy</button><button className="primary-btn" disabled={saving} onClick={save}>{saving ? "Đang lưu..." : "Lưu permissions"}</button></div>
    </div> : null}
  </Modal>;
}

function RoleFormModal({ role, onClose, onSaved }: { role: Role | null; onClose: () => void; onSaved: () => Promise<void> }) {
  const toast = useToast();
  const [form, setForm] = useState<RolePayload>({ name: role?.name ?? "", description: role?.description ?? "", isSystem: false });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!form.name.trim()) return setError("Vui lòng nhập tên role.");
    setSaving(true);
    setError("");
    try {
      if (role?.roleId) await roleService.updateRole(role.roleId, { name: form.name.trim(), description: form.description });
      else await roleService.createRole({ name: form.name.trim(), description: form.description, isSystem: false });
      toast(role ? "Cập nhật role thành công." : "Tạo role thành công.", "success");
      await onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể lưu role.");
    } finally { setSaving(false); }
  }
  return <Modal title={role ? "Chỉnh sửa role" : "Tạo role"} onClose={onClose}>
    <form className="form-stack" onSubmit={submit}>
      {role?.isSystem ? <div className="inline-alert warning">System role không được đổi tên; chỉ nên cập nhật mô tả.</div> : null}
      {error ? <div className="inline-alert error">{error}</div> : null}
      <label><span>Tên role</span><input value={form.name} disabled={role?.isSystem} onChange={(e) => setForm({ ...form, name: e.target.value })} /></label>
      <label><span>Mô tả</span><textarea rows={4} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} /></label>
      <div className="modal-actions"><button type="button" className="ghost-btn" onClick={onClose}>Hủy</button><button className="primary-btn" disabled={saving}>{saving ? "Đang lưu..." : "Lưu"}</button></div>
    </form>
  </Modal>;
}
