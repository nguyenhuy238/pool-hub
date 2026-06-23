"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { useAuth } from "@/components/auth-provider";
import { RoleGuard } from "@/components/guards";
import { useToast } from "@/components/toast";
import { Badge, ConfirmDialog, DataTable, Modal, PageHeader, Pagination, StateBlock } from "@/components/ui";
import { FileUploadButton } from "@/components/admin/settings/FileUploadButton";
import { ROLES } from "@/lib/auth/constants";
import { dateTime } from "@/lib/status";
import { validateEmail, validatePassword } from "@/lib/validation";
import { roleService } from "@/services/role-service";
import { userService } from "@/services/user-service";
import type { PagedResult, Role, User } from "@/types";

type UserForm = { fullName: string; email: string; phoneNumber: string; password: string; confirmPassword: string; roleIds: number[] };

const emptyForm: UserForm = { fullName: "", email: "", phoneNumber: "", password: "", confirmPassword: "", roleIds: [] };

export default function UsersPage() {
  const { hasRole, user: currentUser } = useAuth();
  const toast = useToast();
  const isAdmin = hasRole(ROLES.ADMIN);
  const [query, setQuery] = useState({ keyword: "", status: "", roleId: "", pageNumber: 1, pageSize: 10 });
  const [result, setResult] = useState<PagedResult<User> | null>(null);
  const [roles, setRoles] = useState<Role[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [createOpen, setCreateOpen] = useState(false);
  const [selected, setSelected] = useState<User | null>(null);
  const [manageRoles, setManageRoles] = useState<User | null>(null);
  const [statusAction, setStatusAction] = useState<{ user: User; status: "Active" | "Locked" | "Deleted" } | null>(null);

  async function load() {
    setLoading(true);
    setError("");
    try {
      const [usersResult, rolesResult] = await Promise.all([
        userService.getUsers({
          keyword: query.keyword || undefined,
          status: query.status || undefined,
          roleId: query.roleId ? Number(query.roleId) : undefined,
          pageNumber: query.pageNumber,
          pageSize: query.pageSize
        }),
        roleService.getRoles({ pageNumber: 1, pageSize: 100 })
      ]);
      setResult(usersResult);
      setRoles(rolesResult.items ?? []);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được danh sách người dùng.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 250);
    return () => window.clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [query.keyword, query.status, query.roleId, query.pageNumber, query.pageSize]);

  const roleByName = useMemo(() => new Map(roles.map((role) => [role.name, role])), [roles]);

  async function changeStatus() {
    if (!statusAction) return;
    try {
      await userService.updateUserStatus(statusAction.user.userId, statusAction.status);
      toast(`Đã chuyển trạng thái sang ${statusAction.status}.`, "success");
      setStatusAction(null);
      await load();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể cập nhật trạng thái.", "error");
    }
  }

  const rows = result?.items ?? [];
  return (
    <>
      <PageHeader title="Người dùng" description="Tìm kiếm, quản lý trạng thái và phân quyền tài khoản."
        action={<RoleGuard roles={[ROLES.ADMIN]}><button className="primary-btn" onClick={() => setCreateOpen(true)}>+ Tạo người dùng</button></RoleGuard>} />
      <div className="card filter-grid">
        <label><span>Tìm kiếm</span><input placeholder="Tên hoặc email" value={query.keyword} onChange={(e) => setQuery({ ...query, keyword: e.target.value, pageNumber: 1 })} /></label>
        <label><span>Trạng thái</span><select value={query.status} onChange={(e) => setQuery({ ...query, status: e.target.value, pageNumber: 1 })}><option value="">Tất cả</option><option>Active</option><option>Locked</option><option>Deleted</option></select></label>
        <label><span>Role</span><select value={query.roleId} onChange={(e) => setQuery({ ...query, roleId: e.target.value, pageNumber: 1 })}><option value="">Tất cả</option>{roles.map((role) => <option key={role.roleId} value={role.roleId}>{role.name}</option>)}</select></label>
        <label><span>Số dòng</span><select value={query.pageSize} onChange={(e) => setQuery({ ...query, pageSize: Number(e.target.value), pageNumber: 1 })}><option>10</option><option>20</option><option>50</option></select></label>
      </div>
      <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
      {!loading && rows.length ? (
        <>
          <DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[
            { key: "fullName", label: "Người dùng", render: (row) => <div><strong>{String(row.fullName)}</strong><div className="table-subtext">{String(row.email)}</div></div> },
            { key: "phoneNumber", label: "Điện thoại" },
            { key: "roles", label: "Roles", render: (row) => <div className="badge-list">{(row.roles as string[]).map((role) => <Badge key={role} tone="blue">{role}</Badge>)}</div> },
            { key: "status", label: "Trạng thái", render: (row) => <Badge tone={row.status === "Active" ? "green" : row.status === "Locked" ? "yellow" : "red"}>{String(row.status)}</Badge> },
            { key: "emailConfirmed", label: "Email", render: (row) => <Badge tone={row.emailConfirmed ? "green" : "neutral"}>{row.emailConfirmed ? "Đã xác nhận" : "Chưa xác nhận"}</Badge> },
            { key: "lastLoginAtUtc", label: "Đăng nhập cuối", render: (row) => dateTime(String(row.lastLoginAtUtc ?? "")) },
            { key: "createdAtUtc", label: "Ngày tạo", render: (row) => dateTime(String(row.createdAtUtc ?? "")) }
          ]} actions={(row) => {
            const item = row as unknown as User;
            return <div className="action-group">
              <button className="ghost-btn compact" onClick={() => setSelected(item)}>Chi tiết</button>
              {isAdmin ? <button className="ghost-btn compact" onClick={() => setManageRoles(item)}>Roles</button> : null}
              {isAdmin && item.userId !== currentUser?.userId ? <button className="ghost-btn compact" onClick={() => setStatusAction({ user: item, status: item.status === "Active" ? "Locked" : "Active" })}>{item.status === "Active" ? "Khóa" : "Mở khóa"}</button> : null}
              {isAdmin && item.status !== "Deleted" && item.userId !== currentUser?.userId ? <button className="danger-btn compact" onClick={() => setStatusAction({ user: item, status: "Deleted" })}>Xóa</button> : null}
            </div>;
          }} />
          <Pagination pageNumber={result?.pageNumber ?? 1} totalPages={result?.totalPages ?? 1} onChange={(pageNumber) => setQuery({ ...query, pageNumber })} />
        </>
      ) : null}
      {createOpen ? <CreateUserModal roles={roles} onClose={() => setCreateOpen(false)} onSaved={async () => { setCreateOpen(false); await load(); }} /> : null}
      {selected ? <UserDetailModal userId={selected.userId} editable={isAdmin || hasRole(ROLES.MANAGER)} onClose={() => setSelected(null)} onSaved={async () => { setSelected(null); await load(); }} /> : null}
      {manageRoles ? <ManageRolesModal user={manageRoles} roles={roles} roleByName={roleByName} onClose={() => setManageRoles(null)} onSaved={load} /> : null}
      {statusAction ? <ConfirmDialog title="Xác nhận thay đổi trạng thái" message={`Chuyển ${statusAction.user.fullName} sang trạng thái ${statusAction.status}?`} confirmLabel="Xác nhận" danger={statusAction.status !== "Active"} onCancel={() => setStatusAction(null)} onConfirm={changeStatus} /> : null}
    </>
  );
}

