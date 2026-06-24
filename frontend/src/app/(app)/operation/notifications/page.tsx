"use client";

import { useEffect, useState } from "react";
import { useToast } from "@/components/toast";
import { Badge, ConfirmDialog, DataTable, PageHeader, Pagination, SearchFilterBar, StateBlock } from "@/components/ui";
import { getTotalPages } from "@/lib/api/client";
import { dateTime } from "@/lib/status";
import { notificationService } from "@/services/notification-service";
import type { Notification, PagedResult } from "@/types";

export default function NotificationsPage() {
  const toast = useToast();
  const [query, setQuery] = useState({ isRead: "", pageNumber: 1, pageSize: 20 });
  const [result, setResult] = useState<PagedResult<Notification> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [deleting, setDeleting] = useState<Notification | null>(null);
  const [busyId, setBusyId] = useState<number | null>(null);
  const rows = result?.items ?? [];

  async function load() {
    setLoading(true);
    setError("");
    try {
      setResult(await notificationService.getNotifications({
        isRead: query.isRead === "" ? undefined : query.isRead === "true",
        pageNumber: query.pageNumber,
        pageSize: query.pageSize
      }));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được thông báo.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [query.isRead, query.pageNumber, query.pageSize]);

  async function markAsRead(id: number) {
    setBusyId(id);
    try {
      await notificationService.markAsRead(id);
      toast("Đã đánh dấu thông báo là đã đọc.", "success");
      await load();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể cập nhật thông báo.", "error");
    } finally {
      setBusyId(null);
    }
  }

  async function markAllAsRead() {
    setBusyId(0);
    try {
      await notificationService.markAllAsRead();
      toast("Đã đánh dấu tất cả thông báo là đã đọc.", "success");
      await load();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể cập nhật thông báo.", "error");
    } finally {
      setBusyId(null);
    }
  }

  async function remove() {
    if (!deleting?.notificationId) return;
    setBusyId(deleting.notificationId);
    try {
      await notificationService.deleteNotification(deleting.notificationId);
      toast("Đã xóa thông báo.", "success");
      setDeleting(null);
      await load();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể xóa thông báo.", "error");
    } finally {
      setBusyId(null);
    }
  }

  return <>
    <PageHeader
      title="Thông báo"
      description="Theo dõi và xử lý các thông báo hệ thống."
      action={<button className="ghost-btn" disabled={busyId === 0 || !rows.some((item) => !item.isRead)} onClick={markAllAsRead}>{busyId === 0 ? "Đang xử lý..." : "Đánh dấu tất cả đã đọc"}</button>}
    />
    <SearchFilterBar>
      <label><span>Trạng thái</span><select value={query.isRead} onChange={(e) => setQuery({ ...query, isRead: e.target.value, pageNumber: 1 })}><option value="">Tất cả</option><option value="false">Chưa đọc</option><option value="true">Đã đọc</option></select></label>
      <label><span>Số dòng</span><select value={query.pageSize} onChange={(e) => setQuery({ ...query, pageSize: Number(e.target.value), pageNumber: 1 })}><option>10</option><option>20</option><option>50</option></select></label>
    </SearchFilterBar>
    <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
    {!loading && rows.length ? <>
      <DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[
        { key: "title", label: "Tiêu đề", render: (row) => row.isRead ? <span className="muted-text">{String(row.title)}</span> : <strong>{String(row.title)}</strong> },
        { key: "message", label: "Nội dung" },
        { key: "isRead", label: "Trạng thái", render: (row) => <Badge tone={row.isRead ? "neutral" : "blue"}>{row.isRead ? "Đã đọc" : "Chưa đọc"}</Badge> },
        { key: "createdAtUtc", label: "Thời gian", render: (row) => dateTime(String(row.createdAtUtc || "")) }
      ]} actions={(row) => {
        const item = row as unknown as Notification;
        return <div className="action-group">
          {!item.isRead ? <button className="ghost-btn compact" disabled={busyId === item.notificationId} onClick={() => markAsRead(item.notificationId)}>Đánh dấu đã đọc</button> : null}
          <button className="danger-btn compact" disabled={busyId === item.notificationId} onClick={() => setDeleting(item)}>Xóa</button>
        </div>;
      }} />
      <Pagination pageNumber={query.pageNumber} totalPages={getTotalPages(result, query.pageSize)} onChange={(pageNumber) => setQuery({ ...query, pageNumber })} />
    </> : null}
    {deleting ? <ConfirmDialog title="Xóa thông báo" message={`Xóa thông báo “${deleting.title || deleting.notificationId}”?`} confirmLabel="Xóa" danger busy={busyId === deleting.notificationId} onCancel={() => setDeleting(null)} onConfirm={remove} /> : null}
  </>;
}
