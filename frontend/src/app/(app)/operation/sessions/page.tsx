"use client";

import { useState } from "react";
import { sessionApi } from "@/lib/api/endpoints";
import { getTotalPages } from "@/lib/api/client";
import { dateTime, label, sessionStatus } from "@/lib/status";
import { Badge, ConfirmDialog, DataTable, ListControls, PageHeader, Pagination, SmartForm, StateBlock, useList, useLoad } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { Session } from "@/types";

export default function SessionsPage() {
  const toast = useToast();
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const [ending, setEnding] = useState<Session | null>(null);
  const { data, loading, error, reload } = useLoad(() => sessionApi.list({ Search: params.search, PageNumber: params.pageNumber, PageSize: params.pageSize }), [params]);
  const rows = useList<Session>(data);
  async function endSelected() {
    if (!ending) return;
    await sessionApi.end(ending.sessionId).then(() => toast("Đã đóng phiên chơi.", "success")).catch((err) => toast(err.message, "error"));
    setEnding(null);
    reload();
  }
  return (
    <>
      <PageHeader title="Quản lý phiên chơi" description="Mở, kết thúc và theo dõi các phiên chơi." />
      <ListControls search={params.search} pageNumber={params.pageNumber} pageSize={params.pageSize} onChange={setParams} />
      <SmartForm<Session> title="Mở phiên chơi" initial={{}} fields={[{ name: "tableId", label: "Mã bàn", type: "number", required: true }, { name: "bookingId", label: "Mã đặt bàn", type: "number" }, { name: "customerId", label: "Mã khách hàng", type: "number" }]} onSubmit={async (value) => { await sessionApi.start({ tableId: Number(value.tableId), bookingId: value.bookingId ? Number(value.bookingId) : undefined, customerId: value.customerId ? Number(value.customerId) : undefined }); reload(); }} />
      <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
      <DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[
        { key: "sessionCode", label: "Mã" },
        { key: "startedAtUtc", label: "Bắt đầu", render: (row) => dateTime(String(row.startedAtUtc)) },
        { key: "endedAtUtc", label: "Kết thúc", render: (row) => dateTime(String(row.endedAtUtc || "")) },
        { key: "status", label: "Trạng thái", render: (row) => <Badge tone={Number(row.status) === 1 ? "green" : "neutral"}>{label(sessionStatus, Number(row.status))}</Badge> }
      ]} actions={(row) => <button className="danger-btn" disabled={Number(row.status) !== 1} onClick={() => setEnding(row as unknown as Session)}>Kết thúc</button>} />
      <Pagination pageNumber={params.pageNumber} totalPages={getTotalPages(data, params.pageSize)} onChange={(pageNumber) => setParams({ ...params, pageNumber })} />
      {ending ? <ConfirmDialog title="Kết thúc phiên chơi" message={`Xác nhận kết thúc phiên ${ending.sessionCode || ending.sessionId}?`} confirmLabel="Kết thúc" danger onCancel={() => setEnding(null)} onConfirm={endSelected} /> : null}
    </>
  );
}
