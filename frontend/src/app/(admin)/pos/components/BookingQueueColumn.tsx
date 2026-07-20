"use client";

import React, { useEffect, useState } from 'react';
import { Calendar, User, Clock } from 'lucide-react';
import styles from '../pos.module.css';
import { bookingApi } from '@/lib/api/endpoints';
import type { Booking } from '@/types';
import { usePOS } from '../POSContext';
import { formatVietnamTime, getVietnamDateInputValue, utcTimestampMs } from '@/lib/dateTime';

function BookingItem({ booking }: { booking: Booking }) {
  const { triggerRefresh } = usePOS();
  const [cardState, setCardState] = useState<"NORMAL" | "WARNING" | "LATE">("NORMAL");
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
    const checkTime = () => {
      const now = Date.now();
      const start = utcTimestampMs(booking.startTimeUtc);
      const diffMins = (start - now) / 60000;

      if (diffMins < 0) {
        setCardState("LATE");
      } else if (diffMins <= 15) {
        setCardState("WARNING");
      } else {
        setCardState("NORMAL");
      }
    };

    checkTime();
    const interval = setInterval(checkTime, 60000);
    return () => clearInterval(interval);
  }, [booking.startTimeUtc]);

  const stateClass = cardState === "LATE" ? styles.bookingLate 
                   : cardState === "WARNING" ? styles.bookingWarning 
                   : styles.bookingNormal;
                   
  return (
    <div className={`${styles.bookingCard} ${stateClass}`}>
      <div className={styles.cardHeader}>
        <div className={styles.timeWrap}>
          <Clock size={18} />
          {mounted ? (
            <span className={styles.timeText}>
              {formatVietnamTime(booking.startTimeUtc)}
            </span>
          ) : (
            <span className={styles.timeText}>--:--</span>
          )}
        </div>
        <span className={styles.tableBadge}>
          {booking.tableId ? `Bàn ${booking.tableId}` : 'Chờ xếp bàn'}
        </span>
      </div>
      
      <div className={styles.customerWrap}>
        <User size={16} />
        <span>{booking.customerName || 'Khách vãng lai'}</span>
      </div>
      
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '8px' }}>
        <div className={styles.depositText}>
          Số khách: {booking.numberOfGuests || 1}
        </div>
        <button 
          onClick={async () => {
            if (confirm(`Nhận bàn cho khách: ${booking.customerName || 'Khách'}?`)) {
              try {
                await bookingApi.startSession(booking.bookingId);
                triggerRefresh();
              } catch (err) {
                console.error("Lỗi khi nhận bàn", err);
                alert("Không thể nhận bàn. Vui lòng thử lại.");
              }
            }
          }}
          className="primary-btn" 
          style={{ padding: '0.25rem 0.75rem', fontSize: '0.85rem' }}
        >
          Nhận bàn
        </button>
      </div>
    </div>
  );
}

export function BookingQueueColumn() {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [loading, setLoading] = useState(true);
  const { refreshTrigger } = usePOS();

  useEffect(() => {
    const fetchBookings = async () => {
      try {
        setLoading(true);
        // Fetch Confirmed bookings
        const res = await bookingApi.list({ Status: 2 });
        const items = Array.isArray(res) ? res : (res as any).items || [];
        
        // Filter only today's bookings in business timezone.
        const today = getVietnamDateInputValue();
        const todaysBookings = items.filter((b: Booking) => {
           return getVietnamDateInputValue(new Date(utcTimestampMs(b.startTimeUtc))) === today;
        });
        
        // Sort by start time
        todaysBookings.sort((a: Booking, b: Booking) => utcTimestampMs(a.startTimeUtc) - utcTimestampMs(b.startTimeUtc));
        setBookings(todaysBookings);
      } catch (err) {
        console.error("Failed to fetch bookings", err);
      } finally {
        setLoading(false);
      }
    };

    fetchBookings();
  }, [refreshTrigger]);

  return (
    <div className={styles.queueColumn}>
      <div className={styles.header}>
        <h2 className={styles.headerTitle}>
          <Calendar size={20} strokeWidth={2.5} /> Lịch Đặt Hôm Nay
        </h2>
      </div>
      
      <div className={styles.scrollArea}>
        {loading ? (
          <div className="text-center py-4 text-sm text-gray-500">Đang tải...</div>
        ) : bookings.length === 0 ? (
          <div className="text-center py-4 text-sm text-gray-500">Không có lịch đặt bàn nào.</div>
        ) : (
          bookings.map(b => (
            <BookingItem key={b.bookingId} booking={b} />
          ))
        )}
      </div>
    </div>
  );
}
