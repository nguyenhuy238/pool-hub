"use client";

import { useState } from "react";
import { sessionApi } from "@/lib/api/endpoints";
import { dateTime, label, sessionStatus } from "@/lib/status";
import { Badge, DataTable, ListControls, PageHeader, SmartForm, StateBlock, useList, useLoad } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { Session } from "@/types";

export default function SessionsPage() {
  const toast = useToast();
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const { data, loading, error, reload } = useLoad(() => sessionApi.list({ Search: params.search, PageNumber: params.pageNumber, PageSize: params.pageSize }), [params]);
  const rows = useList<Session>(data);
  return (
    <>
      <PageHeader title="Session Management" description="Mở, đóng và theo dõi phiên chơi." />
      <ListControls search={params.search} pageNumber={params.pageNumber} pageSize={params.pageSize} onChange={setParams} />
      <SmartForm<Session> title="Mở session" initial={{}} fields={[{ name: "tableId", label: "Table ID", type: "number", required: true }, { name: "bookingId", label: "Booking ID", type: "number" }, { name: "customerId", label: "Customer ID", type: "number" }]} onSubmit={async (value) => { await sessionApi.start({ tableId: Number(value.tableId), bookingId: value.bookingId ? Number(value.bookingId) : undefined, customerId: value.customerId ? Number(value.customerId) : undefined }); reload(); }} />
      <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
      <DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[
        { key: "sessionCode", label: "Mã" },
        { key: "startedAtUtc", label: "Bắt đầu", render: (row) => dateTime(String(row.startedAtUtc)) },
        { key: "endedAtUtc", label: "Kết thúc", render: (row) => dateTime(String(row.endedAtUtc || "")) },
        { key: "status", label: "Trạng thái", render: (row) => <Badge tone={Number(row.status) === 1 ? "green" : "neutral"}>{label(sessionStatus, Number(row.status))}</Badge> }
      ]} actions={(row) => <button className="danger-btn" onClick={async () => { await sessionApi.end(Number(row.sessionId)).then(() => toast("Đã đóng session.", "success")).catch((err) => toast(err.message, "error")); reload(); }}>End</button>} />
    </>
  );
}
