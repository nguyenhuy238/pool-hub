"use client";

import { miscApi } from "@/lib/api/endpoints";
import { dateTime } from "@/lib/status";
import { DataTable, PageHeader, StateBlock, useList, useLoad } from "@/components/ui";
import type { Notification } from "@/types";

export default function NotificationsPage() {
  const { data, loading, error } = useLoad(() => miscApi.notifications(), []);
  const rows = useList<Notification>(data);
  return <><PageHeader title="Notifications" /><StateBlock loading={loading} error={error} empty={!loading && !rows.length} /><DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[{ key: "title", label: "Tiêu đề" }, { key: "message", label: "Nội dung" }, { key: "createdAtUtc", label: "Thời gian", render: (row) => dateTime(String(row.createdAtUtc || "")) }]} /></>;
}
