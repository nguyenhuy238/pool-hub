"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { bookingApi, pricingApi } from "@/lib/api/endpoints";
import { getCurrentVietnamHourOfDay, getVietnamDateInputValue, getVietnamDayOfWeek, getVietnamHourOfDay, vietnamDateRangeToUtcIso, vietnamDateTimeToUtcIso } from "@/lib/dateTime";
import { useToast } from "@/components/toast";
import type { Booking, VenueTable, PricingPlan, PricingPlanRule } from "@/types";
import "./booking-modal.css";

// 07:00 to 24:00, in 30-minute boundaries.
const START_HOUR = 7;
const TOTAL_SLOTS = (24 - START_HOUR) * 2;

function generateTimeSlots() {
  const slots = [];
  for (let i = 0; i < TOTAL_SLOTS; i++) {
    const totalMins = START_HOUR * 60 + i * 30;
    const hours = Math.floor(totalMins / 60);
    const mins = totalMins % 60;
    const timeStr = `${hours.toString().padStart(2, '0')}:${mins.toString().padStart(2, '0')}`;
    slots.push(timeStr);
  }
  return slots;
}

const TIME_SLOTS = generateTimeSlots();

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
  
  // Selection can be 1 or 2 slots. If 1, it's the start. If 2, it's start and end.
  const [selectedSlotIndexes, setSelectedSlotIndexes] = useState<number[]>([]);

  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  
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
        const { startUtc, endUtc } = vietnamDateRangeToUtcIso(selectedDate);
        
        const res = await bookingApi.calendar(startUtc, endUtc, { tableId: selectedTableId });
        const items = Array.isArray(res) ? res : (res as any).items || [];
        
        const booked = new Set<number>();
        
        // Mark slots as booked
        items.forEach((b: any) => {
          if (b.status === 3) return; // Cancelled doesn't count
          
          const startHours = getVietnamHourOfDay(b.startTimeUtc);
          const endHours = getVietnamHourOfDay(b.endTimeUtc);
          
          for (let i = 0; i < TOTAL_SLOTS; i++) {
            const slotHour = START_HOUR + i * 0.5;
            // If the slot falls within the booking period (not strictly at the end edge)
            if (slotHour >= startHours && slotHour < endHours) {
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
  }, [selectedDate, selectedTableId, toast]);

  // Calculate past slots
  useEffect(() => {
    const calculatePast = () => {
      const todayStr = getVietnamDateInputValue();
      const past = new Set<number>();
      
      if (selectedDate < todayStr) {
        // All past
        for (let i = 0; i < TOTAL_SLOTS; i++) past.add(i);
      } else if (selectedDate === todayStr) {
        const currentHour = getCurrentVietnamHourOfDay();
        for (let i = 0; i < TOTAL_SLOTS; i++) {
          if (START_HOUR + i * 0.5 <= currentHour) {
            past.add(i);
          }
        }
      }
      setPastSlots(past);
    };
    
    calculatePast();
    const timer = setInterval(calculatePast, 60000); // Update every minute
    return () => clearInterval(timer);
  }, [selectedDate]);

  const handleSlotClick = (index: number) => {
    if (bookedSlots.has(index) || pastSlots.has(index)) return;
    
    if (selectedSlotIndexes.length === 0 || selectedSlotIndexes.length === 2) {
      setSelectedSlotIndexes([index]);
    } else if (selectedSlotIndexes.length === 1) {
      const start = selectedSlotIndexes[0];
      const end = index;
      
      if (end <= start) {
        setSelectedSlotIndexes([end]);
      } else {
        // Check if there are booked slots in between
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
      }
    }
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

  // Get price for a specific 30-minute slot
  const getSlotPrice = useCallback((index: number) => {
    if (!selectedTable) return 0;
    
    const activePlans = plans.filter(p => p.isActive);
    if (!activePlans.length) return 25000; // fallback 50k/hour = 25k/30m
    
    const plan = activePlans.find(p => p.isDefault) || activePlans[0];
    const dayOfWeek = getVietnamDayOfWeek(selectedDate);
    
    const startHour = START_HOUR + index * 0.5;
    const hoursStr = Math.floor(startHour).toString().padStart(2, '0');
    const minsStr = (startHour % 1 * 60).toString().padStart(2, '0');
    const timeStr = `${hoursStr}:${minsStr}:00`;
    
    const rule = rules.find(r => {
      if (!r.startTime || !r.endTime) return false;

      return (
        r.pricingPlanId === plan.pricingPlanId &&
        r.tableTypeId === selectedTable.tableTypeId &&
        r.dayOfWeek === dayOfWeek &&
        r.startTime <= timeStr &&
        r.endTime > timeStr // endTime should be strictly greater than timeStr to cover the block
      );
    });
    
    const rate = rule ? rule.hourlyRate : 50000;
    return rate * 0.5; // 30 mins = 0.5 hours
  }, [plans, rules, selectedDate, selectedTable]);

  // Calculate estimated price by summing all selected slots
  const estimatedPrice = useMemo(() => {
    if (selectedSlotIndexes.length !== 2 || !selectedTable) return 0;
    
    const s = selectedSlotIndexes[0];
    const e = selectedSlotIndexes[1] - 1;
    
    let total = 0;
    for (let i = s; i <= e; i++) {
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
    if (selectedSlotIndexes.length !== 2 || selectedSlotIndexes[1] <= selectedSlotIndexes[0]) {
      toast("Vui lòng chọn khung giờ trên lịch.", "error");
      return;
    }

    setSaving(true);
    try {
      const s = selectedSlotIndexes[0];
      const e = selectedSlotIndexes[1];
      
      const startHour = START_HOUR + s * 0.5;
      const endHour = START_HOUR + e * 0.5;
      const startTime = `${Math.floor(startHour).toString().padStart(2, "0")}:${((startHour % 1) * 60).toString().padStart(2, "0")}`;
      const endTime = `${Math.floor(endHour).toString().padStart(2, "0")}:${((endHour % 1) * 60).toString().padStart(2, "0")}`;

      // Create booking payload
      const payload: Partial<Booking> = {
        customerName,
        phoneNumber,
        email: email || undefined,
        numberOfGuests: Number(numberOfGuests) || 2,
        tableId: Number(selectedTableId),
        tableTypeId: selectedTable?.tableTypeId,
        startTimeUtc: vietnamDateTimeToUtcIso(selectedDate, startTime),
        endTimeUtc: vietnamDateTimeToUtcIso(selectedDate, endTime)
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
                  {TIME_SLOTS.map((timeStr, index) => {
                    const cls = getSlotClass(index);
                    const isDisabled = cls === "disabled" || cls === "booked";
                    const price = getSlotPrice(index);
                    return (
                      <button 
                        key={index} 
                        className={`time-slot-btn ${cls}`}
                        disabled={isDisabled}
                        onClick={() => handleSlotClick(index)}
                        style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '2px', padding: '6px 2px' }}
                      >
                        <span>{timeStr}</span>
                        {!isDisabled && price > 0 && (
                          <span style={{ fontSize: '11px', opacity: 0.8 }}>{(price / 1000)}k</span>
                        )}
                      </button>
                    );
                  })}
                </div>
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
            <button className="primary-btn" onClick={handleSubmit} disabled={saving || selectedSlotIndexes.length !== 2}>
              {saving ? "Đang xử lý..." : "Lưu Booking"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
