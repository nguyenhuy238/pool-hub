"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { bookingApi, pricingApi } from "@/lib/api/endpoints";
import { getVietnamDateInputValue, getVietnamDayOfWeek, utcTimestampMs, vietnamDateRangeToUtcIso } from "@/lib/dateTime";
import { calculateDurationMinutes, formatSlotDateTime, generateBookingSlots, slotToUtcIso, validateSlotRange } from "@/lib/timeSlots";
import { useToast } from "@/components/toast";
import { OvernightToggle } from "@/components/OvernightToggle";
import type { Booking, VenueTable, PricingPlan, PricingPlanRule } from "@/types";
import "./booking-modal.css";

export function BookingModal({ 
  onClose, 
  onSuccess, 
  tables 
}: { 
  onClose: () => void;
  onSuccess: () => void;
  tables: VenueTable[];
}) {
  const toast = useToast();
  
  const [customerName, setCustomerName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [email, setEmail] = useState("");
  const [numberOfGuests, setNumberOfGuests] = useState("2");
  const [selectedDate, setSelectedDate] = useState(() => getVietnamDateInputValue());
  const [selectedTableId, setSelectedTableId] = useState("");
  const [overnightEnabled, setOvernightEnabled] = useState(false);
  const timeSlots = useMemo(() => generateBookingSlots({ startDate: selectedDate, overnightEnabled }), [overnightEnabled, selectedDate]);
  
  // Selection can be 1 or 2 slots. If 1, it's the start. If 2, it's start and end.
  const [selectedSlotIndexes, setSelectedSlotIndexes] = useState<number[]>([]);

  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [checkingRange, setCheckingRange] = useState(false);
  const [rangeConflict, setRangeConflict] = useState(false);
  
  const [bookedSlots, setBookedSlots] = useState<Set<number>>(new Set());
  const [pastSlots, setPastSlots] = useState<Set<number>>(new Set());
  
  const [plans, setPlans] = useState<PricingPlan[]>([]);
  const [rules, setRules] = useState<PricingPlanRule[]>([]);

  // Fetch Pricing Plans and Rules once
  useEffect(() => {
    async function loadPricing() {
      try {
        const [pRes, rRes] = await Promise.all([
          pricingApi.plans(),
          pricingApi.rules({ pageSize: 500 })
        ]);
        setPlans(Array.isArray(pRes) ? pRes : (pRes as any).items || []);
        setRules(Array.isArray(rRes) ? rRes : (rRes as any).items || []);
      } catch (e) {
        console.error("Failed to load pricing", e);
      }
    }
    loadPricing();
  }, []);

  // Fetch Bookings and calculate past slots
  useEffect(() => {
    if (!selectedDate || !selectedTableId) {
      setBookedSlots(new Set());
      setSelectedSlotIndexes([]);
      return;
    }
    
    async function loadAvailability() {
      setLoading(true);
      try {
        const { startUtc, endUtc: sameDayEndUtc } = vietnamDateRangeToUtcIso(selectedDate);
        const endUtc = overnightEnabled ? slotToUtcIso(timeSlots[timeSlots.length - 1]) : sameDayEndUtc;
        
        const res = await bookingApi.calendar(startUtc, endUtc, { tableId: selectedTableId });
        const items = Array.isArray(res) ? res : (res as any).items || [];
        
        const booked = new Set<number>();
        
        // Mark slots as booked
        items.forEach((b: any) => {
          if (b.status === 3) return; // Cancelled doesn't count
          
          const bookingStart = utcTimestampMs(b.startTimeUtc);
          const bookingEnd = utcTimestampMs(b.endTimeUtc);
          for (let i = 0; i < timeSlots.length; i++) {
            const slotStart = new Date(slotToUtcIso(timeSlots[i])).getTime();
            const slotEnd = slotStart + 30 * 60 * 1000;
            if (slotStart < bookingEnd && bookingStart < slotEnd) {
              booked.add(i);
            }
          }
        });
        
        setBookedSlots(booked);
        setSelectedSlotIndexes([]); // Reset selection when table/date changes
      } catch (err: any) {
        toast("Không tải được lịch trống: " + err.message, "error");
      } finally {
        setLoading(false);
      }
    }
    
    loadAvailability();
  }, [overnightEnabled, selectedDate, selectedTableId, timeSlots, toast]);

  // Calculate past slots
  useEffect(() => {
    const calculatePast = () => {
      const past = new Set<number>();
      const now = Date.now();
      timeSlots.forEach((slot, index) => {
        if (new Date(slotToUtcIso(slot)).getTime() <= now) past.add(index);
      });
      setPastSlots(past);
    };
    
    calculatePast();
    const timer = setInterval(calculatePast, 60000); // Update every minute
    return () => clearInterval(timer);
  }, [selectedDate, timeSlots]);

  const handleSlotClick = (index: number) => {
    if (pastSlots.has(index) || (bookedSlots.has(index) && selectedSlotIndexes.length !== 1)) return;
    
    if (selectedSlotIndexes.length === 1) {
      const start = selectedSlotIndexes[0];
      if (index === start) {
        setSelectedSlotIndexes([]);
        return;
      }
      if (index < start) {
        setSelectedSlotIndexes([index]);
        return;
      }
      const end = index;
      let hasBooked = false;
      for (let i = start; i < end; i++) {
        if (bookedSlots.has(i)) hasBooked = true;
      }
      if (hasBooked) {
        toast("Khoảng thời gian chọn bị vướng lịch đã đặt. Vui lòng chọn lại.", "error");
        setSelectedSlotIndexes([index]);
      } else {
        setSelectedSlotIndexes([start, end]);
      }
      return;
    }

    if (selectedSlotIndexes.length === 2 && (index === selectedSlotIndexes[0] || index === selectedSlotIndexes[1])) {
      setSelectedSlotIndexes([]);
      return;
    }

    setSelectedSlotIndexes([index]);
  };

  const getSlotClass = (index: number) => {
    if (pastSlots.has(index)) return "disabled";
    if (bookedSlots.has(index)) return "booked";
    
    if (selectedSlotIndexes.length === 1 && selectedSlotIndexes[0] === index) return "selected";
    if (selectedSlotIndexes.length === 2) {
      const [s, e] = selectedSlotIndexes;
      if (index === s || index === e) return "selected";
      if (index > s && index < e) return "in-range";
    }
    
    return "";
  };

  const selectedTable = useMemo(() => tables.find(t => t.tableId === Number(selectedTableId)), [tables, selectedTableId]);
  const selectedStartSlot = timeSlots[selectedSlotIndexes[0]];
  const selectedEndSlot = timeSlots[selectedSlotIndexes[1]];
  const durationMinutes = calculateDurationMinutes(selectedStartSlot, selectedEndSlot);
  const nightRules = useMemo(() => rules.filter((rule) => {
    if (selectedTable && rule.tableTypeId !== selectedTable.tableTypeId) return false;
    const planName = plans.find((plan) => plan.pricingPlanId === rule.pricingPlanId)?.name.toLowerCase() || "";
    const start = rule.startTime?.slice(0, 5) || "";
    const end = rule.endTime?.slice(0, 5) || "";
    return planName.includes("đêm") || planName.includes("night") || planName.includes("overnight") ||
      Boolean(start && end && (start > end || start >= "22:00" || end <= "06:00"));
  }), [plans, rules, selectedTable]);

  useEffect(() => {
    if (!selectedTableId || !selectedStartSlot || !selectedEndSlot) {
      setRangeConflict(false);
      return;
    }
    let cancelled = false;
    setCheckingRange(true);
    bookingApi.availability(Number(selectedTableId), slotToUtcIso(selectedStartSlot), slotToUtcIso(selectedEndSlot))
      .then((available) => { if (!cancelled) setRangeConflict(!available.some((table) => Number(table.tableId) === Number(selectedTableId))); })
      .catch(() => { if (!cancelled) setRangeConflict(true); })
      .finally(() => { if (!cancelled) setCheckingRange(false); });
    return () => { cancelled = true; };
  }, [selectedEndSlot, selectedStartSlot, selectedTableId]);

  // Get price for a specific 30-minute slot
  const getSlotPrice = useCallback((index: number) => {
    if (!selectedTable) return 0;
    
    const activePlans = plans.filter(p => p.isActive);
    if (!activePlans.length) return 25000; // fallback 50k/hour = 25k/30m
    
    const plan = activePlans.find(p => p.isDefault) || activePlans[0];
    const slot = timeSlots[index];
    if (!slot) return 0;
    const dayOfWeek = getVietnamDayOfWeek(slot.localDate);
    const timeStr = `${slot.time}:00`;
    
    const rule = rules.find(r => {
      if (!r.startTime || !r.endTime) return false;
      const matchesTime = r.startTime <= r.endTime
        ? r.startTime <= timeStr && r.endTime > timeStr
        : r.startTime <= timeStr || r.endTime > timeStr;

      return (
        r.pricingPlanId === plan.pricingPlanId &&
        r.tableTypeId === selectedTable.tableTypeId &&
        r.dayOfWeek === dayOfWeek &&
        matchesTime
      );
    });
    
    const rate = rule ? rule.hourlyRate : 50000;
    return rate * 0.5; // 30 mins = 0.5 hours
  }, [plans, rules, selectedTable, timeSlots]);

  // Calculate estimated price by summing all selected slots
  const estimatedPrice = useMemo(() => {
    if (selectedSlotIndexes.length !== 2 || !selectedTable) return 0;
    
    const s = selectedSlotIndexes[0];
    const e = selectedSlotIndexes[1];
    
    let total = 0;
    for (let i = s; i < e; i++) {
      total += getSlotPrice(i);
    }
    
    return total;
  }, [selectedSlotIndexes, selectedTable, getSlotPrice]);

  const handleSubmit = async () => {
    if (!customerName || !phoneNumber) {
      toast("Vui lòng nhập tên và số điện thoại khách hàng.", "error");
      return;
    }
    if (!selectedTableId) {
      toast("Vui lòng chọn bàn.", "error");
      return;
    }
    const startSlot = timeSlots[selectedSlotIndexes[0]];
    const endSlot = timeSlots[selectedSlotIndexes[1]];
    const validation = validateSlotRange(startSlot, endSlot, overnightEnabled);
    if (!validation.valid) {
      toast(validation.message, "error");
      return;
    }

    setSaving(true);
    try {
      const startTimeUtc = slotToUtcIso(startSlot);
      const endTimeUtc = slotToUtcIso(endSlot);
      const available = await bookingApi.availability(Number(selectedTableId), startTimeUtc, endTimeUtc);
      if (!available.some((table) => Number(table.tableId) === Number(selectedTableId))) {
        toast("Bàn đã có booking hoặc phiên chơi trong khung giờ này.", "error");
        return;
      }

      // Create booking payload
      const payload: Partial<Booking> = {
        customerName,
        phoneNumber,
        email: email || undefined,
        numberOfGuests: Number(numberOfGuests) || 2,
        tableId: Number(selectedTableId),
        tableTypeId: selectedTable?.tableTypeId,
        startTimeUtc,
        endTimeUtc
      };

      await bookingApi.create(payload);
      toast("Tạo đặt bàn thành công!", "success");
      onSuccess();
    } catch (err: any) {
      toast(err.message || "Tạo đặt bàn thất bại.", "error");
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="booking-modal-overlay" onClick={onClose}>
      <div className="booking-modal-content" onClick={e => e.stopPropagation()}>
        <div className="booking-modal-header">
          <h2>Tạo lịch đặt bàn</h2>
          <button className="btn-close" onClick={onClose}>&times;</button>
        </div>
        
        <div className="booking-modal-body">
          <div className="booking-modal-col-left">
            <label>
              <span>Tên khách hàng *</span>
              <input type="text" value={customerName} onChange={e => setCustomerName(e.target.value)} placeholder="Nhập tên khách" />
            </label>
            <label>
              <span>Số điện thoại *</span>
              <input type="text" value={phoneNumber} onChange={e => setPhoneNumber(e.target.value)} placeholder="Nhập SĐT" />
            </label>
            <label>
              <span>Email (nhận thông báo)</span>
              <input type="email" value={email} onChange={e => setEmail(e.target.value)} placeholder="email@example.com" />
            </label>
            <label>
              <span>Số lượng khách</span>
              <input type="number" value={numberOfGuests} onChange={e => setNumberOfGuests(e.target.value)} min="1" />
            </label>
            <hr style={{borderTop: '1px solid var(--line)', borderBottom: 'none', margin: '8px 0'}} />
            <label>
              <span>Chọn Ngày *</span>
              <input type="date" value={selectedDate} onChange={e => setSelectedDate(e.target.value)} />
            </label>
            <OvernightToggle checked={overnightEnabled} onChange={(checked) => { setOvernightEnabled(checked); setSelectedSlotIndexes((current) => current.length ? [current[0]] : []); }} />
            <label>
              <span>Chọn Bàn cụ thể *</span>
              <select value={selectedTableId} onChange={e => setSelectedTableId(e.target.value)}>
                <option value="">-- Chọn bàn --</option>
                {tables.map(t => <option key={t.tableId} value={t.tableId}>{t.tableName} ({t.tableCode})</option>)}
              </select>
              <span style={{fontSize: '12px', color: 'var(--muted)'}}>Phải chọn bàn để xem lịch trống.</span>
            </label>
          </div>
          
          <div className="booking-modal-col-right">
            <div className="legend">
              <div className="legend-item"><div className="legend-box" style={{background: 'white', border: '1px solid var(--line)'}}></div> Trống</div>
              <div className="legend-item"><div className="legend-box" style={{background: '#123b63'}}></div> Đang chọn</div>
              <div className="legend-item"><div className="legend-box" style={{background: '#ffe3e3', border: '1px solid #ffc9c9'}}></div> Đã đặt</div>
              <div className="legend-item"><div className="legend-box" style={{background: '#f1f3f5'}}></div> Đã qua</div>
            </div>
            
            {loading ? (
              <div style={{padding: '40px', textAlign: 'center', color: 'var(--muted)'}}>Đang tải lịch trống...</div>
            ) : !selectedTableId ? (
              <div style={{padding: '40px', textAlign: 'center', color: 'var(--muted)'}}>Vui lòng chọn Bàn ở cột trái để xem lịch.</div>
            ) : (
              <>
                <p style={{fontSize: '14px', margin: 0, color: 'var(--ink)'}}>Nhấp vào 1 ô để chọn giờ bắt đầu, nhấp ô tiếp theo để chọn giờ kết thúc.</p>
                <div className="time-slots-grid">
                  {timeSlots.map((slot, index) => {
                    const cls = getSlotClass(index);
                    const isDisabled = cls === "disabled" || (cls === "booked" && selectedSlotIndexes.length !== 1);
                    const price = getSlotPrice(index);
                    return (
                      <button 
                        key={index} 
                        className={`time-slot-btn ${cls}`}
                        disabled={isDisabled}
                        onClick={() => handleSlotClick(index)}
                        style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '2px', padding: '6px 2px' }}
                      >
                        <span>{slot.displayLabel}</span>
                        {!isDisabled && price > 0 && (
                          <span style={{ fontSize: '11px', opacity: 0.8 }}>{(price / 1000)}k</span>
                        )}
                      </button>
                    );
                  })}
                </div>
                {overnightEnabled ? <p style={{ margin: "8px 0 0", color: "var(--muted)", fontSize: 13 }}>(+1) nghĩa là ngày hôm sau.</p> : null}
                {selectedEndSlot?.dayOffset === 1 ? <div className="inline-note">Bạn đang chọn ca qua đêm.<br />Thời gian dự kiến: {formatSlotDateTime(selectedStartSlot)} → {formatSlotDateTime(selectedEndSlot)}.<br />Thời lượng: {(durationMinutes / 60).toLocaleString("vi-VN")} giờ.</div> : null}
                {overnightEnabled ? <div className="inline-note"><strong>Gói đêm áp dụng</strong><br />{nightRules.length ? nightRules.map((rule) => `${plans.find((plan) => plan.pricingPlanId === rule.pricingPlanId)?.name || "Bảng giá"}: ${rule.startTime?.slice(0, 5)}–${rule.endTime?.slice(0, 5)} (${rule.hourlyRate.toLocaleString("vi-VN")} đ/giờ)`).join("; ") : "Chưa có gói đêm được cấu hình. Giá tạm tính theo bảng giá hiện tại."}</div> : null}
                {checkingRange ? <div className="inline-note">Đang kiểm tra lịch trống...</div> : null}
                {rangeConflict ? <div className="inline-alert error">Bàn đã có booking hoặc phiên chơi trong khung giờ này.</div> : null}
              </>
            )}
          </div>
        </div>
        
        <div className="booking-modal-footer">
          <div className="booking-modal-price">
            <span className="label">Tạm tính (Dự kiến):</span>
            <span className="value">{estimatedPrice.toLocaleString('vi-VN')} đ</span>
          </div>
          <div className="booking-modal-actions">
            <button className="ghost-btn" onClick={onClose} disabled={saving}>Hủy</button>
            <button className="primary-btn" onClick={handleSubmit} disabled={saving || checkingRange || rangeConflict || selectedSlotIndexes.length !== 2}>
              {saving ? "Đang xử lý..." : "Lưu Booking"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
