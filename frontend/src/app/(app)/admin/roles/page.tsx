"use client";
import { adminApi } from "@/lib/api/endpoints";
import { DataTable, PageHeader, StateBlock, useLoad } from "@/components/ui";
export default function RolesPage() { const { data, loading, error } = useLoad(() => adminApi.roles(), []); const rows = data || []; return <><PageHeader title="Roles" /><StateBlock loading={loading} error={error} empty={!loading && !rows.length} /><DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[{ key: "roleId", label: "ID" }, { key: "name", label: "Name" }, { key: "roleName", label: "Role Name" }]} /></>; }
