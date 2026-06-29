"use client";

import React, { useEffect, useState } from 'react';
import { usePOS } from '../POSContext';
import { Play } from 'lucide-react';
import styles from '../pos.module.css';
import { venueApi, sessionApi, bookingApi } from '@/lib/api/endpoints';
import type { VenueTableLayoutItem, VenueLayoutResponse } from '@/types';

function formatDuration(ms: number) {
  const totalSeconds = Math.max(0, Math.floor(ms / 1000));
  const h = Math.floor(totalSeconds / 3600).toString().padStart(2, '0');
  const m = Math.floor((totalSeconds % 3600) / 60).toString().padStart(2, '0');
  const s = (totalSeconds % 60).toString().padStart(2, '0');
  return `${h}:${m}:${s}`;
}

function getTableColor(typeId: number) {
  switch (typeId) {
    case 1: return { color: '#3b82f6', bg: '#eff6ff', border: '#bfdbfe' }; // Pool
    case 2: return { color: '#9333ea', bg: '#faf5ff', border: '#e9d5ff' }; // VIP
    case 3: return { color: '#16a34a', bg: '#f0fdf4', border: '#bbf7d0' }; // Carom
    case 4: return { color: '#f97316', bg: '#fff7ed', border: '#fed7aa' }; // Snooker
    default: return { color: '#64748b', bg: '#f8fafc', border: '#e2e8f0' }; // Default
  }
}

function TableCard({ table }: { table: VenueTableLayoutItem }) {
  const { selectedTable, setSelectedTable, setIsDrawerOpen, triggerRefresh } = usePOS();
  
  const isInUse = !!table.activeSessionId;
  const isSelected = selectedTable?.tableId === table.tableId;
  const theme = getTableColor(table.tableTypeId);

  const now = Date.now();
  const nextBookingTime = table.nextBookingStartTimeUtc ? new Date(table.nextBookingStartTimeUtc + (table.nextBookingStartTimeUtc.endsWith('Z') ? '' : 'Z')).getTime() : null;
  const minsToNextBooking = nextBookingTime ? (nextBookingTime - now) / 60000 : null;
  
  // Block table if empty and next booking is <= 30 mins
  const isReserved = !isInUse && minsToNextBooking !== null && minsToNextBooking <= 30;
  
  // Warning if playing but booking is <= 30 mins
  const isPlayingButReservedSoon = isInUse && minsToNextBooking !== null && minsToNextBooking <= 30 && minsToNextBooking >= -60;

  const handleClick = () => {
    setSelectedTable(table);
    setIsDrawerOpen(true);
  };

  const handleStartSession = async (e: React.MouseEvent) => {
    e.stopPropagation(); // Prevent opening drawer when clicking quick start
    try {
      await sessionApi.start({ tableId: table.tableId });
      triggerRefresh();
    } catch (err) {
      console.error("Lỗi khi mở bàn", err);
      alert("Không thể mở bàn. Vui lòng thử lại.");
    }
  };

  if (isReserved) {
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
              {new Date(table.nextBookingStartTimeUtc! + (table.nextBookingStartTimeUtc!.endsWith('Z') ? '' : 'Z')).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
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
            style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.25rem', background: '#fef08a', color: '#854d0e', border: '1px solid #fde047', fontSize: '0.85rem' }}
          >
            Hủy
          </button>
          <button 
            onClick={async (e) => {
              e.stopPropagation();
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
            style={{ flex: 2, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem', background: '#eab308', color: 'white', border: 'none', fontSize: '0.85rem' }}
          >
            <Play size={14} fill="currentColor" /> Nhận bàn
          </button>
        </div>
      </div>
    );
  }

  if (!isInUse) {
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

  // IN_USE
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
        <span className={styles.timerBadge} style={{ background: theme.color, color: 'white' }}>
          --:--:--
        </span>
      </div>
      <div className={styles.tableCardFooter}>
        <div className={styles.billAmount} style={{ color: theme.color }}>
          Đang chơi
        </div>
      </div>
    </div>
  );
}

export function LiveTableMapColumn() {
  const [tables, setTables] = useState<VenueTableLayoutItem[]>([]);
  const [loading, setLoading] = useState(true);
  const { refreshTrigger } = usePOS();

  useEffect(() => {
    const fetchLayout = async () => {
      try {
        setLoading(true);
        const res = await venueApi.layout();
        let allTables: VenueTableLayoutItem[] = [];
        
        if (res.floors) {
          res.floors.forEach(f => {
            if (f.zones) {
              f.zones.forEach(z => {
                if (z.tables) {
                  allTables = [...allTables, ...z.tables];
                }
              });
            }
          });
        }
        setTables(allTables);
      } catch (err) {
        console.error("Failed to fetch venue layout", err);
      } finally {
        setLoading(false);
      }
    };

    fetchLayout();
  }, [refreshTrigger]);

  return (
    <div className={styles.mapColumn}>
      <div className={styles.mapHeader}>
        <h2 className={styles.mapTitle}>Sơ Đồ Bàn</h2>
        <div className={styles.legendList}>
          <div className={styles.legendItem}>
            <div className={`${styles.legendBox} ${styles.legendEmpty}`}></div> 
            <span>Trống</span>
          </div>
          <div className={styles.legendItem}>
            <div className={`${styles.legendBox} ${styles.legendActive}`}></div> 
            <span>Đang chơi</span>
          </div>
          <div className={styles.legendItem}>
            <div className={`${styles.legendBox} ${styles.legendReserved}`}></div> 
            <span>Đã giữ</span>
          </div>
        </div>
      </div>
      
      <div className={styles.gridArea}>
        {loading ? (
          <div className="text-center py-8 text-gray-500">Đang tải sơ đồ bàn...</div>
        ) : tables.length === 0 ? (
          <div className="text-center py-8 text-gray-500">Không tìm thấy bàn nào.</div>
        ) : (
          <div className={styles.tableGrid}>
            {tables.map(t => (
              <TableCard key={t.tableId} table={t} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
