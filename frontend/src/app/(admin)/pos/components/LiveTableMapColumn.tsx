"use client";

import React, { useEffect, useState } from 'react';
import { usePOS } from '../POSContext';
import { Play } from 'lucide-react';
import styles from '../pos.module.css';
import { venueApi } from '@/lib/api/endpoints';
import type { VenueTableLayoutItem, VenueLayoutResponse } from '@/types';

function formatDuration(ms: number) {
  const totalSeconds = Math.max(0, Math.floor(ms / 1000));
  const h = Math.floor(totalSeconds / 3600).toString().padStart(2, '0');
  const m = Math.floor((totalSeconds % 3600) / 60).toString().padStart(2, '0');
  const s = (totalSeconds % 60).toString().padStart(2, '0');
  return `${h}:${m}:${s}`;
}

function TableCard({ table }: { table: VenueTableLayoutItem }) {
  const { selectedTable, setSelectedTable, setIsDrawerOpen } = usePOS();
  
  // Fake a start time locally for demo of IN_USE since VenueTableLayoutItem doesn't include it.
  // We determine IN_USE if activeSessionId is present.
  const isInUse = !!table.activeSessionId;
  const isSelected = selectedTable?.tableId === table.tableId;

  const handleClick = () => {
    if (isInUse) {
      setSelectedTable(table);
      setIsDrawerOpen(true);
    }
  };

  if (!isInUse) {
    return (
      <div className={`${styles.tableCard} ${styles.tableEmpty}`}>
        <div className={styles.tableCardHeader}>
          <h3 className={styles.tableName}>{table.tableName}</h3>
          <span className={styles.emptyBadge}>TRỐNG</span>
        </div>
        <button className="primary-btn" style={{ width: '100%', marginTop: 'auto', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem' }}>
          <Play size={16} fill="currentColor" /> Mở bàn
        </button>
      </div>
    );
  }

  // IN_USE
  return (
    <div 
      onClick={handleClick}
      className={`${styles.tableCard} ${styles.tableActive} ${isSelected ? styles.tableSelected : ''}`}
    >
      <div className={styles.tableCardHeader}>
        <h3 className={styles.tableName}>{table.tableName}</h3>
        <span className={styles.timerBadge}>
          --:--:--
        </span>
      </div>
      <div className={styles.tableCardFooter}>
        <div className={styles.billAmount}>
          Đang chơi
        </div>
        <button className="primary-btn" style={{ padding: '0.25rem 0.75rem', background: 'transparent', color: '#16a34a', border: '1px solid #16a34a' }}>
          Chi tiết
        </button>
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