function CreateUserModal({ roles, onClose, onSaved }: { roles: Role[]; onClose: () => void; onSaved: () => Promise<void> }) {
  const toast = useToast();
  const [form, setForm] = useState<UserForm>(emptyForm);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  async function submit(event: FormEvent) {
    event.preventDefault();
    const validation = !form.fullName.trim() ? "Vui lòng nhập họ tên." : validateEmail(form.email)
      || validatePassword(form.password) || (form.password !== form.confirmPassword ? "Xác nhận mật khẩu không khớp." : "")
      || (!form.roleIds.length ? "Vui lòng chọn ít nhất một role." : "");
    if (validation) return setError(validation);
    setSaving(true);
    try {
      await userService.createUser({ ...form, fullName: form.fullName.trim(), email: form.email.trim() });
      toast("Tạo người dùng thành công.", "success");
      await onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể tạo người dùng.");
    } finally { setSaving(false); }
  }
  return <Modal title="Tạo người dùng" onClose={onClose} size="large"><form className="form-grid modal-form" onSubmit={submit}>
    {error ? <div className="inline-alert error full-field">{error}</div> : null}
    <label><span>Họ tên</span><input value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} /></label>
    <label><span>Email</span><input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} /></label>
    <label><span>Số điện thoại</span><input value={form.phoneNumber} onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })} /></label>
    <label><span>Mật khẩu</span><input type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} /></label>
    <label><span>Xác nhận mật khẩu</span><input type="password" value={form.confirmPassword} onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })} /></label>
    <fieldset className="role-options full-field"><legend>Roles</legend>{roles.map((role) => <label className="check-option" key={role.roleId}><input type="checkbox" checked={form.roleIds.includes(role.roleId!)} onChange={(e) => setForm({ ...form, roleIds: e.target.checked ? [...form.roleIds, role.roleId!] : form.roleIds.filter((id) => id !== role.roleId) })} />{role.name}</label>)}</fieldset>
    <div className="modal-actions full-field"><button type="button" className="ghost-btn" onClick={onClose}>Hủy</button><button className="primary-btn" disabled={saving}>{saving ? "Đang tạo..." : "Tạo người dùng"}</button></div>
  </form></Modal>;
}

