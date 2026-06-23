import React, { useState, useEffect } from 'react';
import './BookingWizard.css';
import { availabilityApi, type LandingAvailability, type LandingPricing } from "@/lib/api/availabilityApi";
import { publicBookingApi } from "@/lib/api/publicBookingApi";
import type { BookingPolicySettings } from "@/lib/api/landingSettingsApi";
import { useToast } from "@/components/toast";
import type { VenueFloorLayoutItem, VenueZoneLayoutItem, VenueTableLayoutItem, PricingPlan, PricingPlanRule } from '@/types';
import { TableTimelinePicker, type TimelineBooking } from './TableTimelinePicker';

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
  
  const [bookingDate, setBookingDate] = useState(new Date().toISOString().slice(0, 10));
  const [startTime, setStartTime] = useState('19:00');
  const [durationHours, setDurationHours] = useState(Math.max(1, Math.round(policy.defaultDurationMinutes / 60)));
  const [existingBookings, setExistingBookings] = useState<TimelineBooking[]>([]);
  const [loadingBookings, setLoadingBookings] = useState(false);
  const [isTimeValid, setIsTimeValid] = useState(true);
  
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
      // Gọi API thật để lấy danh sách các khung giờ bị đặt
      const res = await publicBookingApi.getPublicCalendar(selectedTable.tableId, bookingDate);
      
      // apiFetch trả về mảng trực tiếp, không bọc trong .data nữa
      const dataArray = Array.isArray(res) ? res : ((res as any).data || []);
      
      const targetDate = new Date(bookingDate);
      targetDate.setHours(0, 0, 0, 0);
      const nextDay = new Date(targetDate);
      nextDay.setDate(nextDay.getDate() + 1);

      // Map UTC về Local string (HH:mm)
      const mapped = dataArray.map((slot: any) => {
        const startStr = slot.startTimeUtc.endsWith('Z') ? slot.startTimeUtc : slot.startTimeUtc + 'Z';
        const endStr = slot.endTimeUtc.endsWith('Z') ? slot.endTimeUtc : slot.endTimeUtc + 'Z';
        
        const dStart = new Date(startStr);
        const dEnd = new Date(endStr);
        
        let hStart = dStart.getHours();
        let mStart = dStart.getMinutes();
        let hEnd = dEnd.getHours();
        let mEnd = dEnd.getMinutes();

        // Xử lý nếu booking vắt từ ngày hôm trước sang
        if (dStart < targetDate) {
          hStart = 0; mStart = 0;
        }

        // Xử lý nếu booking kéo dài qua ngày hôm sau hoặc kết thúc đúng 00:00 ngày hôm sau
        if (dEnd > nextDay || (dEnd.getTime() === nextDay.getTime())) {
          hEnd = 24; mEnd = 0;
        } else if (hEnd === 0 && mEnd === 0 && dEnd > dStart) {
          hEnd = 24; mEnd = 0;
        }

        return {
          startTime: `${hStart.toString().padStart(2, '0')}:${mStart.toString().padStart(2, '0')}`,
          endTime: `${hEnd.toString().padStart(2, '0')}:${mEnd.toString().padStart(2, '0')}`
        };
      });
      
      setExistingBookings(mapped);
    } catch (err) {
      console.error(err);
      setExistingBookings([]);
    } finally {
      setLoadingBookings(false);
    }
  };

  // Pricing Calculation
  const calculateEstimatedPrice = () => {
    if (!selectedTable || !pricing || !bookingDate || !startTime) return 0;
    
    // Find rules for this TableType
    const rulesForTableType = pricing.rules.filter(r => r.tableTypeId === selectedTable.tableTypeId);
    if (rulesForTableType.length === 0) return 0;
    
    const dayOfWeek = new Date(bookingDate).getDay(); // 0 is Sunday
    // Adjust dayOfWeek if backend expects 1-7 or 0-6. Let's assume standard JS getDay().
    
    // Simplistic match: find rule that matches day and time.
    // In a real app, logic would consider overlap between rule times and booking times.
    // We'll just take the first matching rule or the default rule.
    let applicableRule = rulesForTableType.find(r => r.dayOfWeek === dayOfWeek || r.dayOfWeek === -1); 
    if (!applicableRule) applicableRule = rulesForTableType[0];

    const hourlyRate = applicableRule?.hourlyRate || 0;
    return hourlyRate * durationHours;
  };

  const handleNextStep1 = () => {
    if (!selectedTable) {
      toast("Vui lòng chọn bàn.", "error");
      return;
    }
    setStep(2);
  };

  const handleNextStep2 = () => {
    if (!bookingDate || !startTime) {
      toast("Vui lòng chọn ngày và giờ.", "error");
      return;
    }
    const start = new Date(`${bookingDate}T${startTime}`);
    if (Number.isNaN(start.getTime()) || start.getTime() < Date.now()) {
      toast("Không thể chọn ngày/giờ trong quá khứ.", "error");
      return;
    }
    
    // Check overlap one more time
    const timeToMinutes = (timeStr: string) => {
      const [h, m] = timeStr.split(':').map(Number);
      return h * 60 + (m || 0);
    };
    const startMins = timeToMinutes(startTime);
    const endMins = startMins + durationHours * 60;
    const isOverlap = existingBookings.some(b => {
      const bStart = timeToMinutes(b.startTime);
      const bEnd = timeToMinutes(b.endTime);
      return (startMins < bEnd && endMins > bStart);
    });

    if (isOverlap) {
      toast("Khoảng thời gian này đã có người đặt.", "error");
      return;
    }

    setStep(3);
  };

  const handleNextStep3 = () => {
    if (!customerInfo.customerName.trim()) {
      toast("Vui lòng nhập họ tên khách.", "error");
      return;
    }
    if (!customerInfo.phoneNumber.trim()) {
      toast("Vui lòng nhập số điện thoại.", "error");
      return;
    }
    setStep(4);
  };

  const handleSubmit = async () => {
    setSaving(true);
    try {
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
                      className={`bw-table-card ${table.operationalStatus === 1 ? 'available' : 'unavailable'} ${selectedTable?.tableId === table.tableId ? 'selected' : ''}`}
                      onClick={() => table.operationalStatus === 1 ? setSelectedTable(table) : toast("Bàn này hiện không trống.", "error")}
                      disabled={table.operationalStatus !== 1}
                    >
                      <span className="bw-table-name">{table.tableName}</span>
                      <span className="bw-table-type">{table.tableTypeName}</span>
                      <span className="bw-table-status">
                        {table.operationalStatus === 1 ? "Trống" : table.operationalStatus === 2 ? "Đang chơi" : "Đã đặt"}
                      </span>
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

  const renderStep2 = () => {
    const estimatedPrice = calculateEstimatedPrice();
    return (
      <div className="bw-step bw-step-2">
        <h3>2. Chọn Thời gian & Xem giá</h3>
        <p className="bw-subtitle">Bàn đã chọn: <strong>{selectedTable?.tableName}</strong> ({selectedTable?.tableTypeName})</p>
        
        <div className="bw-form-grid" style={{ marginBottom: '16px' }}>
          <label><span>Ngày đặt *</span>
            <input type="date" min={new Date().toISOString().slice(0, 10)} 
                   max={new Date(Date.now() + policy.advanceBookingDays * 24 * 60 * 60 * 1000).toISOString().slice(0, 10)} 
                   value={bookingDate} onChange={e => setBookingDate(e.target.value)} />
          </label>
        </div>

        <div className="bw-timeline-container" style={{ marginBottom: '32px' }}>
          <p style={{ fontSize: '0.875rem', fontWeight: 500, color: '#374151', marginBottom: '8px' }}>
            Chọn giờ bắt đầu trên Timeline (Màu đỏ: Đã đặt, Xanh: Đang chọn)
          </p>
          {loadingBookings ? (
            <div style={{ padding: '20px', textAlign: 'center', color: '#6b7280' }}>Đang tải lịch đặt...</div>
          ) : (
            <TableTimelinePicker 
              existingBookings={existingBookings}
              selectedDate={bookingDate}
              startTime={startTime}
              durationHours={durationHours}
              onTimeChange={(newStartTime, newDuration) => {
                setStartTime(newStartTime);
                setDurationHours(newDuration);
              }}
            />
          )}
        </div>

        <div className="bw-form-grid">
          <label><span>Giờ bắt đầu * (Chọn trên timeline hoặc nhập)</span>
            <input type="time" value={startTime} onChange={e => setStartTime(e.target.value)} />
          </label>
          <label><span>Thời lượng *</span>
            <select value={durationHours} onChange={e => setDurationHours(Number(e.target.value))}>
              {[1, 2, 3, 4, 5, 6].filter(hour => hour * 60 >= policy.minDurationMinutes && hour * 60 <= policy.maxDurationMinutes)
                .map(hour => <option key={hour} value={hour}>{hour} giờ</option>)}
            </select>
          </label>
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
    const estimatedPrice = calculateEstimatedPrice();
    return (
      <div className="bw-step bw-step-4">
        <h3>4. Xác nhận Đặt bàn</h3>
        
        <div className="bw-summary">
          <div className="bw-summary-row"><span>Khách hàng:</span> <strong>{customerInfo.customerName} - {customerInfo.phoneNumber}</strong></div>
          <div className="bw-summary-row"><span>Ngày giờ:</span> <strong>{startTime} ngày {bookingDate}</strong></div>
          <div className="bw-summary-row"><span>Thời lượng:</span> <strong>{durationHours} giờ</strong></div>
          <div className="bw-summary-row"><span>Khu vực/Bàn:</span> <strong>{selectedTable?.tableName} ({selectedTable?.tableTypeName})</strong></div>
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
