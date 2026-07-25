"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { useAuth } from "@/components/auth-provider";
import { RoleGuard } from "@/components/guards";
import { useToast } from "@/components/toast";
import { Badge, ConfirmDialog, DataTable, Modal, PageHeader, Pagination, SearchFilterBar, StateBlock, useDebouncedValue } from "@/components/ui";
import { usePagination } from "@/hooks/usePagination";
import { ROLES } from "@/lib/auth/constants";
import { dateTime } from "@/lib/status";
import { roleService, type Permission, type RolePayload } from "@/services/role-service";
import type { PagedResult, Role } from "@/types";

const systemRoleNames = [ROLES.ADMIN, ROLES.MANAGER, ROLES.STAFF];
const permissionLabels: Record<string, { name: string; group: string; description: string }> = {
  "users.manage": { name: "Quản lý người dùng", group: "Người dùng", description: "Xem và cập nhật tài khoản nội bộ theo quyền backend." },
  "roles.manage": { name: "Quản lý vai trò", group: "Vai trò", description: "Xem vai trò, danh mục quyền và cấu hình quyền vai trò." },
  "customers.manage": { name: "Quản lý khách hàng", group: "Khách hàng", description: "Xem hồ sơ, lịch sử và thông tin khách hàng." },
  "venue.manage": { name: "Quản lý cơ sở", group: "Cơ sở", description: "Quản lý tầng, khu vực, loại bàn và bàn chơi." },
  "pricing.manage": { name: "Quản lý bảng giá", group: "Bảng giá", description: "Quản lý gói giá và ngày giá đặc biệt." },
  "products.manage": { name: "Quản lý sản phẩm", group: "Sản phẩm", description: "Quản lý sản phẩm và danh mục sản phẩm." },
  "inventory.manage": { name: "Quản lý tồn kho", group: "Tồn kho", description: "Xem và xử lý nghiệp vụ tồn kho." },
  "discounts.manage": { name: "Quản lý giảm giá", group: "Giảm giá", description: "Xem mã giảm giá và áp dụng nghiệp vụ giảm giá được phép." },
  "payments.manage": { name: "Quản lý thanh toán", group: "Thanh toán", description: "Xem phương thức thanh toán, lịch sử giao dịch và ghi nhận thanh toán." },
  "landing.manage": { name: "Cấu hình trang chủ", group: "Trang chủ", description: "Cập nhật nội dung và media trang public." },
  "reports.view": { name: "Xem báo cáo", group: "Báo cáo", description: "Xem dashboard quản trị và báo cáo." },
  "audit.view": { name: "Xem nhật ký hệ thống", group: "Bảo mật", description: "Xem audit log hệ thống." }
};

function labelForPermission(code: string) {
  return permissionLabels[code]?.name ?? code.replace(".", " ");
}

function groupForPermission(permission: Permission) {
  return permissionLabels[permission.code]?.group ?? permission.group;
}

export default function RolesPage() {
  const { hasRole } = useAuth();
  const toast = useToast();
  const isAdmin = hasRole(ROLES.ADMIN);
  const { query, setQuery, changePage, changePageSize, setFilter } = usePagination({ keyword: "" }, 10);
  const debouncedKeyword = useDebouncedValue(query.keyword, 350);
  const [result, setResult] = useState<PagedResult<Role> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [editing, setEditing] = useState<Role | "new" | null>(null);
  const [detail, setDetail] = useState<Role | null>(null);
  const [deleting, setDeleting] = useState<Role | null>(null);
  const [permissionRole, setPermissionRole] = useState<Role | null>(null);

  async function load() {
    setLoading(true);
    setError("");
    try { setResult(await roleService.getRoles({ ...query, keyword: debouncedKeyword || undefined })); }
    catch (err) { setError(err instanceof Error ? err.message : "Không tải được danh sách vai trò."); }
    finally { setLoading(false); }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedKeyword, query.pageNumber, query.pageSize]);

  async function remove() {
    if (!deleting?.roleId) return;
    try {
      await roleService.deleteRole(deleting.roleId);
      toast("Đã xóa vai trò.", "success");
      setDeleting(null);
      await load();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể xóa vai trò.", "error");
    }
  }

  const rows = result?.items ?? [];
  return <>
    <PageHeader title="Vai trò và quyền hạn" description="Quản lý vai trò, phạm vi truy cập và số lượng người dùng được phân quyền."
      action={<RoleGuard roles={[ROLES.ADMIN]}><button className="primary-btn" onClick={() => setEditing("new")}>+ Tạo vai trò</button></RoleGuard>} />
    <SearchFilterBar>
      <label><span>Tìm kiếm</span><input placeholder="Tên hoặc mô tả" value={query.keyword} onChange={(e) => setFilter("keyword", e.target.value)} /></label>
      <label><span>Số dòng</span><select value={query.pageSize} onChange={(e) => changePageSize(Number(e.target.value))}><option>10</option><option>20</option><option>50</option></select></label>
    </SearchFilterBar>
    <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
    {!loading && rows.length ? <>
      <DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[
        { key: "name", label: "Tên", render: (row) => <strong>{String(row.name)}</strong> },
        { key: "description", label: "Mô tả" },
        { key: "isSystem", label: "Loại", render: (row) => <Badge tone={row.isSystem ? "purple" : "neutral"}>{row.isSystem ? "Hệ thống" : "Tùy chỉnh"}</Badge> },
        { key: "userCount", label: "Người dùng" },
        { key: "createdAtUtc", label: "Ngày tạo", render: (row) => dateTime(String(row.createdAtUtc ?? "")) },
        { key: "updatedAtUtc", label: "Cập nhật", render: (row) => dateTime(String(row.updatedAtUtc ?? "")) }
      ]} actions={isAdmin ? (row) => {
        const role = row as unknown as Role;
        return <div className="action-group"><button className="ghost-btn compact" onClick={() => setDetail(role)}>Chi tiết</button><button className="ghost-btn compact" onClick={() => setPermissionRole(role)}>Quyền</button><button className="ghost-btn compact" onClick={() => setEditing(role)}>Sửa</button><button className="danger-btn compact" disabled={role.isSystem} onClick={() => setDeleting(role)}>Xóa</button></div>;
      } : undefined} />
      <Pagination pageNumber={result?.pageNumber ?? 1} totalPages={result?.totalPages ?? 1} onChange={changePage} />
    </> : null}
    {editing ? <RoleFormModal role={editing === "new" ? null : editing} onClose={() => setEditing(null)} onSaved={async () => { setEditing(null); await load(); }} /> : null}
    {detail ? <RoleDetailModal role={detail} onClose={() => setDetail(null)} /> : null}
    {permissionRole ? <RolePermissionsModal role={permissionRole} onClose={() => setPermissionRole(null)} onSaved={async () => { setPermissionRole(null); await load(); }} /> : null}
    {deleting ? <ConfirmDialog title="Xóa vai trò" message={`Xóa vai trò “${deleting.name}”? Hệ thống sẽ từ chối nếu vai trò vẫn đang được gán cho người dùng.`} confirmLabel="Xóa vai trò" danger onCancel={() => setDeleting(null)} onConfirm={remove} /> : null}
  </>;
}

