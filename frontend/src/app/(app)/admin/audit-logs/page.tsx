"use client";

import { useEffect, useState } from "react";
import { Badge, DataTable, JsonPreview, Modal, PageHeader, Pagination, SearchFilterBar, StateBlock, useDebouncedValue } from "@/components/ui";
import { dateTime } from "@/lib/status";
import { localDateRangeToUtcRange } from "@/lib/dateTime";
import { auditService } from "@/services/audit-service";
import type { AuditLog, PagedResult } from "@/types";

export default function AuditLogsPage() {
  const [query, setQuery] = useState({ actorUserId: "", action: "", entityName: "", fromDate: "", toDate: "", pageNumber: 1, pageSize: 20 });
  const debouncedAction = useDebouncedValue(query.action, 350);
  const debouncedEntity = useDebouncedValue(query.entityName, 350);
  const debouncedActor = useDebouncedValue(query.actorUserId, 350);
  const [result, setResult] = useState<PagedResult<AuditLog> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [selected, setSelected] = useState<AuditLog | null>(null);

  async function load() {
    setLoading(true);
    setError("");
    try {
      setResult(await auditService.getAuditLogs({
        actorUserId: debouncedActor ? Number(debouncedActor) : undefined,
        action: debouncedAction || undefined,
        entityName: debouncedEntity || undefined,
        fromDate: query.fromDate ? localDateRangeToUtcRange(query.fromDate).fromUtc : undefined,
        toDate: query.toDate ? localDateRangeToUtcRange(query.toDate).toUtc : undefined,
        pageNumber: query.pageNumber,
        pageSize: query.pageSize
      }));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được nhật ký hệ thống.");
    } finally { setLoading(false); }
  }
  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedActor, debouncedAction, debouncedEntity, query.fromDate, query.toDate, query.pageNumber, query.pageSize]);

  async function openDetail(id: number) {
    try {
      setSelected(await auditService.getAuditLogById(id));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được chi tiết nhật ký.");
    }
  }

  const rows = result?.items ?? [];
  return <>
    <PageHeader title="Nhật ký hệ thống" description="Theo dõi các thao tác bảo mật, quản trị và thay đổi dữ liệu quan trọng." />
    <SearchFilterBar>
      <label><span>Mã người thực hiện</span><input inputMode="numeric" placeholder="Ví dụ: 1" value={query.actorUserId} onChange={(e) => setQuery({ ...query, actorUserId: e.target.value.replace(/\D/g, ""), pageNumber: 1 })} /></label>
      <label><span>Hành động</span><input placeholder="Ví dụ: AUTH_LOGIN..." value={query.action} onChange={(e) => setQuery({ ...query, action: e.target.value, pageNumber: 1 })} /></label>
      <label><span>Đối tượng dữ liệu</span><input placeholder="Ví dụ: User, Role..." value={query.entityName} onChange={(e) => setQuery({ ...query, entityName: e.target.value, pageNumber: 1 })} /></label>
      <label><span>Từ ngày</span><input type="date" value={query.fromDate} onChange={(e) => setQuery({ ...query, fromDate: e.target.value, pageNumber: 1 })} /></label>
      <label><span>Đến ngày</span><input type="date" value={query.toDate} onChange={(e) => setQuery({ ...query, toDate: e.target.value, pageNumber: 1 })} /></label>
      <label><span>Số dòng</span><select value={query.pageSize} onChange={(e) => setQuery({ ...query, pageSize: Number(e.target.value), pageNumber: 1 })}><option>10</option><option>20</option><option>50</option></select></label>
    </SearchFilterBar>
    <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
    {!loading && rows.length ? <>
      <DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[
        { key: "createdAtUtc", label: "Thời gian", render: (row) => dateTime(String(row.createdAtUtc)) },
        { key: "actorName", label: "Người thực hiện", render: (row) => <div>{String(row.actorName ?? "Hệ thống")}<div className="table-subtext">{row.actorUserId ? `#${row.actorUserId}` : "Ẩn danh"}</div></div> },
        { key: "action", label: "Hành động", render: (row) => <Badge tone={String(row.action).includes("FAILED") ? "red" : "blue"}>{String(row.action)}</Badge> },
        { key: "entityName", label: "Đối tượng" },
        { key: "entityId", label: "Mã đối tượng" },
        { key: "description", label: "Mô tả" },
        { key: "ipAddress", label: "IP" }
      ]} actions={(row) => <button className="ghost-btn compact" onClick={() => openDetail(Number(row.auditLogId))}>Chi tiết</button>} />
      <Pagination pageNumber={result?.pageNumber ?? 1} totalPages={result?.totalPages ?? 1} onChange={(pageNumber) => setQuery({ ...query, pageNumber })} />
    </> : null}
    {selected ? <Modal title={`Nhật ký #${selected.auditLogId}`} onClose={() => setSelected(null)} size="large">
      <div className="audit-detail-grid">
        <div><span>Hành động</span><strong>{selected.action}</strong></div><div><span>Đối tượng</span><strong>{selected.entityName} #{selected.entityId ?? "-"}</strong></div>
        <div><span>Người thực hiện</span><strong>{selected.actorName ?? "Hệ thống/Ẩn danh"}</strong></div><div><span>Địa chỉ IP</span><strong>{selected.ipAddress ?? "-"}</strong></div>
        <div className="full-field"><span>Mô tả</span><p>{selected.description ?? "-"}</p></div>
        <div className="full-field"><span>Thông tin thiết bị</span><p className="break-text">{selected.userAgent ?? "-"}</p></div>
        <div><span>Giá trị cũ</span><JsonPreview value={selected.oldValues} /></div><div><span>Giá trị mới</span><JsonPreview value={selected.newValues} /></div>
      </div>
    </Modal> : null}
  </>;
}
