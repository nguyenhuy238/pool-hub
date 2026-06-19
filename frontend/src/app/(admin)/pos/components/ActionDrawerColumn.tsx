"use client";

import React, { useEffect, useState } from 'react';
import { usePOS } from '../POSContext';
import { X, Receipt, Clock, ArrowRightLeft, Coffee } from 'lucide-react';
import styles from '../pos.module.css';
import { sessionApi } from '@/lib/api/endpoints';
import type { Session, SessionTableAssignment } from '@/types';

function formatDuration(ms: number) {
  const totalSeconds = Math.max(0, Math.floor(ms / 1000));
  const h = Math.floor(totalSeconds / 3600).toString().padStart(2, '0');
  const m = Math.floor((totalSeconds % 3600) / 60).toString().padStart(2, '0');
  const s = (totalSeconds % 60).toString().padStart(2, '0');
  return `${h}:${m}:${s}`;
}

export function ActionDrawerColumn() {
  const { selectedTable, isDrawerOpen, setIsDrawerOpen, setSelectedTable } = usePOS();
  const [sessionData, setSessionData] = useState<Session | null>(null);
  const [loading, setLoading] = useState(false);
  const [elapsed, setElapsed] = useState(0);

  useEffect(() => {
    const fetchSession = async () => {
      if (!selectedTable?.activeSessionId) {
        setSessionData(null);
        return;
      }
      try {
        setLoading(true);
        const res = await sessionApi.detail(selectedTable.activeSessionId);
        setSessionData(res);
      } catch (err) {
        console.error("Failed to fetch session detail", err);
        setSessionData(null);
      } finally {
        setLoading(false);
      }
    };

    if (isDrawerOpen && selectedTable?.activeSessionId) {
      fetchSession();
    }
  }, [selectedTable, isDrawerOpen]);

  // Timer effect
  useEffect(() => {
    if (!sessionData?.startedAtUtc) return;
    
    const startMs = new Date(sessionData.startedAtUtc).getTime();
    setElapsed(Date.now() - startMs);

    const interval = setInterval(() => {
      setElapsed(Date.now() - new Date(sessionData.startedAtUtc).getTime());
    }, 1000);
    
    return () => clearInterval(interval);
  }, [sessionData?.startedAtUtc]);

  if (!isDrawerOpen || !selectedTable) {
    return (
      <div className={styles.emptyDrawer}>
        <div className={styles.emptyBox}>
          <Receipt size={32} />
        </div>
        <h3 className={styles.emptyTitle}>Chưa chọn bàn</h3>
        <p className={styles.emptyDesc}>
          Chọn một bàn đang hoạt động trên sơ đồ để xem chi tiết Bill và thực hiện thanh toán.
        </p>
      </div>
    );
  }

  const closeDrawer = () => {
    setIsDrawerOpen(false);
    setSelectedTable(null);
  };

  // Safe cast for assignment amount since types might not fully cover it.
  const assignment = sessionData?.assignments?.find(a => a.tableId === selectedTable.tableId);
  const timeAmount = assignment?.amount || 0;
  
  // Fake F&B total for now if order is not attached to Session type.
  // In real implementation, we'd fetch orderApi.bySession(sessionId)
  const fbAmount = 0; 
  const deposit = 0; // If booking is attached, could be deposit
  
  const finalTotal = timeAmount + fbAmount - deposit;

  return (
    <div className={styles.drawerColumn}>
      <div className={styles.drawerHeader}>
        <div>
          <h2 className={styles.drawerHeaderTitle}>{selectedTable.tableName}</h2>
          <div className={styles.drawerTimer}>
            {loading ? "Đang tải..." : formatDuration(elapsed)}
          </div>
        </div>
        <button onClick={closeDrawer} className={styles.closeBtn}>
          <X size={20} strokeWidth={2.5} />
        </button>
      </div>

      <div className={styles.drawerBody}>
        
        {/* Quick F&B Grid */}
        <div>
          <h3 className={styles.sectionTitle}>
            <Coffee size={16} /> Menu Gọi Nhanh
          </h3>
          <div className={styles.fbGrid}>
            <button className="primary-btn" style={{ padding: '0.75rem', background: '#f8fafc', color: '#0f172a', border: '1px solid #cbd5e1' }}>Bia Tiger</button>
            <button className="primary-btn" style={{ padding: '0.75rem', background: '#f8fafc', color: '#0f172a', border: '1px solid #cbd5e1' }}>Redbull</button>
            <button className="primary-btn" style={{ padding: '0.75rem', background: '#f8fafc', color: '#0f172a', border: '1px solid #cbd5e1' }}>Nước Suối</button>
            <button className="primary-btn" style={{ padding: '0.75rem', background: '#f8fafc', color: '#0f172a', border: '1px solid #cbd5e1' }}>Thuốc Lá</button>
          </div>
        </div>

        {/* Bill Summary */}
        <div>
          <h3 className={styles.sectionTitle}>
            <Receipt size={16} /> Hóa Đơn Tạm Tính
          </h3>
          <div className={styles.billBox}>
            {loading ? (
              <div className="text-center py-4 text-sm text-gray-500">Đang tải hóa đơn...</div>
            ) : (
              <>
                <div className={styles.billRow}>
                  <span>Tiền giờ ({formatDuration(elapsed).substring(0, 5)})</span>
                  <span>{timeAmount.toLocaleString()}Đ</span>
                </div>
                {/* 
                <div className={styles.billRow}>
                  <span>Đồ uống (F&B)</span>
                  <span>{fbAmount.toLocaleString()}Đ</span>
                </div> 
                */}
                <div className={styles.billLine}></div>
                <div className={styles.billTotal}>
                  <span>CẦN THU</span>
                  <span>{finalTotal.toLocaleString()}Đ</span>
                </div>
              </>
            )}
          </div>
        </div>

      </div>

      <div className={styles.drawerFooter}>
        <button className="primary-btn" style={{ width: '100%', padding: '0.75rem', background: 'white', color: '#0f172a', border: '1px solid #cbd5e1', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem' }}>
          <ArrowRightLeft size={16} /> Chuyển Bàn
        </button>

        <button className="primary-btn" style={{ width: '100%', padding: '0.75rem', background: '#eff6ff', color: '#2563eb', border: '1px solid #bfdbfe', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem' }}>
          <Clock size={16} /> Gia Hạn Thời Gian
        </button>

        <button className="primary-btn" style={{ width: '100%', padding: '1rem', background: '#22c55e', color: 'white', border: 'none', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem', fontSize: '1.1rem' }}>
          <Receipt size={20} /> Thanh Toán
        </button>
      </div>

    </div>
  );
}