function RoleDetailModal({ role, onClose }: { role: Role; onClose: () => void }) {
  const [permissions, setPermissions] = useState<Permission[]>([]);

  useEffect(() => {
    void roleService.getPermissions().then(setPermissions).catch(() => setPermissions([]));
  }, []);

  const permissionByCode = useMemo(() => new Map(permissions.map((permission) => [permission.code, permission])), [permissions]);

  return <Modal title={`Chi tiết vai trò — ${role.name}`} onClose={onClose}>
    <div className="audit-detail-grid">
      <div><span>Tên vai trò</span><strong>{role.name}</strong></div>
      <div><span>Loại</span><strong>{role.isSystem ? "Hệ thống" : "Tùy chỉnh"}</strong></div>
      <div><span>Số người dùng</span><strong>{role.userCount ?? 0}</strong></div>
      <div><span>Trạng thái</span><strong>{role.isActive === false ? "Ngừng hoạt động" : "Đang hoạt động"}</strong></div>
      <div><span>Ngày tạo</span><strong>{dateTime(role.createdAtUtc)}</strong></div>
      <div><span>Cập nhật</span><strong>{dateTime(role.updatedAtUtc)}</strong></div>
      <div className="full-field"><span>Mô tả</span><p>{role.description || "-"}</p></div>
      <div className="full-field"><span>Quyền hiện có</span><div className="permission-chip-list">{(role.permissionCodes ?? []).length ? role.permissionCodes?.map((code) => {
        const permission = permissionByCode.get(code);
        return <span className="permission-chip" key={code}><strong>{permission?.name && permission.name !== code ? permission.name : labelForPermission(code)}</strong><small>{code}</small></span>;
      }) : <span className="muted-text">Chưa có quyền</span>}</div></div>
    </div>
    <div className="modal-actions"><button className="ghost-btn" onClick={onClose}>Đóng</button></div>
  </Modal>;
}

