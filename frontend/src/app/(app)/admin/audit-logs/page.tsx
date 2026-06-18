"use client";

import { useEffect, useState } from "react";
import { Badge, DataTable, JsonPreview, Modal, PageHeader, Pagination, StateBlock } from "@/components/ui";
import { dateTime } from "@/lib/status";
import { auditService } from "@/services/audit-service";
import type { AuditLog, PagedResult } from "@/types";

export default function AuditLogsPage() {
  const [query, setQuery] = useState({ action: "", entityName: "", fromDate: "", toDate: "", pageNumber: 1, pageSize: 20 });
  const [result, setResult] = useState<PagedResult<AuditLog> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [selected, setSelected] = useState<AuditLog | null>(null);

  async function load() {
    setLoading(true);
    setError("");
    try {
      setResult(await auditService.getAuditLogs({
        action: query.action || undefined,
        entityName: query.entityName || undefined,
        fromDate: query.fromDate ? new Date(`${query.fromDate}T00:00:00`).toISOString() : undefined,
        toDate: query.toDate ? new Date(`${query.toDate}T23:59:59`).toISOString() : undefined,
        pageNumber: query.pageNumber,
        pageSize: query.pageSize
      }));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được audit logs.");
    } finally { setLoading(false); }
  }
  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [query.action, query.entityName, query.fromDate, query.toDate, query.pageNumber, query.pageSize]);
  const rows = result?.items ?? [];
  return <>
    <PageHeader title="Audit Logs" description="Theo dõi các thao tác bảo mật và quản trị quan trọng." />
    <div className="card filter-grid">
      <label><span>Action</span><input placeholder="AUTH_LOGIN..." value={query.action} onChange={(e) => setQuery({ ...query, action: e.target.value, pageNumber: 1 })} /></label>
      <label><span>Entity</span><input placeholder="User, Role..." value={query.entityName} onChange={(e) => setQuery({ ...query, entityName: e.target.value, pageNumber: 1 })} /></label>
      <label><span>Từ ngày</span><input type="date" value={query.fromDate} onChange={(e) => setQuery({ ...query, fromDate: e.target.value, pageNumber: 1 })} /></label>
      <label><span>Đến ngày</span><input type="date" value={query.toDate} onChange={(e) => setQuery({ ...query, toDate: e.target.value, pageNumber: 1 })} /></label>
    </div>
    <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
    {!loading && rows.length ? <>
      <DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[
        { key: "createdAtUtc", label: "Thời gian", render: (row) => dateTime(String(row.createdAtUtc)) },
        { key: "actorName", label: "Actor", render: (row) => <div>{String(row.actorName ?? "System")}<div className="table-subtext">{row.actorUserId ? `#${row.actorUserId}` : "Anonymous"}</div></div> },
        { key: "action", label: "Action", render: (row) => <Badge tone={String(row.action).includes("FAILED") ? "red" : "blue"}>{String(row.action)}</Badge> },
        { key: "entityName", label: "Entity" },
        { key: "entityId", label: "Entity ID" },
        { key: "description", label: "Mô tả" },
        { key: "ipAddress", label: "IP" }
      ]} actions={(row) => <button className="ghost-btn compact" onClick={() => setSelected(row as unknown as AuditLog)}>Chi tiết</button>} />
      <Pagination pageNumber={result?.pageNumber ?? 1} totalPages={result?.totalPages ?? 1} onChange={(pageNumber) => setQuery({ ...query, pageNumber })} />
    </> : null}
    {selected ? <Modal title={`Audit #${selected.auditLogId}`} onClose={() => setSelected(null)} size="large">
      <div className="audit-detail-grid">
        <div><span>Action</span><strong>{selected.action}</strong></div><div><span>Entity</span><strong>{selected.entityName} #{selected.entityId ?? "-"}</strong></div>
        <div><span>Actor</span><strong>{selected.actorName ?? "System/Anonymous"}</strong></div><div><span>IP</span><strong>{selected.ipAddress ?? "-"}</strong></div>
        <div className="full-field"><span>Mô tả</span><p>{selected.description ?? "-"}</p></div>
        <div className="full-field"><span>User Agent</span><p className="break-text">{selected.userAgent ?? "-"}</p></div>
        <div><span>Giá trị cũ</span><JsonPreview value={selected.oldValues} /></div><div><span>Giá trị mới</span><JsonPreview value={selected.newValues} /></div>
      </div>
    </Modal> : null}
  </>;
}
