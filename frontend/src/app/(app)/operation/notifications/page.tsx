"use client";

import { miscApi } from "@/lib/api/endpoints";
import { dateTime } from "@/lib/status";
import { DataTable, PageHeader, StateBlock, useList, useLoad } from "@/components/ui";
import type { Notification } from "@/types";

export default function NotificationsPage() {
  const { data, loading, error } = useLoad(() => miscApi.notifications(), []);
  const rows = useList<Notification>(data);
  return <><PageHeader title="Thông báo" /><StateBlock loading={loading} error={error} empty={!loading && !rows.length} /><DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[{ key: "title", label: "Tiêu đề", render: row => row.isRead ? <span style={{color: 'gray'}}>{String(row.title)}</span> : <strong>{String(row.title)}</strong> }, { key: "message", label: "Nội dung" }, { key: "createdAtUtc", label: "Thời gian", render: (row) => dateTime(String(row.createdAtUtc || "")) }]} actions={row => <button className="danger-btn ghost-btn" onClick={async () => { await miscApi.notificationDelete(Number(row.notificationId)); alert('Đã xóa!'); window.location.reload(); }}>Xóa</button>} /></>;
}