function RolePermissionsModal({ role, onClose, onSaved }: { role: Role; onClose: () => void; onSaved: () => Promise<void> }) {
  const toast = useToast();
  const [permissions, setPermissions] = useState<Permission[]>([]);
  const [selected, setSelected] = useState<number[]>([]);
  const [initialSelected, setInitialSelected] = useState<number[]>([]);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    roleService.getPermissions()
      .then((items) => {
        setPermissions(items);
        const codes = new Set(role.permissionCodes ?? []);
        const initial = items.filter((item) => codes.has(item.code)).map((item) => item.permissionId).sort((a, b) => a - b);
        setSelected(initial);
        setInitialSelected(initial);
      })
      .catch((err) => setError(err instanceof Error ? err.message : "Không tải được danh mục quyền."))
      .finally(() => setLoading(false));
  }, [role.permissionCodes]);

  async function save() {
    if (!role.roleId) return;
    setSaving(true);
    setError("");
    try {
      await roleService.setPermissions(role.roleId, selected);
      toast("Đã cập nhật quyền hạn cho vai trò.", "success");
      await onSaved();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể cập nhật quyền hạn.", "error");
    } finally {
      setSaving(false);
    }
  }

  const normalizedSearch = search.trim().toLowerCase();
  const filteredPermissions = permissions.filter((permission) => {
    if (!normalizedSearch) return true;
    return [permission.code, permission.name, permission.description, groupForPermission(permission), labelForPermission(permission.code)]
      .filter(Boolean)
      .some((value) => String(value).toLowerCase().includes(normalizedSearch));
  });
  const groups = filteredPermissions.reduce<Record<string, Permission[]>>((result, permission) => {
    (result[groupForPermission(permission)] ??= []).push(permission);
    return result;
  }, {});
  const normalizedSelected = [...selected].sort((a, b) => a - b).join(",");
  const normalizedInitial = initialSelected.join(",");
  const isDirty = normalizedSelected !== normalizedInitial;
  const selectedSet = new Set(selected);

  function toggleGroup(items: Permission[], checked: boolean) {
    setSelected((current) => {
      const next = new Set(current);
      items.forEach((permission) => checked ? next.add(permission.permissionId) : next.delete(permission.permissionId));
      return [...next].sort((a, b) => a - b);
    });
  }

  return <Modal title={`Quyền hạn — ${role.name}`} onClose={() => { if (!saving) onClose(); }} size="large">
    <div className="role-permission-modal">
      <div className="permission-toolbar">
        <label><span>Tìm quyền</span><input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Tên quyền, module hoặc key" /></label>
        <Badge tone="blue">{selected.length} quyền đã chọn</Badge>
      </div>
      <StateBlock loading={loading} error={error} empty={!loading && !filteredPermissions.length} />
      {!loading && !error ? <div className="permission-scroll">
        {Object.entries(groups).map(([group, items]) => {
          const allChecked = items.every((permission) => selectedSet.has(permission.permissionId));
          return <fieldset className="permission-group" key={group}>
            <legend>{group}</legend>
            <label className="check-row select-all">
              <input type="checkbox" checked={allChecked} onChange={(event) => toggleGroup(items, event.target.checked)} />
              <span>Chọn tất cả trong nhóm</span>
            </label>
            <div className="permission-grid">
              {items.map((permission) => <label className="check-row permission-row" key={permission.permissionId}>
                <input type="checkbox" checked={selectedSet.has(permission.permissionId)} onChange={(event) =>
                  setSelected((current) => event.target.checked
                    ? [...new Set([...current, permission.permissionId])].sort((a, b) => a - b)
                    : current.filter((id) => id !== permission.permissionId))}
                />
                <span><strong>{permission.name && permission.name !== permission.code ? permission.name : labelForPermission(permission.code)}</strong><small>{permission.code}</small><em>{permission.description || permissionLabels[permission.code]?.description}</em></span>
              </label>)}
            </div>
          </fieldset>;
        })}
      </div> : null}
      <div className="modal-actions role-permission-actions"><button className="ghost-btn" onClick={onClose} disabled={saving}>Hủy</button><button className="primary-btn" disabled={saving || !isDirty || loading || Boolean(error)} onClick={save}>{saving ? "Đang lưu..." : "Lưu quyền hạn"}</button></div>
    </div>
  </Modal>;
}

function RoleFormModal({ role, onClose, onSaved }: { role: Role | null; onClose: () => void; onSaved: () => Promise<void> }) {
  const toast = useToast();
  const [form, setForm] = useState<RolePayload>({ name: role?.name ?? "", description: role?.description ?? "", isSystem: false });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  async function submit(event: FormEvent) {
    event.preventDefault();
    const normalized = form.name.trim();
    if (!normalized) return setError("Vui lòng nhập tên vai trò.");
    if (!role && systemRoleNames.some((name) => name.toLowerCase() === normalized.toLowerCase())) return setError("Tên vai trò hệ thống đã tồn tại.");
    setSaving(true);
    setError("");
    try {
      if (role?.roleId) await roleService.updateRole(role.roleId, { name: normalized, description: form.description });
      else await roleService.createRole({ name: normalized, description: form.description, isSystem: false });
      toast(role ? "Cập nhật vai trò thành công." : "Tạo vai trò thành công.", "success");
      await onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể lưu vai trò.");
    } finally { setSaving(false); }
  }
  return <Modal title={role ? "Chỉnh sửa vai trò" : "Tạo vai trò"} onClose={onClose}>
    <form className="form-stack" onSubmit={submit}>
      {role?.isSystem ? <div className="inline-alert warning">Vai trò hệ thống không được đổi tên; chỉ có thể cập nhật mô tả.</div> : null}
      {error ? <div className="inline-alert error">{error}</div> : null}
      <label><span>Tên vai trò</span><input value={form.name} disabled={role?.isSystem} onChange={(e) => setForm({ ...form, name: e.target.value })} /></label>
      <label><span>Mô tả</span><textarea rows={4} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} /></label>
      <div className="modal-actions"><button type="button" className="ghost-btn" onClick={onClose}>Hủy</button><button className="primary-btn" disabled={saving}>{saving ? "Đang lưu..." : "Lưu"}</button></div>
    </form>
  </Modal>;
}
