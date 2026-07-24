"use client";

import React, { useMemo, useState } from 'react';
import { usePOS } from '../POSContext';
import styles from '../pos.module.css';
import { TableCard } from './TableCard';
import { getPOSTableStatus, type POSTableStatus } from '../lib/tableStatus';

type FilterKey = 'all' | 'empty' | 'active' | 'reserved';

const FILTERS: { key: FilterKey; label: string; match?: POSTableStatus }[] = [
  { key: 'all', label: 'Tất cả' },
  { key: 'empty', label: 'Trống', match: 'empty' },
  { key: 'active', label: 'Đang chơi', match: 'active' },
  { key: 'reserved', label: 'Đã giữ', match: 'reserved' },
];

export function LiveTableMapColumn() {
  const { tables, layoutLoading, layoutError } = usePOS();
  const [filter, setFilter] = useState<FilterKey>('all');

  // Đếm số bàn theo trạng thái để hiển thị trên chip lọc.
  const counts = useMemo(() => {
    const c: Record<POSTableStatus, number> = { empty: 0, active: 0, reserved: 0, maintenance: 0 };
    tables.forEach((t) => { c[getPOSTableStatus(t)] += 1; });
    return c;
  }, [tables]);

  const filtered = useMemo(() => {
    if (filter === 'all') return tables;
    const match = FILTERS.find((f) => f.key === filter)?.match;
    return tables.filter((t) => getPOSTableStatus(t) === match);
  }, [tables, filter]);

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

      <div className={styles.filterBar}>
        {FILTERS.map((f) => {
          const count = f.key === 'all' ? tables.length : counts[f.match!];
          const active = filter === f.key;
          return (
            <button
              key={f.key}
              onClick={() => setFilter(f.key)}
              className={`${styles.filterChip} ${active ? styles.filterChipActive : ''}`}
            >
              {f.label} <span className={styles.filterCount}>{count}</span>
            </button>
          );
        })}
      </div>

      <div className={styles.gridArea}>
        {layoutLoading && tables.length === 0 ? (
          <div className="text-center py-8 text-gray-500">Đang tải sơ đồ bàn...</div>
        ) : layoutError ? (
          <div className="text-center py-8 text-red-500">{layoutError}</div>
        ) : filtered.length === 0 ? (
          <div className="text-center py-8 text-gray-500">Không có bàn nào phù hợp bộ lọc.</div>
        ) : (
          <div className={styles.tableGrid}>
            {filtered.map((t) => (
              <TableCard key={t.tableId} table={t} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
