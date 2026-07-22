"use client";

import { FormEvent, useEffect, useState } from "react";
import { bookingApi, venueApi } from "@/lib/api/endpoints";
import { unwrapList } from "@/lib/api/client";
import { vietnamDatetimeLocalToUtcIso } from "@/lib/dateTime";
import { useToast } from "@/components/toast";
import type { TableType, VenueTable } from "@/types";

export default function PublicBookingPage() {
  const toast = useToast();
  const [tables, setTables] = useState<VenueTable[]>([]);
  const [types, setTypes] = useState<TableType[]>([]);
  const [form, setForm] = useState({ customerName: "", phoneNumber: "", email: "", tableId: "", tableTypeId: "", startTimeUtc: "", endTimeUtc: "", numberOfGuests: 2 });

  useEffect(() => {
    Promise.all([venueApi.tables(), venueApi.tableTypes()]).then(([tableData, typeData]) => {
      setTables(unwrapList(tableData));
      setTypes(unwrapList(typeData));
    }).catch(() => undefined);
  }, []);

  async function submit(event: FormEvent) {
    event.preventDefault();
    await bookingApi.create({
      customerName: form.customerName,
      phoneNumber: form.phoneNumber,
      email: form.email || undefined,
      tableId: form.tableId ? Number(form.tableId) : undefined,
      tableTypeId: form.tableTypeId ? Number(form.tableTypeId) : undefined,
      startTimeUtc: vietnamDatetimeLocalToUtcIso(form.startTimeUtc),
      endTimeUtc: vietnamDatetimeLocalToUtcIso(form.endTimeUtc),
      numberOfGuests: Number(form.numberOfGuests)
    }).then(() => toast("Đặt bàn thành công. Nhân viên sẽ xác nhận sớm.", "success"))
      .catch((err) => toast(err instanceof Error ? err.message : "Đặt bàn thất bại.", "error"));
  }

  return (
    <section className="section">
      <div className="page-header"><div><h1>Đặt bàn</h1><p>Gửi yêu cầu đặt bàn trực tiếp tới hệ thống PoolHub.</p></div></div>
      <form className="card form-grid" onSubmit={submit}>
        <label><span>Tên khách</span><input required value={form.customerName} onChange={(e) => setForm({ ...form, customerName: e.target.value })} /></label>
        <label><span>Số điện thoại</span><input required value={form.phoneNumber} onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })} /></label>
        <label><span>Email (để nhận xác nhận)</span><input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} placeholder="khach@example.com" /></label>
        <label><span>Loại bàn</span><select value={form.tableTypeId} onChange={(e) => setForm({ ...form, tableTypeId: e.target.value })}><option value="">Theo gợi ý</option>{types.map((item) => <option key={item.tableTypeId} value={item.tableTypeId}>{item.name}</option>)}</select></label>
        <label><span>Bàn cụ thể</span><select value={form.tableId} onChange={(e) => setForm({ ...form, tableId: e.target.value })}><option value="">Không chọn</option>{tables.map((item) => <option key={item.tableId} value={item.tableId}>{item.tableName}</option>)}</select></label>
        <label><span>Bắt đầu</span><input type="datetime-local" required value={form.startTimeUtc} onChange={(e) => setForm({ ...form, startTimeUtc: e.target.value })} /></label>
        <label><span>Kết thúc</span><input type="datetime-local" required value={form.endTimeUtc} onChange={(e) => setForm({ ...form, endTimeUtc: e.target.value })} /></label>
        <label><span>Số khách</span><input type="number" min={1} max={20} value={form.numberOfGuests} onChange={(e) => setForm({ ...form, numberOfGuests: Number(e.target.value) })} /></label>
        <button className="primary-btn">Gửi đặt bàn</button>
      </form>
    </section>
  );
}
