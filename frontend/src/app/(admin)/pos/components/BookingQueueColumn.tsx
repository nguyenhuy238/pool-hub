"use client";

import React, { useEffect, useState, useCallback } from 'react';
import { Calendar, User, Clock, AlertTriangle, PhoneCall, XCircle } from 'lucide-react';
import styles from '../pos.module.css';
import { bookingApi } from '@/lib/api/endpoints';
import type { Booking } from '@/types';
import { usePOS } from '../POSContext';
import { formatVietnamTime, getVietnamDateInputValue, utcTimestampMs } from '@/lib/dateTime';

const EARLY_CHECK_IN_MINUTES = 15;

type CardState = 'UPCOMING' | 'READY' | 'LATE' | 'EXPIRED';

function getCardState(startMs: number, status: number): CardState {
  // status 9 = Late (backend đã chuyển)
  if (status === 9) return 'LATE';

  const now = Date.now();
  const diffMins = (startMs - now) / 60000;

  if (diffMins > EARLY_CHECK_IN_MINUTES) return 'UPCOMING';
  if (diffMins >= -EARLY_CHECK_IN_MINUTES) return 'READY';
  return 'LATE';
}

function formatCountdown(ms: number): string {
  const totalSecs = Math.max(0, Math.floor(ms / 1000));
  const m = Math.floor(totalSecs / 60);
  const s = totalSecs % 60;
  return `${m}:${s.toString().padStart(2, '0')}`;
}