function UserDetailModal({ userId, editable, onClose, onSaved }: { userId: number; editable: boolean; onClose: () => void; onSaved: () => Promise<void> }) {
  const toast = useToast();
  const [user, setUser] = useState<User | null>(null);
  const [form, setForm] = useState({ fullName: "", phoneNumber: "", avatarUrl: "", emailConfirmed: false });
  const [saving, setSaving] = useState(false);
  useEffect(() => { void userService.getUserById(userId).then((value) => { setUser(value); setForm({ fullName: value.fullName, phoneNumber: value.phoneNumber ?? "", avatarUrl: value.avatarUrl ?? "", emailConfirmed: Boolean(value.emailConfirmed) }); }); }, [userId]);
  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!form.fullName.trim()) return;
    setSaving(true);
    try { await userService.updateUser(userId, form); toast("Cập nhật người dùng thành công.", "success"); await onSaved(); }
    catch (err) { toast(err instanceof Error ? err.message : "Không thể cập nhật.", "error"); }
    finally { setSaving(false); }
  }
  return <Modal title="Chi tiết người dùng" onClose={onClose} size="large">{!user ? <StateBlock loading /> : <form className="form-grid modal-form" onSubmit={submit}>
    <label><span>Email</span><input value={user.email} disabled /></label>
    <label><span>Trạng thái</span><input value={user.status} disabled /></label>
    <label><span>Ngày tạo</span><input value={dateTime(user.createdAtUtc)} disabled /></label>
    <label><span>Đăng nhập cuối</span><input value={dateTime(user.lastLoginAtUtc)} disabled /></label>
    <label><span>Họ tên</span><input value={form.fullName} disabled={!editable} onChange={(e) => setForm({ ...form, fullName: e.target.value })} /></label>
    <label><span>Số điện thoại</span><input value={form.phoneNumber} disabled={!editable} onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })} /></label>
    <label className="full-field"><span>Avatar URL</span><input value={form.avatarUrl} disabled={!editable} onChange={(e) => setForm({ ...form, avatarUrl: e.target.value })} /></label>
    {editable ? <div className="full-field"><FileUploadButton mediaType="image" folder="avatars" altText={form.fullName} onUploaded={(asset) => setForm({ ...form, avatarUrl: asset.url })} /></div> : null}
    <label className="check-option full-field"><input type="checkbox" checked={form.emailConfirmed} disabled={!editable} onChange={(e) => setForm({ ...form, emailConfirmed: e.target.checked })} />Email đã xác nhận</label>
    <div className="modal-actions full-field"><button type="button" className="ghost-btn" onClick={onClose}>Đóng</button>{editable ? <button className="primary-btn" disabled={saving}>{saving ? "Đang lưu..." : "Lưu thay đổi"}</button> : null}</div>
  </form>}</Modal>;
}

function ManageRolesModal({ user, roles, roleByName, onClose, onSaved }: { user: User; roles: Role[]; roleByName: Map<string | undefined, Role>; onClose: () => void; onSaved: () => Promise<void> }) {
  const toast = useToast();
  const [selected, setSelected] = useState<number[]>([]);
  const [busy, setBusy] = useState(false);
  const currentRoleIds = user.roles.map((name) => roleByName.get(name)?.roleId).filter((id): id is number => Boolean(id));
  const available = roles.filter((role) => role.roleId && !currentRoleIds.includes(role.roleId));
  async function assign() {
    if (!selected.length) return;
    setBusy(true);
    try { await userService.assignRoles(user.userId, selected); toast("Đã gán role.", "success"); onClose(); await onSaved(); }
    catch (err) { toast(err instanceof Error ? err.message : "Không thể gán role.", "error"); }
    finally { setBusy(false); }
  }
  async function remove(roleId: number) {
    setBusy(true);
    try { await userService.removeRole(user.userId, roleId); toast("Đã gỡ role.", "success"); onClose(); await onSaved(); }
    catch (err) { toast(err instanceof Error ? err.message : "Không thể gỡ role.", "error"); }
    finally { setBusy(false); }
  }
  return <Modal title={`Quản lý role — ${user.fullName}`} onClose={onClose}>
    <div className="role-manager"><h3>Role hiện tại</h3>{user.roles.map((name) => <div className="role-row" key={name}><Badge tone="blue">{name}</Badge><button className="danger-btn compact" disabled={busy || user.roles.length <= 1} onClick={() => remove(roleByName.get(name)?.roleId ?? 0)}>Gỡ</button></div>)}
      <h3>Thêm role</h3><div className="role-options">{available.map((role) => <label className="check-option" key={role.roleId}><input type="checkbox" checked={selected.includes(role.roleId!)} onChange={(e) => setSelected(e.target.checked ? [...selected, role.roleId!] : selected.filter((id) => id !== role.roleId))} />{role.name}</label>)}</div>
      <div className="modal-actions"><button className="ghost-btn" onClick={onClose}>Đóng</button><button className="primary-btn" disabled={busy || !selected.length} onClick={assign}>Gán role</button></div>
    </div>
  </Modal>;
}
