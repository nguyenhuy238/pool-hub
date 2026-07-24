"use client";

import React, { useMemo, useState } from 'react';
import { Modal } from '@/components/ui';
import { ArrowRightLeft, Check } from 'lucide-react';
import { sessionApi } from '@/lib/api/endpoints';
import type { Session, VenueTableLayoutItem } from '@/types';
import { usePOS } from '../POSContext';
import { getPOSTableStatus } from '../lib/tableStatus';

interface TransferTableModalProps {
  session: Session;
  fromTable: VenueTableLayoutItem;
  onClose: () => void;
  onTransferred: () => void;
}

const REASONS: { value: string; label: string }[] = [
  { value: 'CustomerRequest', label: 'Khách yêu cầu' },
  { value: 'TableIssue', label: 'Bàn có sự cố' },
  { value: 'StaffCorrection', label: 'Nhân viên điều chỉnh' },
  { value: 'Other', label: 'Khác' },
];

/** Tìm assignmentId của bàn nguồn trong phiên (nhiều nguồn dữ liệu -> lấy cái nào có). */
function resolveSourceAssignmentId(session: Session, tableId: number): number | null {
  const active = (session.activeAssignments || []).find((a) => Number(a.tableId) === tableId);
  if (active?.assignmentId) return Number(active.assignmentId);
  if (session.currentTable && Number(session.currentTable.tableId) === tableId && session.currentTable.assignmentId) {
    return Number(session.currentTable.assignmentId);
  }
  const assign = (session.assignments || []).find((a) => Number(a.tableId) === tableId);
  const id = assign?.sessionTableAssignmentId || assign?.assignmentId;
  return id ? Number(id) : null;
}

export function TransferTableModal({ session, fromTable, onClose, onTransferred }: TransferTableModalProps) {
  const { tables } = usePOS();

  const sourceAssignmentId = useMemo(
    () => resolveSourceAssignmentId(session, fromTable.tableId),
    [session, fromTable.tableId]
  );

  // Bàn đích hợp lệ: đang trống thực sự (available, không phiên, không bảo trì, không sắp có khách đặt).
  const candidates = useMemo(
    () => tables.filter(
      (t) => t.tableId !== fromTable.tableId && t.operationalStatus === 1 && !t.activeSessionId && getPOSTableStatus(t) === 'empty'
    ),
    [tables, fromTable.tableId]
  );

  const [targetId, setTargetId] = useState<number | null>(null);
  const [reason, setReason] = useState('CustomerRequest');
  const [note, setNote] = useState('');
  const [markOldTableMaintenance, setMarkOldTableMaintenance] = useState(false);
  const [saving, setSaving] = useState(false);

  const handleTransfer = async () => {
    if (sourceAssignmentId == null) return;
    if (targetId == null) {
      alert('Vui lòng chọn bàn chuyển đến.');
      return;
    }
    try {
      setSaving(true);
      await sessionApi.transfer(session.sessionId, {
        sourceAssignmentId,
        toTableId: targetId,
        reason,
        note: note.trim() || undefined,
        markOldTableMaintenance,
      });
      onTransferred();
    } catch (err) {
      console.error('Không thể chuyển bàn', err);
      alert(err instanceof Error ? err.message : 'Không thể chuyển bàn. Vui lòng thử lại.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal title={`Chuyển bàn — ${fromTable.tableName}`} onClose={onClose} size="medium">
      <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
        {sourceAssignmentId == null ? (
          <div style={{ padding: '16px', background: '#fef2f2', border: '1px solid #fecaca', borderRadius: '8px', color: '#b91c1c', fontSize: '0.9rem' }}>
            Không xác định được bàn nguồn trong phiên này. Vui lòng tải lại và thử lại.
          </div>
        ) : (
          <>
            <div>
              <label style={{ fontWeight: 600, fontSize: '0.9rem' }}>Chuyển đến bàn trống</label>
              {candidates.length === 0 ? (
                <div style={{ marginTop: '8px', padding: '16px', textAlign: 'center', color: '#64748b', background: '#f8fafc', border: '1px solid #e2e8f0', borderRadius: '8px' }}>
                  Hiện không có bàn trống phù hợp để chuyển.
                </div>
              ) : (
                <div style={{ marginTop: '8px', display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(120px, 1fr))', gap: '8px', maxHeight: '220px', overflowY: 'auto' }}>
                  {candidates.map((t) => {
                    const selected = targetId === t.tableId;
                    return (
                      <button
                        key={t.tableId}
                        onClick={() => setTargetId(t.tableId)}
                        style={{ padding: '10px', border: `1px solid ${selected ? '#2563eb' : '#e2e8f0'}`, borderRadius: '8px', background: selected ? '#eff6ff' : 'white', cursor: 'pointer', display: 'flex', flexDirection: 'column', alignItems: 'flex-start', gap: '2px' }}
                      >
                        <span style={{ fontWeight: 600, display: 'flex', alignItems: 'center', gap: '0.3rem' }}>
                          {t.tableName} {selected && <Check size={14} color="#2563eb" />}
                        </span>
                        <span style={{ fontSize: '0.75rem', color: '#94a3b8' }}>{t.tableTypeName}</span>
                      </button>
                    );
                  })}
                </div>
              )}
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
              <label style={{ fontWeight: 600, fontSize: '0.9rem' }}>Lý do</label>
              <select
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                style={{ padding: '10px', borderRadius: '6px', border: '1px solid #cbd5e1', fontSize: '0.95rem', outline: 'none' }}
              >
                {REASONS.map((r) => (
                  <option key={r.value} value={r.value}>{r.label}</option>
                ))}
              </select>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
              <label style={{ fontWeight: 600, fontSize: '0.9rem' }}>Ghi chú (tùy chọn)</label>
              <textarea
                value={note}
                onChange={(e) => setNote(e.target.value)}
                rows={2}
                placeholder="Ghi chú thêm nếu có"
                style={{ padding: '10px', borderRadius: '6px', border: '1px solid #cbd5e1', fontSize: '0.95rem', outline: 'none', resize: 'vertical', fontFamily: 'inherit' }}
              />
            </div>

            <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '0.9rem', cursor: 'pointer' }}>
              <input
                type="checkbox"
                checked={markOldTableMaintenance}
                onChange={(e) => setMarkOldTableMaintenance(e.target.checked)}
                style={{ width: 18, height: 18, cursor: 'pointer', accentColor: '#2563eb' }}
              />
              <span>Đánh dấu bàn cũ cần bảo trì</span>
            </label>

            <div style={{ display: 'flex', gap: '12px', marginTop: '8px' }}>
              <button onClick={onClose} disabled={saving} style={{ flex: 1, padding: '12px', background: '#e2e8f0', color: '#475569', border: 'none', borderRadius: '6px', fontWeight: 600, cursor: 'pointer' }}>Hủy</button>
              <button
                onClick={handleTransfer}
                disabled={saving || targetId == null}
                style={{ flex: 2, padding: '12px', background: targetId == null ? '#e2e8f0' : '#2563eb', color: targetId == null ? '#94a3b8' : 'white', border: 'none', borderRadius: '6px', fontWeight: 600, cursor: targetId == null ? 'not-allowed' : 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem', opacity: saving ? 0.7 : 1 }}
              >
                <ArrowRightLeft size={18} /> {saving ? 'Đang chuyển...' : 'Chuyển Bàn'}
              </button>
            </div>
          </>
        )}
      </div>
    </Modal>
  );
}
