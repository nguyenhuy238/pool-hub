import React, { useState, useEffect } from 'react';
import './BookingWizard.css';
import { availabilityApi, type LandingAvailability, type LandingPricing } from "@/lib/api/availabilityApi";
import { publicBookingApi } from "@/lib/api/publicBookingApi";
import { addDaysToVietnamDateInput, getCurrentVietnamHourOfDay, getVietnamDateInputValue, getVietnamDayOfWeek, getVietnamHourOfDay, vietnamDateTimeToUtcIso } from "@/lib/dateTime";
import type { BookingPolicySettings } from "@/lib/api/landingSettingsApi";
import { useToast } from "@/components/toast";
import type { VenueFloorLayoutItem, VenueZoneLayoutItem, VenueTableLayoutItem, PricingPlan, PricingPlanRule } from '@/types';
const START_HOUR = 7;
const TOTAL_SLOTS = (24 - START_HOUR) * 2;
const TIME_SLOTS = Array.from({ length: TOTAL_SLOTS }).map((_, i) => {
  const totalMins = START_HOUR * 60 + i * 30;
  return `${Math.floor(totalMins / 60).toString().padStart(2, '0')}:${(totalMins % 60).toString().padStart(2, '0')}`;
});

function getTableTypeColors(name: string) {
  const n = name.toLowerCase();
  if (n.includes('vip')) return { color: '#8b5cf6', background: '#ede9fe' };
  if (n.includes('standard')) return { color: '#10b981', background: '#d1fae5' };
  if (n.includes('carom')) return { color: '#f59e0b', background: '#fef3c7' };
  if (n.includes('snooker')) return { color: '#ef4444', background: '#fee2e2' };
  return { color: '#6b7280', background: '#f3f4f6' };
}

