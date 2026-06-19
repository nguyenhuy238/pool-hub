"use client";

import React, { useEffect, useState } from 'react';
import { Calendar, User, Clock } from 'lucide-react';
import styles from '../pos.module.css';
import { bookingApi } from '@/lib/api/endpoints';
import type { Booking } from '@/types';
import { usePOS } from '../POSContext';

function BookingItem({ booking }: { booking: Booking }) {
  const [cardState, setCardState] = useState<"NORMAL" | "WARNING" | "LATE">("NORMAL");
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
    const checkTime = () => {
      const now = new Date().getTime();
      const start = new Date(booking.startTimeUtc).getTime();
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
              {new Date(booking.startTimeUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
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
      
      <div className={styles.depositText}>
        Số khách: {booking.numberOfGuests || 1}
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
        
        // Filter only today's bookings
        const todayStr = new Date().toISOString().slice(0, 10);
        const todaysBookings = items.filter((b: Booking) => b.startTimeUtc.startsWith(todayStr));
        
        // Sort by start time
        todaysBookings.sort((a: Booking, b: Booking) => new Date(a.startTimeUtc).getTime() - new Date(b.startTimeUtc).getTime());
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