function BookingItem({ booking }: { booking: Booking }) {
  const { triggerRefresh } = usePOS();
  const [cardState, setCardState] = useState<CardState>('UPCOMING');
  const [mounted, setMounted] = useState(false);
  const [countdown, setCountdown] = useState('');
  const [cancelling, setCancelling] = useState(false);
  const [starting, setStarting] = useState(false);

  const startMs = utcTimestampMs(booking.startTimeUtc);
  const earliestMs = startMs - EARLY_CHECK_IN_MINUTES * 60 * 1000;

  const checkTime = useCallback(() => {
    const now = Date.now();
    const state = getCardState(startMs, booking.status);
    setCardState(state);

    if (state === 'UPCOMING') {
      setCountdown(formatCountdown(earliestMs - now));
    } else {
      setCountdown('');
    }
  }, [startMs, earliestMs, booking.status]);

  useEffect(() => {
    setMounted(true);
    checkTime();
    const interval = setInterval(checkTime, 1000);
    return () => clearInterval(interval);
  }, [checkTime]);

  const handleStart = async () => {
    if (starting) return;
    if (!confirm(`Nhận bàn cho khách: ${booking.customerName || 'Khách'}?`)) return;
    try {
      setStarting(true);
      await bookingApi.startSession(booking.bookingId, booking.tableId);
      triggerRefresh();
    } catch (err: any) {
      const msg = err?.response?.data?.message || err?.message || 'Không thể nhận bàn. Vui lòng thử lại.';
      alert(msg);
    } finally {
      setStarting(false);
    }
  };

  const handleCancelLate = async () => {
    if (cancelling) return;
    const depositText = booking.deposit?.paidAmount && booking.deposit.paidAmount > 0
      ? ` Tiền cọc ${booking.deposit.paidAmount.toLocaleString()}đ sẽ KHÔNG được hoàn.`
      : '';
    if (!confirm(`Khách trễ giờ nhận bàn.${depositText}\nXác nhận hủy booking và gửi email thông báo cho khách?`)) return;
    try {
      setCancelling(true);
      await bookingApi.cancel(booking.bookingId, 'Khách trễ giờ nhận bàn, nhân viên hủy booking.');
      triggerRefresh();
    } catch (err: any) {
      const msg = err?.response?.data?.message || err?.message || 'Không thể hủy booking.';
      alert(msg);
    } finally {
      setCancelling(false);
    }
  };

  const depositPaid = booking.deposit?.paidAmount ?? 0;

  // Màu và icon theo state
  const stateConfig = {
    UPCOMING: {
      className: styles.bookingNormal,
      badge: null,
      icon: <Clock size={14} style={{ color: '#64748b' }} />,
    },
    READY: {
      className: styles.bookingWarning,
      badge: <span style={{ background: '#22c55e', color: '#fff', fontSize: '0.7rem', padding: '2px 7px', borderRadius: '99px', fontWeight: 700 }}>SẴN SÀNG</span>,
      icon: <Clock size={14} style={{ color: '#16a34a' }} />,
    },
    LATE: {
      className: styles.bookingLate,
      badge: <span style={{ background: '#f97316', color: '#fff', fontSize: '0.7rem', padding: '2px 7px', borderRadius: '99px', fontWeight: 700 }}>TRỄ GIỜ</span>,
      icon: <AlertTriangle size={14} style={{ color: '#f97316' }} />,
    },
    EXPIRED: {
      className: styles.bookingLate,
      badge: <span style={{ background: '#ef4444', color: '#fff', fontSize: '0.7rem', padding: '2px 7px', borderRadius: '99px', fontWeight: 700 }}>QUÁ GIỜ</span>,
      icon: <XCircle size={14} style={{ color: '#ef4444' }} />,
    },
  };

  const config = stateConfig[cardState];

  return (
    <div className={`${styles.bookingCard} ${config.className}`}>
      {/* Header: giờ + badge */}
      <div className={styles.cardHeader}>
        <div className={styles.timeWrap}>
          {config.icon}
          {mounted ? (
            <span className={styles.timeText}>
              {formatVietnamTime(booking.startTimeUtc)}
            </span>
          ) : (
            <span className={styles.timeText}>--:--</span>
          )}
          {config.badge}
        </div>
        <span className={styles.tableBadge}>
          {booking.tableId ? `Bàn ${booking.tableId}` : 'Chờ xếp bàn'}
        </span>
      </div>

      {/* Khách */}
      <div className={styles.customerWrap}>
        <User size={16} />
        <span>{booking.customerName || 'Khách vãng lai'}</span>
      </div>

      {/* Thông tin nhận bàn / cọc */}
      <div style={{ fontSize: '0.78rem', color: '#64748b', marginTop: '6px', display: 'flex', flexDirection: 'column', gap: '2px' }}>
        {mounted && cardState === 'UPCOMING' && (
          <span>🕐 Nhận bàn từ: <strong style={{ color: '#0f172a' }}>
            {formatVietnamTime(new Date(earliestMs).toISOString())}
          </strong> {countdown && <span style={{ color: '#2563eb' }}>(còn {countdown})</span>}</span>
        )}
        {depositPaid > 0 && (
          <span>💰 Đã cọc: <strong style={{ color: '#16a34a' }}>{depositPaid.toLocaleString()}đ</strong></span>
        )}
        <span>👥 {booking.numberOfGuests || 1} khách</span>
      </div>

      {/* Actions */}
      <div style={{ display: 'flex', gap: '6px', marginTop: '10px' }}>
        {/* Nút nhận bàn — disable khi UPCOMING */}
        <button
          onClick={handleStart}
          disabled={cardState === 'UPCOMING' || cardState === 'EXPIRED' || starting}
          className="primary-btn"
          style={{
            flex: 1,
            padding: '0.3rem 0.6rem',
            fontSize: '0.82rem',
            opacity: (cardState === 'UPCOMING' || cardState === 'EXPIRED') ? 0.45 : 1,
            cursor: (cardState === 'UPCOMING' || cardState === 'EXPIRED') ? 'not-allowed' : 'pointer',
            background: cardState === 'READY' ? '#22c55e' : undefined,
          }}
        >
          {starting ? '...' : cardState === 'UPCOMING' ? 'Chưa đến giờ' : 'Nhận bàn'}
        </button>

        {/* Nút hủy — chỉ hiển thị khi LATE */}
        {cardState === 'LATE' && (
          <button
            onClick={handleCancelLate}
            disabled={cancelling}
            className="primary-btn"
            title="Hủy booking khi khách trễ — tiền cọc sẽ không hoàn"
            style={{
              padding: '0.3rem 0.6rem',
              fontSize: '0.82rem',
              background: '#ef4444',
              color: '#fff',
              border: 'none',
              display: 'flex',
              alignItems: 'center',
              gap: '4px',
            }}
          >
            <XCircle size={14} />
            {cancelling ? '...' : 'Hủy'}
          </button>
        )}
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
        // Lấy cả Confirmed (status=2) và Late (status=9)
        const [confirmedRes, lateRes] = await Promise.all([
          bookingApi.list({ Status: 2 }),
          bookingApi.list({ Status: 9 }),
        ]);
        const confirmed = Array.isArray(confirmedRes) ? confirmedRes : (confirmedRes as any).items || [];
        const late = Array.isArray(lateRes) ? lateRes : (lateRes as any).items || [];
        const combined: Booking[] = [...confirmed, ...late];

        // Chỉ lấy hôm nay
        const today = getVietnamDateInputValue();
        const todaysBookings = combined.filter((b: Booking) =>
          getVietnamDateInputValue(new Date(utcTimestampMs(b.startTimeUtc))) === today
        );

        // Sắp xếp theo giờ bắt đầu
        todaysBookings.sort((a: Booking, b: Booking) => utcTimestampMs(a.startTimeUtc) - utcTimestampMs(b.startTimeUtc));
        setBookings(todaysBookings);
      } catch (err) {
        console.error('Failed to fetch bookings', err);
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