export function BookingWizard({ policy }: { policy: BookingPolicySettings }) {
  const toast = useToast();
  
  // Data State
  const [availability, setAvailability] = useState<LandingAvailability | null>(null);
  const [pricing, setPricing] = useState<LandingPricing | null>(null);
  const [loadingData, setLoadingData] = useState(true);

  // Wizard State
  const [step, setStep] = useState(1);
  const [selectedFloorId, setSelectedFloorId] = useState<number | null>(null);
  
  // Form State
  const [selectedTable, setSelectedTable] = useState<VenueTableLayoutItem | null>(null);
  
  const [bookingDate, setBookingDate] = useState(getVietnamDateInputValue());
  const [selectedSlotIndexes, setSelectedSlotIndexes] = useState<number[]>([]);
  const [bookedSlots, setBookedSlots] = useState<Set<number>>(new Set());
  const [pastSlots, setPastSlots] = useState<Set<number>>(new Set());
  const [loadingBookings, setLoadingBookings] = useState(false);
  
  const [customerInfo, setCustomerInfo] = useState({
    customerName: '',
    phoneNumber: '',
    email: '',
    numberOfGuests: 4,
    note: ''
  });

  const [saving, setSaving] = useState(false);

  useEffect(() => {
    Promise.all([
      availabilityApi.getAvailability(),
      availabilityApi.getPricing()
    ]).then(([availData, priceData]) => {
      setAvailability(availData);
      setPricing(priceData);
      if (availData.layout?.floors?.length > 0) {
        setSelectedFloorId(availData.layout.floors[0].floorId);
      }
    }).catch(err => {
      console.error(err);
      toast("Không thể tải dữ liệu sơ đồ bàn.", "error");
    }).finally(() => {
      setLoadingData(false);
    });
  }, [toast]);

  useEffect(() => {
    if (selectedTable && bookingDate) {
      fetchExistingBookings();
    }
    // fetchExistingBookings intentionally follows the selected table/date lifecycle.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedTable, bookingDate]);

  const fetchExistingBookings = async () => {
    if (!selectedTable) return;
    setLoadingBookings(true);
    try {
      const res = await publicBookingApi.getPublicCalendar(selectedTable.tableId, bookingDate);
      const dataArray = Array.isArray(res) ? res : ((res as any).data || []);
      
      const booked = new Set<number>();
      dataArray.forEach((b: any) => {
        if (b.status === 3) return;
        const start = new Date(b.startTimeUtc);
        const end = new Date(b.endTimeUtc);
        const targetStart = new Date(vietnamDateTimeToUtcIso(bookingDate, "00:00"));
        const targetEnd = new Date(vietnamDateTimeToUtcIso(bookingDate, "23:59:59"));

        let startHours = getVietnamHourOfDay(b.startTimeUtc);
        let endHours = getVietnamHourOfDay(b.endTimeUtc);

        if (start < targetStart) startHours = 0;
        if (end > targetEnd || end.getTime() === targetEnd.getTime()) endHours = 24;
        else if (endHours === 0 && end.getMinutes() === 0 && end > start) endHours = 24;

        for (let i = 0; i < TOTAL_SLOTS; i++) {
          const slotHour = START_HOUR + i * 0.5;
          if (slotHour >= startHours && slotHour < endHours) {
            booked.add(i);
          }
        }
      });
      setBookedSlots(booked);
      setSelectedSlotIndexes([]);
    } catch (err) {
      console.error(err);
      setBookedSlots(new Set());
    } finally {
      setLoadingBookings(false);
    }
  };

  useEffect(() => {
    const calculatePast = () => {
      const todayStr = getVietnamDateInputValue();
      const past = new Set<number>();
      
      if (bookingDate < todayStr) {
        for (let i = 0; i < TOTAL_SLOTS; i++) past.add(i);
      } else if (bookingDate === todayStr) {
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
    const timer = setInterval(calculatePast, 60000);
    return () => clearInterval(timer);
  }, [bookingDate]);

  const handleNextStep1 = () => {
    if (!selectedTable) {
      toast("Vui lòng chọn bàn.", "error");
      return;
    }
    setStep(2);
  };

  const handleNextStep2 = () => {
    if (selectedSlotIndexes.length !== 2 || selectedSlotIndexes[1] <= selectedSlotIndexes[0]) {
      toast("Vui lòng chọn khung giờ.", "error");
      return;
    }
    setStep(3);
  };


  const handleNextStep3 = () => {
    if (!customerInfo.customerName.trim()) {
      toast("Vui lòng nhập họ tên khách.", "error");
      return;
    }

    const phoneRegex = /^(0|\+84)[3|5|7|8|9][0-9]{8}$/;
    if (!customerInfo.phoneNumber.trim()) {
      toast("Vui lòng nhập số điện thoại.", "error");
      return;
    } else if (!phoneRegex.test(customerInfo.phoneNumber.trim())) {
      toast("Số điện thoại không hợp lệ (Ví dụ: 0987654321).", "error");
      return;
    }

    if (customerInfo.email.trim()) {
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(customerInfo.email.trim())) {
        toast("Địa chỉ email không hợp lệ.", "error");
        return;
      }
    }

    if (!customerInfo.numberOfGuests || customerInfo.numberOfGuests < 1) {
      toast("Số lượng người phải lớn hơn 0.", "error");
      return;
    }

    setStep(4);
  };

  const handleSubmit = async () => {
    setSaving(true);
    try {
      const { startTime, durationHours } = getCalculatedTimeAndDuration();
      await publicBookingApi.create({
        customerName: customerInfo.customerName.trim(),
        phoneNumber: customerInfo.phoneNumber.trim(),
        email: customerInfo.email.trim() || undefined,
        bookingDate,
        startTime,
        durationHours: Number(durationHours),
        tableTypeId: Number(selectedTable!.tableTypeId),
        tableId: selectedTable!.tableId,
        numberOfGuests: Number(customerInfo.numberOfGuests),
        note: customerInfo.note.trim() || undefined
      });
      toast(policy.successMessage, "success");
      setStep(5); // Success step
    } catch (err) {
      toast(err instanceof Error ? err.message : "Gửi yêu cầu đặt bàn thất bại.", "error");
    } finally {
      setSaving(false);
    }
  };

  const renderStep1 = () => {
    if (!availability) return null;
    const floors = availability.layout.floors || [];
    const selectedFloor = floors.find(f => f.floorId === selectedFloorId);

    return (
      <div className="bw-step bw-step-1">
        <h3>1. Chọn Khu vực & Bàn</h3>
        
        <div className="bw-floor-tabs">
          {floors.map(floor => (
            <button 
              key={floor.floorId} 
              className={`bw-floor-tab ${selectedFloorId === floor.floorId ? 'active' : ''}`}
              onClick={() => setSelectedFloorId(floor.floorId)}
            >
              {floor.floorName}
            </button>
          ))}
        </div>

        {selectedFloor && (
          <div className="bw-zones-container">
            {selectedFloor.zones.map(zone => (
              <div key={zone.zoneId} className="bw-zone">
                <h4 className="bw-zone-title">{zone.zoneName}</h4>
                <div className="bw-tables-grid">
                  {zone.tables.map(table => (
                    <button 
                      key={table.tableId}
                      className={`bw-table-card available ${selectedTable?.tableId === table.tableId ? 'selected' : ''}`}
                      onClick={() => setSelectedTable(table)}
                    >
                      <span className="bw-table-name">{table.tableName}</span>
                      <span className="bw-table-type" style={getTableTypeColors(table.tableTypeName)}>{table.tableTypeName}</span>
                    </button>
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}
        
        <div className="bw-actions">
          <button className="primary-btn" onClick={handleNextStep1} disabled={!selectedTable}>Tiếp tục</button>
        </div>
      </div>
    );
  };

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

  const getSlotPrice = (index: number) => {
    if (!selectedTable || !pricing || !pricing.plans || !pricing.rules) return 0;
    
    const activePlans = pricing.plans.filter(p => p.isActive);
    if (!activePlans.length) return 25000;
    const plan = activePlans.find(p => p.isDefault) || activePlans[0];
    
    const rulesForTableType = pricing.rules.filter(r => 
      r.pricingPlanId === plan.pricingPlanId && 
      r.tableTypeId === selectedTable.tableTypeId && 
      r.isActive !== false
    );
    
    if (rulesForTableType.length === 0) return 0;
    
    const dayOfWeek = getVietnamDayOfWeek(bookingDate);
    const timeStr = TIME_SLOTS[index] + ":00";
    
    let rule = rulesForTableType.find(r => {
      if (!r.startTime || !r.endTime) return false;
      const ruleDay = r.dayOfWeek !== undefined ? r.dayOfWeek : -1;
      return (ruleDay === dayOfWeek || ruleDay === -1) && r.startTime <= timeStr && r.endTime > timeStr;
    });
    
    if (!rule) {
      rule = rulesForTableType.find(r => {
        const ruleDay = r.dayOfWeek !== undefined ? r.dayOfWeek : -1;
        return ruleDay === dayOfWeek || ruleDay === -1;
      }) || rulesForTableType[0];
    }
    
    const rate = rule ? (rule.hourlyRate || 0) : 0;
    return rate * 0.5;
  };

  const estimatedPrice = React.useMemo(() => {
    if (selectedSlotIndexes.length !== 2 || !selectedTable) return 0;
    const s = selectedSlotIndexes[0];
    const e = selectedSlotIndexes[1] - 1;
    let total = 0;
    for (let i = s; i <= e; i++) total += getSlotPrice(i);
    return total;
  }, [selectedSlotIndexes, selectedTable, pricing, bookingDate]);

  const getCalculatedTimeAndDuration = () => {
    if (selectedSlotIndexes.length !== 2) return { startTime: "00:00", durationHours: 0 };
    const s = selectedSlotIndexes[0];
    const e = selectedSlotIndexes[1];
    const startHourNum = START_HOUR + s * 0.5;
    const endHourNum = START_HOUR + e * 0.5;
    const durationHours = endHourNum - startHourNum;
    return { startTime: TIME_SLOTS[s], durationHours };
  };

  const renderStep2 = () => {
    return (
      <div className="bw-step bw-step-2">
        <h3>2. Chọn Thời gian & Xem giá</h3>
        <p className="bw-subtitle">Bàn đã chọn: <strong>{selectedTable?.tableName}</strong> ({selectedTable?.tableTypeName})</p>
        
        <div className="bw-form-grid" style={{ marginBottom: '16px' }}>
          <label><span>Ngày đặt *</span>
            <input type="date" min={getVietnamDateInputValue()} 
                   max={addDaysToVietnamDateInput(getVietnamDateInputValue(), policy.advanceBookingDays)} 
                   value={bookingDate} onChange={e => {
                     setBookingDate(e.target.value);
                     setSelectedSlotIndexes([]);
                   }} />
          </label>
        </div>

        <div className="bw-timeline-container" style={{ marginBottom: '32px' }}>
          <div className="legend" style={{ display: 'flex', gap: '12px', fontSize: '13px', marginBottom: '12px', flexWrap: 'wrap' }}>
            <div style={{display: 'flex', alignItems: 'center', gap: '4px'}}><div style={{width: '16px', height: '16px', border: '1px solid #e5e7eb', background: 'white'}}></div> Trống</div>
            <div style={{display: 'flex', alignItems: 'center', gap: '4px'}}><div style={{width: '16px', height: '16px', background: '#eff6ff', border: '1px solid #3b82f6'}}></div> Đang chọn</div>
            <div style={{display: 'flex', alignItems: 'center', gap: '4px'}}><div style={{width: '16px', height: '16px', background: '#fee2e2'}}></div> Đã đặt</div>
            <div style={{display: 'flex', alignItems: 'center', gap: '4px'}}><div style={{width: '16px', height: '16px', background: '#f3f4f6'}}></div> Đã qua</div>
          </div>
          <p style={{ fontSize: '0.875rem', fontWeight: 500, color: '#374151', marginBottom: '8px' }}>
            Nhấp vào 1 ô để chọn giờ bắt đầu, nhấp ô tiếp theo để chọn giờ kết thúc.
          </p>
          {loadingBookings ? (
            <div style={{ padding: '20px', textAlign: 'center', color: '#6b7280' }}>Đang tải lịch đặt...</div>
          ) : (
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(65px, 1fr))', gap: '6px' }}>
              {TIME_SLOTS.map((timeStr, index) => {
                const cls = getSlotClass(index);
                const isDisabled = cls === "disabled" || cls === "booked";
                const price = getSlotPrice(index);
                return (
                  <button 
                    key={index} 
                    className={`bw-time-slot-btn ${cls}`}
                    disabled={isDisabled}
                    onClick={() => handleSlotClick(index)}
                    style={{ 
                      display: 'flex', flexDirection: 'column', alignItems: 'center', padding: '6px 2px',
                      border: '1px solid #e5e7eb', borderRadius: '4px', cursor: isDisabled ? 'not-allowed' : 'pointer',
                      background: cls === 'disabled' ? '#f3f4f6' : cls === 'booked' ? '#fee2e2' : cls === 'selected' || cls === 'in-range' ? '#eff6ff' : 'white',
                      borderColor: cls === 'selected' || cls === 'in-range' ? '#3b82f6' : cls === 'booked' ? '#fca5a5' : '#e5e7eb',
                      color: cls === 'disabled' ? '#9ca3af' : cls === 'booked' ? '#b91c1c' : '#111827'
                    }}
                  >
                    <span style={{ fontSize: '13px', fontWeight: cls.includes('selected') ? 600 : 400 }}>{timeStr}</span>
                    {!isDisabled && price > 0 && (
                      <span style={{ fontSize: '11px', color: cls.includes('selected') ? '#2563eb' : '#6b7280' }}>{(price / 1000)}k</span>
                    )}
                  </button>
                );
              })}
            </div>
          )}
        </div>

        <div className="bw-pricing-box">
          <h4>Tổng tiền tạm tính</h4>
          <div className="bw-price-amount">{estimatedPrice.toLocaleString('vi-VN')} đ</div>
          <p className="bw-price-note">* Giá ước tính dựa trên bảng giá. Giá thực tế tính theo thời gian sử dụng khi kết thúc.</p>
        </div>

        <div className="bw-actions">
          <button className="outline-btn" onClick={() => setStep(1)}>Quay lại</button>
          <button className="primary-btn" onClick={handleNextStep2}>Tiếp tục</button>
        </div>
      </div>
    );
  };

  const renderStep3 = () => (
    <div className="bw-step bw-step-3">
      <h3>3. Thông tin Khách hàng</h3>
      
      <div className="bw-form-grid">
        <label><span>Họ tên khách *</span>
          <input value={customerInfo.customerName} onChange={e => setCustomerInfo({...customerInfo, customerName: e.target.value})} />
        </label>
        <label><span>Số điện thoại *</span>
          <input inputMode="tel" value={customerInfo.phoneNumber} onChange={e => setCustomerInfo({...customerInfo, phoneNumber: e.target.value})} />
        </label>
        <label><span>Email</span>
          <input type="email" value={customerInfo.email} onChange={e => setCustomerInfo({...customerInfo, email: e.target.value})} />
        </label>
        <label><span>Số người</span>
          <input type="number" min={1} max={20} value={customerInfo.numberOfGuests} onChange={e => setCustomerInfo({...customerInfo, numberOfGuests: Number(e.target.value)})} />
        </label>
        <label className="bw-full-field"><span>Ghi chú</span>
          <textarea rows={3} value={customerInfo.note} onChange={e => setCustomerInfo({...customerInfo, note: e.target.value})} />
        </label>
      </div>

      <div className="bw-actions">
        <button className="outline-btn" onClick={() => setStep(2)}>Quay lại</button>
        <button className="primary-btn" onClick={handleNextStep3}>Xem lại & Xác nhận</button>
      </div>
    </div>
  );

  const renderStep4 = () => {
    const { startTime, durationHours } = getCalculatedTimeAndDuration();
    return (
      <div className="bw-step bw-step-4">
        <h3>4. Xác nhận Đặt bàn</h3>
        
        <div className="bw-summary">
          <div className="bw-summary-row"><span>Khách hàng:</span> <strong>{customerInfo.customerName} - {customerInfo.phoneNumber}</strong></div>
          {customerInfo.email && <div className="bw-summary-row"><span>Email:</span> <strong>{customerInfo.email}</strong></div>}
          <div className="bw-summary-row"><span>Số người:</span> <strong>{customerInfo.numberOfGuests} người</strong></div>
          <div className="bw-summary-row"><span>Ngày giờ:</span> <strong>{startTime} ngày {bookingDate}</strong></div>
          <div className="bw-summary-row"><span>Thời lượng:</span> <strong>{durationHours} giờ</strong></div>
          <div className="bw-summary-row"><span>Khu vực/Bàn:</span> <strong>{selectedTable?.tableName} ({selectedTable?.tableTypeName})</strong></div>
          {customerInfo.note && <div className="bw-summary-row"><span>Ghi chú:</span> <strong>{customerInfo.note}</strong></div>}
          <div className="bw-summary-row"><span>Tạm tính:</span> <strong className="highlight-price">{estimatedPrice.toLocaleString('vi-VN')} đ</strong></div>
        </div>
        
        <div className="bw-actions bw-actions-center">
          <button className="outline-btn" onClick={() => setStep(3)} disabled={saving}>Chỉnh sửa</button>
          <button className="primary-btn bw-large-btn" onClick={handleSubmit} disabled={saving || !policy.allowOnlineBooking}>
            {saving ? "Đang xử lý..." : "Xác nhận Đặt bàn"}
          </button>
        </div>
      </div>
    );
  };

  const renderStep5 = () => (
    <div className="bw-step bw-step-5 bw-success">
      <div className="bw-success-icon">✓</div>
      <h3>Đặt bàn thành công!</h3>
      <p>{policy.successMessage}</p>
      <button className="outline-btn" onClick={() => {
        setStep(1);
        setSelectedTable(null);
        setCustomerInfo({ customerName: '', phoneNumber: '', email: '', numberOfGuests: 4, note: '' });
      }}>
        Đặt bàn khác
      </button>
    </div>
  );

  return (
    <section className="landing-section booking-wizard-section" id="booking">
      <div className="booking-copy" style={{ textAlign: 'center', marginBottom: '40px' }}>
        <p className="eyebrow">Đặt bàn online</p>
        <h2>Trải nghiệm mượt mà, giữ bàn ngay tức thì</h2>
      </div>

      <div className="bw-container">
        {loadingData ? (
          <div className="bw-loading">Đang tải dữ liệu sơ đồ...</div>
        ) : (
          <>
            {step < 5 && (
              <div className="bw-stepper">
                {[1, 2, 3, 4].map(num => (
                  <div key={num} className={`bw-stepper-item ${step >= num ? 'active' : ''} ${step > num ? 'completed' : ''}`}>
                    <div className="bw-stepper-circle">{num}</div>
                  </div>
                ))}
              </div>
            )}

            <div className="bw-content">
              {step === 1 && renderStep1()}
              {step === 2 && renderStep2()}
              {step === 3 && renderStep3()}
              {step === 4 && renderStep4()}
              {step === 5 && renderStep5()}
            </div>
          </>
        )}
      </div>
    </section>
  );
}
