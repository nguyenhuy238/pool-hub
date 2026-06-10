"use client";
import { useState } from "react";
import { adminApi } from "@/lib/api/endpoints";
import { Badge, DataTable, ListControls, PageHeader, SmartForm, StateBlock, useList, useLoad } from "@/components/ui";
import type { User } from "@/types";
export default function UsersPage() {
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const { data, loading, error, reload } = useLoad(() => adminApi.users({ Search: params.search, PageNumber: params.pageNumber, PageSize: params.pageSize }), [params]);
  const rows = useList<User>(data);
  return <><PageHeader title="Users" description="Quản lý user, role và trạng thái." /><ListControls search={params.search} pageNumber={params.pageNumber} pageSize={params.pageSize} onChange={setParams} /><SmartForm<User & { password: string; role: string }> title="Tạo user" initial={{ role: "Staff" }} fields={[{ name: "fullName", label: "Họ tên", required: true }, { name: "email", label: "Email", type: "email", required: true }, { name: "password", label: "Mật khẩu", type: "password", required: true }, { name: "role", label: "Role", required: true }]} onSubmit={async (value) => { await adminApi.createUser({ fullName: String(value.fullName || ""), email: String(value.email || ""), password: String(value.password || ""), role: String(value.role || "") }); reload(); }} /><StateBlock loading={loading} error={error} empty={!loading && !rows.length} /><DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[{ key: "fullName", label: "Tên" }, { key: "email", label: "Email" }, { key: "roles", label: "Roles", render: (row) => Array.isArray(row.roles) ? row.roles.join(", ") : "-" }, { key: "status", label: "Trạng thái", render: (row) => <Badge tone={row.status ? "green" : "neutral"}>{row.status ? "Active" : "Inactive"}</Badge> }]} actions={(row) => <button className="ghost-btn" onClick={() => adminApi.updateStatus(Number(row.userId), !row.status).then(() => reload())}>Toggle</button>} /></>;
}
