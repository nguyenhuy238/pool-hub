"use client";

import React from 'react';
import { usePOS } from '../POSContext';
import { Play, Wrench } from 'lucide-react';
import styles from '../pos.module.css';
import { sessionApi, bookingApi } from '@/lib/api/endpoints';
import type { VenueTableLayoutItem } from '@/types';
import { formatVietnamTime, utcTimestampMs } from '@/lib/dateTime';
import { useElapsed, formatDuration } from '../hooks/useElapsed';
import { estimateTimeAmount } from '../hooks/usePricingEstimate';
import { getPOSTableStatus } from '../lib/tableStatus';

function getTableColor(typeId: number) {
  switch (typeId) {
    case 1: return { color: '#3b82f6', bg: '#eff6ff', border: '#bfdbfe' }; // Pool
    case 2: return { color: '#9333ea', bg: '#faf5ff', border: '#e9d5ff' }; // VIP
    case 3: return { color: '#16a34a', bg: '#f0fdf4', border: '#bbf7d0' }; // Carom
    case 4: return { color: '#f97316', bg: '#fff7ed', border: '#fed7aa' }; // Snooker
    default: return { color: '#64748b', bg: '#f8fafc', border: '#e2e8f0' }; // Default
  }
}

export function TableCard({ table }: { table: VenueTableLayoutItem }) {
  const { selectedTable, setSelectedTable, setIsDrawerOpen, triggerRefresh, rateByType } = usePOS();

  const status = getPOSTableStatus(table);
  const isSelected = selectedTable?.tableId === table.tableId;
  const theme = getTableColor(table.tableTypeId);

  // Đồng hồ đếm giờ cho bàn đang chơi (chỉ chạy khi status === 'active').
  const elapsed = useElapsed(table.activeSessionStartedAtUtc, status === 'active');
  const estimatedAmount = estimateTimeAmount(rateByType, table.tableTypeId, elapsed);

  const now = Date.now();
  const nextBookingTime = table.nextBookingStartTimeUtc ? utcTimestampMs(table.nextBookingStartTimeUtc) : null;
  const minsToNextBooking = nextBookingTime ? (nextBookingTime - now) / 60000 : null;

  // Chỉ được nhận bàn <= 15 phút trước giờ đặt.
  const isReadyToReceive = status === 'reserved' && minsToNextBooking !== null && minsToNextBooking <= 15;
  // Cảnh báo nếu đang chơi nhưng sắp tới giờ khách đặt (<= 30 phút).
  const isPlayingButReservedSoon =
    status === 'active' && minsToNextBooking !== null && minsToNextBooking <= 30 && minsToNextBooking >= -60;

  const handleClick = () => {
    setSelectedTable(table);
    setIsDrawerOpen(true);
  };

  const handleStartSession = async (e: React.MouseEvent) => {
    e.stopPropagation(); // Không mở drawer khi bấm mở nhanh
    try {
      await sessionApi.start({ tableId: table.tableId });
      triggerRefresh();
    } catch (err) {
      console.error("Lỗi khi mở bàn", err);
      alert("Không thể mở bàn. Vui lòng thử lại.");
    }
  };

  // BẢO TRÌ / NGỪNG HOẠT ĐỘNG — không cho thao tác
  if (status === 'maintenance') {
    const label = table.operationalStatus === 5 ? 'NGỪNG HĐ' : 'BẢO TRÌ';
    return (
      <div
        className={styles.tableCard}
        style={{ cursor: 'not-allowed', opacity: 0.65, background: '#f1f5f9', borderTop: '4px solid #94a3b8' }}
        title="Bàn không khả dụng"
      >
        <div className={styles.tableCardHeader}>
          <h3 className={styles.tableName} style={{ color: '#64748b' }}>{table.tableName}</h3>
          <span className={styles.emptyBadge} style={{ background: '#e2e8f0', color: '#475569', borderColor: '#cbd5e1' }}>{label}</span>
        </div>
        <div style={{ marginTop: 'auto', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.4rem', color: '#94a3b8', fontSize: '0.85rem' }}>
          <Wrench size={16} /> Tạm ngưng phục vụ
        </div>
      </div>
    );
  }

  // ĐÃ GIỮ (có booking sắp tới)
  if (status === 'reserved') {
    return (
      <div
        onClick={handleClick}
        className={`${styles.tableCard} ${isSelected ? styles.tableSelected : ''}`}
        style={{ cursor: 'pointer', borderTop: `4px solid #eab308` }}
      >
        <div className={styles.tableCardHeader}>
          <h3 className={styles.tableName}>{table.tableName}</h3>
          <span className={styles.emptyBadge} style={{ background: '#fef08a', color: '#854d0e', borderColor: '#fde047' }}>ĐÃ GIỮ</span>
        </div>

        <div style={{ padding: '0 8px', flex: 1, display: 'flex', flexDirection: 'column', justifyContent: 'center', fontSize: '0.85rem', color: '#475569', textAlign: 'center' }}>
          Sắp có khách đặt lúc<br />
          <strong style={{ fontSize: '1rem', color: '#854d0e', marginTop: '2px' }}>
            {formatVietnamTime(table.nextBookingStartTimeUtc!)}
          </strong>
        </div>

        <div style={{ display: 'flex', gap: '4px', width: '100%', marginTop: 'auto' }}>
          <button
            onClick={async (e) => {
              e.stopPropagation();
              if (confirm(`Bạn có chắc muốn HỦY lịch đặt ${table.nextBookingCode || table.nextBookingId}?`)) {
                try {
                  await bookingApi.cancel(table.nextBookingId!);
                  triggerRefresh();
                } catch (err) {
                  console.error("Lỗi khi hủy bàn", err);
                  alert("Không thể hủy lịch đặt. Vui lòng thử lại.");
                }
              }
            }}
            className="primary-btn"
            style={{ flex: 1, minWidth: 0, padding: '0.35rem 0.2rem', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.2rem', background: '#fef08a', color: '#854d0e', border: '1px solid #fde047', fontSize: '0.8rem' }}
          >
            Hủy
          </button>
          <button
            disabled={!isReadyToReceive}
            onClick={async (e) => {
              e.stopPropagation();
              if (!isReadyToReceive) return;
              if (confirm(`Nhận bàn (Booking ${table.nextBookingCode || table.nextBookingId}) cho bàn này?`)) {
                try {
                  await bookingApi.startSession(table.nextBookingId!, table.tableId);
                  triggerRefresh();
                } catch (err) {
                  console.error("Lỗi khi nhận bàn", err);
                  alert("Không thể nhận bàn. Vui lòng thử lại.");
                }
              }
            }}
            className="primary-btn"
            style={{
              flex: 2,
              minWidth: 0,
              padding: '0.35rem 0.2rem',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              gap: '0.25rem',
              background: isReadyToReceive ? '#eab308' : '#e2e8f0',
              color: isReadyToReceive ? 'white' : '#94a3b8',
              border: 'none',
              fontSize: '0.8rem',
              cursor: isReadyToReceive ? 'pointer' : 'not-allowed'
            }}
          >
            <Play size={14} fill="currentColor" style={{ flexShrink: 0 }} /> 
            <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>Nhận bàn</span>
          </button>
        </div>
      </div>
    );
  }

  // TRỐNG
  if (status === 'empty') {
    return (
      <div
        onClick={handleClick}
        className={`${styles.tableCard} ${isSelected ? styles.tableSelected : ''}`}
        style={{ cursor: 'pointer', borderTop: `4px solid ${theme.color}` }}
      >
        <div className={styles.tableCardHeader}>
          <h3 className={styles.tableName}>{table.tableName}</h3>
          <span className={styles.emptyBadge} style={{ background: theme.bg, color: theme.color, borderColor: theme.border }}>TRỐNG</span>
        </div>
        <button
          onClick={handleStartSession}
          className="primary-btn"
          style={{ width: '100%', marginTop: 'auto', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem', background: '#f1f5f9', color: '#475569' }}
        >
          <Play size={16} fill="currentColor" /> Mở nhanh
        </button>
      </div>
    );
  }

  // ĐANG CHƠI
  return (
    <div
      onClick={handleClick}
      className={`${styles.tableCard} ${styles.tableActive} ${isSelected ? styles.tableSelected : ''}`}
      style={{ borderTop: `4px solid ${theme.color}`, background: theme.bg, position: 'relative' }}
    >
      {isPlayingButReservedSoon && (
        <div style={{ position: 'absolute', top: '-10px', right: '-10px', background: '#ef4444', color: 'white', fontSize: '0.75rem', padding: '2px 8px', borderRadius: '12px', fontWeight: 'bold', boxShadow: '0 2px 4px rgba(0,0,0,0.2)', zIndex: 10 }}>
          Sắp tới giờ khách đặt!
        </div>
      )}
      <div className={styles.tableCardHeader}>
        <h3 className={styles.tableName} style={{ color: theme.color }}>{table.tableName}</h3>
        <span className={styles.timerBadge} style={{ background: theme.color, color: 'white', padding: '2px 8px', borderRadius: '6px', fontSize: '0.85rem' }}>
          {formatDuration(elapsed)}
        </span>
      </div>
      <div className={styles.tableCardFooter}>
        <div className={styles.billAmount} style={{ color: theme.color }}>
          {estimatedAmount !== null ? `~${estimatedAmount.toLocaleString()}Đ` : 'Đang chơi'}
        </div>
      </div>
    </div>
  );
}
