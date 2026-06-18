"use client";

import { useMemo, useState } from "react";
import { bookingApi, venueApi } from "@/lib/api/endpoints";
import { PageHeader, StateBlock, useLoad } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { BookingCalendarItem } from "@/types";
import "./calendar.css";

// Giờ hoạt động: 08:00 - 24:00 (16 tiếng)
const START_HOUR = 8;
const END_HOUR = 24;
const TOTAL_HOURS = END_HOUR - START_HOUR;

export default function BookingCalendarPage() {
  // Mặc định chọn ngày hôm nay
  const [selectedDate, setSelectedDate] = useState(() => {
    const today = new Date();
    return today.toISOString().split("T")[0];
  });

  const [statusFilter, setStatusFilter] = useState("");
  
  const [selectedBooking, setSelectedBooking] = useState<BookingCalendarItem | null>(null);
  const [actionLoading, setActionLoading] = useState(false);
  const toast = useToast();

  const { data, loading, error, reload } = useLoad(async () => {
    if (!selectedDate) return { items: [], tables: [] };
    
    // Tính khoảng thời gian từ 00:00:00 đến 23:59:59 của ngày được chọn theo UTC
    // (Trong thực tế cần convert local timezone sang UTC cho chính xác)
    const fromDate = new Date(`${selectedDate}T00:00:00Z`);
    const toDate = new Date(`${selectedDate}T23:59:59Z`);
    
    const params: Record<string, string> = {};
    if (statusFilter) params.status = statusFilter;
    
    const [bookingsRes, tablesRes] = await Promise.all([
      bookingApi.calendar(fromDate.toISOString(), toDate.toISOString(), params),
      venueApi.tables({ pageSize: 500 }) // Fetch up to 500 tables to populate Y-axis
    ]);
    
    const items = Array.isArray(bookingsRes) ? bookingsRes : (bookingsRes && 'items' in bookingsRes ? bookingsRes.items : []);
    const tables = Array.isArray(tablesRes) ? tablesRes : (tablesRes && 'items' in tablesRes ? tablesRes.items : []);
    
    return { items: items || [], tables: tables || [] };
  }, [selectedDate, statusFilter]);

  const items = useMemo(() => (data?.items as BookingCalendarItem[] | undefined) ?? [], [data?.items]);
  const venueTables = useMemo(() => data?.tables ?? [], [data?.tables]);

  // Group bookings by table
  const tableGroups = useMemo(() => {
    const groups = new Map<number, { tableId: number; tableCode: string; tableName: string; bookings: BookingCalendarItem[] }>();
    
    // Populate all tables first
    venueTables.forEach(t => {
      groups.set(t.tableId, {
        tableId: t.tableId,
        tableCode: t.tableCode,
        tableName: t.tableName,
        bookings: []
      });
    });

    items.forEach(booking => {
      const tId = booking.tableId || 0;
      if (!groups.has(tId)) {
        groups.set(tId, {
          tableId: tId,
          tableCode: booking.tableCode || "Unknown",
          tableName: booking.tableName || "Không xác định",
          bookings: []
        });
      }
      groups.get(tId)!.bookings.push(booking);
    });
    
    return Array.from(groups.values()).sort((a, b) => a.tableCode.localeCompare(b.tableCode));
  }, [items, venueTables]);

  // Tính toán style (left, width) dựa trên StartTime và EndTime so với khung giờ 08:00 - 24:00
  const getPositionStyle = (startTimeStr: string, endTimeStr: string) => {
    const start = new Date(startTimeStr);
    const end = new Date(endTimeStr);
    
    // Chuyển về giờ local
    const startHours = start.getHours() + start.getMinutes() / 60;
    const endHours = end.getHours() + end.getMinutes() / 60;
    
    // Nếu booking ngoài giờ hoạt động (trước 8h sáng)
    const effectiveStart = Math.max(START_HOUR, startHours);
    const effectiveEnd = Math.min(END_HOUR, endHours);
    
    if (effectiveStart >= END_HOUR || effectiveEnd <= START_HOUR) {
      return { display: 'none' };
    }
    
    const leftPercent = ((effectiveStart - START_HOUR) / TOTAL_HOURS) * 100;
    const widthPercent = ((effectiveEnd - effectiveStart) / TOTAL_HOURS) * 100;
    
    return {
      left: `${leftPercent}%`,
      width: `${widthPercent}%`
    };
  };

  const formatTime = (isoString: string) => {
    const d = new Date(isoString);
    return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  };

  const handleAction = async (action: 'confirm' | 'cancel') => {
    if (!selectedBooking) return;
    try {
      setActionLoading(true);
      if (action === 'confirm') {
        await bookingApi.confirm(selectedBooking.bookingId);
        toast('Đã xác nhận đặt bàn thành công.', 'success');
      } else {
        await bookingApi.cancel(selectedBooking.bookingId);
        toast('Đã hủy đặt bàn thành công.', 'success');
      }
      setSelectedBooking(null);
      reload();
    } catch (err: any) {
      toast(err.message || 'Có lỗi xảy ra', 'error');
    } finally {
      setActionLoading(false);
    }
  };

  return (
    <div className="calendar-container">
      <PageHeader 
        title="Lịch Đặt Bàn (Calendar)" 
        description="Xem danh sách đặt bàn theo trục thời gian trong ngày." 
      />

      <div className="calendar-filters">
        <div className="filter-group">
          <label>Ngày xem lịch</label>
          <input 
            type="date" 
            value={selectedDate} 
            onChange={e => setSelectedDate(e.target.value)} 
          />
        </div>
        <div className="filter-group">
          <label>Trạng thái</label>
          <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)}>
            <option value="">Tất cả trạng thái</option>
            <option value="1">Pending</option>
            <option value="2">Confirmed</option>
            <option value="3">Cancelled</option>
          </select>
        </div>
      </div>

      <StateBlock loading={loading} error={error} />

      {!loading && (
        <div className="timeline-wrapper">
          {tableGroups.length === 0 ? (
            <div className="no-bookings">Không có lịch đặt bàn nào trong ngày này.</div>
          ) : (
            <div className="timeline">
              {/* Header: Cột thời gian (cứ 2 tiếng 1 mốc) */}
              <div className="timeline-header">
                <div className="timeline-col-header">Bàn / Thời gian</div>
                <div className="timeline-grid-wrapper">
                  {Array.from({ length: TOTAL_HOURS / 2 }).map((_, i) => (
                    <div key={i} className="timeline-col-header" style={{ flex: 1, textAlign: 'left', borderRight: '1px dashed var(--line)' }}>
                      {START_HOUR + i * 2}:00
                    </div>
                  ))}
                </div>
              </div>

              {/* Rows: Mỗi hàng là 1 bàn */}
              {tableGroups.map(group => (
                <div key={group.tableId} className="timeline-row">
                  <div className="table-info-cell">
                    <strong>{group.tableName}</strong>
                    <span>Mã: {group.tableCode}</span>
                  </div>
                  <div className="timeline-grid-wrapper">
                    {/* Render lưới dọc gạch ngang */}
                    <div style={{ position: 'absolute', top: 0, bottom: 0, left: 0, right: 0, display: 'flex' }}>
                      {Array.from({ length: TOTAL_HOURS / 2 }).map((_, i) => (
                        <div key={i} style={{ flex: 1, borderRight: '1px dashed var(--line)', opacity: 0.5 }}></div>
                      ))}
                    </div>

                    {/* Render booking blocks */}
                    {group.bookings.map(booking => (
                      <div 
                        key={booking.bookingId} 
                        className={`booking-block status-${booking.status}`}
                        style={getPositionStyle(booking.startTimeUtc, booking.endTimeUtc)}
                        title={`Booking: ${booking.bookingCode}\nKhách: ${booking.customerName}\nGiờ: ${formatTime(booking.startTimeUtc)} - ${formatTime(booking.endTimeUtc)}`}
                        onClick={() => setSelectedBooking(booking)}
                      >
                        <span className="booking-time">{formatTime(booking.startTimeUtc)} - {formatTime(booking.endTimeUtc)}</span>
                        <span className="booking-customer">{booking.customerName || 'Khách vãng lai'}</span>
                        {booking.customerPhone && <span className="booking-phone">{booking.customerPhone}</span>}
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Booking Details Modal */}
      {selectedBooking && (
        <div className="modal-overlay" onClick={() => setSelectedBooking(null)}>
          <div className="modal-content" onClick={e => e.stopPropagation()}>
            <h3>Chi tiết Đặt bàn</h3>
            <div className="modal-body">
              <p><strong>Khách hàng:</strong> {selectedBooking.customerName || 'Khách vãng lai'}</p>
              <p><strong>Điện thoại:</strong> {selectedBooking.customerPhone || '-'}</p>
              <p><strong>Bàn:</strong> {selectedBooking.tableName} (Mã: {selectedBooking.tableCode})</p>
              <p><strong>Thời gian:</strong> {formatTime(selectedBooking.startTimeUtc)} - {formatTime(selectedBooking.endTimeUtc)}</p>
              <p><strong>Trạng thái:</strong> {
                selectedBooking.status === 1 ? 'Chờ xác nhận (Pending)' :
                selectedBooking.status === 2 ? 'Đã xác nhận (Confirmed)' : 'Đã hủy (Cancelled)'
              }</p>
            </div>
            <div className="modal-actions">
              {selectedBooking.status === 1 && (
                <button className="btn-confirm" disabled={actionLoading} onClick={() => handleAction('confirm')}>Xác nhận (Confirm)</button>
              )}
              {(selectedBooking.status === 1 || selectedBooking.status === 2) && (
                <button className="btn-cancel" disabled={actionLoading} onClick={() => handleAction('cancel')}>Hủy đơn (Cancel)</button>
              )}
              <button className="btn-close" onClick={() => setSelectedBooking(null)}>Đóng</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
