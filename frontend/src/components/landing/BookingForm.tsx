"use client";

import { FormEvent, useEffect, useState } from "react";
import { availabilityApi, type LandingAvailability } from "@/lib/api/availabilityApi";
import { publicBookingApi } from "@/lib/api/publicBookingApi";
import type { BookingPolicySettings } from "@/lib/api/landingSettingsApi";
import { useToast } from "@/components/toast";

const initialForm = {
  customerName: "",
  phoneNumber: "",
  email: "",
  bookingDate: new Date().toISOString().slice(0, 10),
  startTime: "",
  durationHours: 2,
  tableTypeId: "",
  tableId: "",
  numberOfGuests: 4,
  note: ""
};

export function BookingForm({ policy }: { policy: BookingPolicySettings }) {
  const toast = useToast();
  const [availability, setAvailability] = useState<LandingAvailability | null>(null);
  const [form, setForm] = useState({ ...initialForm, durationHours: Math.max(1, Math.round(policy.defaultDurationMinutes / 60)) });
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    availabilityApi.getAvailability().then(setAvailability).catch(() => undefined);
  }, []);

  const tables = availability?.layout.floors.flatMap((floor) => floor.zones.flatMap((zone) => zone.tables)) || [];

  function validate() {
    if (!policy.allowOnlineBooking) return "Hiện tại PoolHub chưa mở đặt bàn online.";
    if (!form.customerName.trim()) return "Vui lòng nhập họ tên khách.";
    if (!form.phoneNumber.trim()) return "Vui lòng nhập số điện thoại.";
    if (!form.bookingDate || !form.startTime) return "Vui lòng chọn ngày và giờ đặt.";
    if (!form.tableTypeId) return "Vui lòng chọn loại bàn.";

    const start = new Date(`${form.bookingDate}T${form.startTime}`);
    if (Number.isNaN(start.getTime()) || start.getTime() < Date.now()) return "Không thể chọn ngày/giờ trong quá khứ.";

    return null;
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const error = validate();
    if (error) {
      toast(error, "error");
      return;
    }

    setSaving(true);
    try {
      await publicBookingApi.create({
        customerName: form.customerName.trim(),
        phoneNumber: form.phoneNumber.trim(),
        email: form.email.trim() || undefined,
        bookingDate: form.bookingDate,
        startTime: form.startTime,
        durationHours: Number(form.durationHours),
        tableTypeId: Number(form.tableTypeId),
        tableId: form.tableId ? Number(form.tableId) : undefined,
        numberOfGuests: Number(form.numberOfGuests),
        note: form.note.trim() || undefined
      });
      toast(policy.successMessage, "success");
      setForm({ ...initialForm, durationHours: Math.max(1, Math.round(policy.defaultDurationMinutes / 60)) });
    } catch (err) {
      toast(err instanceof Error ? err.message : "Gửi yêu cầu đặt bàn thất bại.", "error");
    } finally {
      setSaving(false);
    }
  }

  return (
    <section className="landing-section booking-section" id="booking">
      <div className="booking-copy">
        <p className="eyebrow">Đặt bàn online</p>
        <h2>Gửi yêu cầu, nhân viên sẽ xác nhận lịch trong thời gian sớm nhất</h2>
        <p>{policy.policyNote}</p>
      </div>
      <form className="booking-form" onSubmit={submit}>
        <label><span>Họ tên khách *</span><input value={form.customerName} onChange={(e) => setForm({ ...form, customerName: e.target.value })} /></label>
        <label><span>Số điện thoại *</span><input inputMode="tel" value={form.phoneNumber} onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })} /></label>
        <label><span>Email</span><input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} /></label>
        <label><span>Ngày đặt *</span><input type="date" min={new Date().toISOString().slice(0, 10)} max={new Date(Date.now() + policy.advanceBookingDays * 24 * 60 * 60 * 1000).toISOString().slice(0, 10)} value={form.bookingDate} onChange={(e) => setForm({ ...form, bookingDate: e.target.value })} /></label>
        <label><span>Giờ bắt đầu *</span><input type="time" value={form.startTime} onChange={(e) => setForm({ ...form, startTime: e.target.value })} /></label>
        <label><span>Thời lượng</span><select value={form.durationHours} onChange={(e) => setForm({ ...form, durationHours: Number(e.target.value) })}>
          {[1, 2, 3, 4, 5, 6].filter((hour) => hour * 60 >= policy.minDurationMinutes && hour * 60 <= policy.maxDurationMinutes).map((hour) => <option key={hour} value={hour}>{hour} giờ</option>)}
        </select></label>
        <label><span>Loại bàn *</span><select value={form.tableTypeId} onChange={(e) => setForm({ ...form, tableTypeId: e.target.value })}><option value="">Chọn loại bàn</option>{availability?.tableTypes.map((item) => <option key={item.tableTypeId} value={item.tableTypeId}>{item.name}</option>)}</select></label>
        <label><span>Bàn cụ thể</span><select value={form.tableId} onChange={(e) => setForm({ ...form, tableId: e.target.value })}><option value="">Để nhân viên gợi ý</option>{tables.map((item) => <option key={item.tableId} value={item.tableId}>{item.tableName}</option>)}</select></label>
        <label><span>Số người</span><input type="number" min={1} max={20} value={form.numberOfGuests} onChange={(e) => setForm({ ...form, numberOfGuests: Number(e.target.value) })} /></label>
        <label className="full-field"><span>Ghi chú</span><textarea rows={4} value={form.note} onChange={(e) => setForm({ ...form, note: e.target.value })} /></label>
        <button className="primary-btn full-field" disabled={saving || !policy.allowOnlineBooking}>{saving ? "Đang gửi..." : "Gửi yêu cầu đặt bàn"}</button>
      </form>
    </section>
  );
}
