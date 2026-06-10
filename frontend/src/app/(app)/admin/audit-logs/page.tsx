"use client";
import { useState } from "react";
import { adminApi } from "@/lib/api/endpoints";
import { dateTime } from "@/lib/status";
import { DataTable, ListControls, PageHeader, StateBlock, useList, useLoad } from "@/components/ui";
import type { AuditLog } from "@/types";
export default function AuditLogsPage() { const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20 }); const { data, loading, error } = useLoad(() => adminApi.auditLogs({ Search: params.search, PageNumber: params.pageNumber, PageSize: params.pageSize }), [params]); const rows = useList<AuditLog>(data); return <><PageHeader title="Audit Logs" /><ListControls search={params.search} pageNumber={params.pageNumber} pageSize={params.pageSize} onChange={setParams} /><StateBlock loading={loading} error={error} empty={!loading && !rows.length} /><DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[{ key: "actor", label: "Actor" }, { key: "action", label: "Action" }, { key: "entity", label: "Entity" }, { key: "ipAddress", label: "IP" }, { key: "createdAtUtc", label: "Created", render: (row) => dateTime(String(row.createdAtUtc || "")) }]} /></>; }
