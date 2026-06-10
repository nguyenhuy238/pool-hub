"use client";

import { useState } from "react";
import { bookingApi } from "@/lib/api/endpoints";
import { dateTime, label, bookingStatus } from "@/lib/status";
import { Badge, DataTable, ListControls, PageHeader, SmartForm, StateBlock, useList, useLoad } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { Booking } from "@/types";

export default function BookingsPage() {
  const toast = useToast();
  const [status, setStatus] = useState("");
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const { data, loading, error, reload } = useLoad(() => bookingApi.list({ Status: status || undefined, Search: params.search, PageNumber: params.pageNumber, PageSize: params.pageSize }), [status, params]);
  const rows = useList<Booking>(data);

  async function action(fn: Promise<unknown>, message: string) {
    await fn.then(() => toast(message, "success")).catch((err) => toast(err.message, "error"));
    reload();
  }

  return (
    <>
      <PageHeader title="Booking Management" description="Xác nhận, hủy và tạo booking." action={<select value={status} onChange={(e) => setStatus(e.target.value)}><option value="">Tất cả trạng thái</option><option value="1">Pending</option><option value="2">Confirmed</option><option value="3">Cancelled</option></select>} />
      <ListControls search={params.search} pageNumber={params.pageNumber} pageSize={params.pageSize} onChange={setParams} />
      <SmartForm<Booking> title="Tạo booking nhanh" initial={{ numberOfGuests: 2 }} fields={[
        { name: "customerName", label: "Tên khách", required: true },
        { name: "phoneNumber", label: "Số điện thoại", required: true },
        { name: "tableId", label: "Table ID", type: "number" },
        { name: "tableTypeId", label: "Table Type ID", type: "number" },
        { name: "startTimeUtc", label: "Bắt đầu", type: "datetime-local", required: true },
        { name: "endTimeUtc", label: "Kết thúc", type: "datetime-local", required: true },
        { name: "numberOfGuests", label: "Số khách", type: "number", required: true }
      ]} onSubmit={async (value) => { await bookingApi.create({ ...value, startTimeUtc: new Date(String(value.startTimeUtc)).toISOString(), endTimeUtc: new Date(String(value.endTimeUtc)).toISOString() }); reload(); }} />
      <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
      <DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[
        { key: "bookingCode", label: "Mã" },
        { key: "tableId", label: "Bàn" },
        { key: "startTimeUtc", label: "Bắt đầu", render: (row) => dateTime(String(row.startTimeUtc)) },
        { key: "endTimeUtc", label: "Kết thúc", render: (row) => dateTime(String(row.endTimeUtc)) },
        { key: "status", label: "Trạng thái", render: (row) => <Badge tone={Number(row.status) === 3 ? "red" : Number(row.status) === 2 ? "green" : "yellow"}>{label(bookingStatus, Number(row.status))}</Badge> }
      ]} actions={(row) => <><button className="primary-btn" onClick={() => action(bookingApi.confirm(Number(row.bookingId)), "Đã xác nhận booking.")}>Confirm</button><button className="danger-btn" onClick={() => action(bookingApi.cancel(Number(row.bookingId)), "Đã hủy booking.")}>Cancel</button></>} />
    </>
  );
}
