"use client";

import React, { useEffect, useState } from 'react';
import { Modal, useDebouncedValue } from '@/components/ui';
import { User, Search, Calendar, X, Play, Check } from 'lucide-react';
import { sessionApi, bookingApi, customerApi } from '@/lib/api/endpoints';
import type { VenueTableLayoutItem, CustomerDto, Booking } from '@/types';
import { formatVietnamTime, getVietnamDateInputValue, utcTimestampMs } from '@/lib/dateTime';

interface StartSessionModalProps {
  table: VenueTableLayoutItem;
  onClose: () => void;
  onStarted: () => void;
}

type Tab = 'walkin' | 'booking';

export function StartSessionModal({ table, onClose, onStarted }: StartSessionModalProps) {
  const [tab, setTab] = useState<Tab>('walkin');
  const [submitting, setSubmitting] = useState(false);

  // --- Walk-in: tìm & gắn khách (tùy chọn) ---
  const [query, setQuery] = useState('');
  const debouncedQuery = useDebouncedValue(query, 350);
  const [results, setResults] = useState<CustomerDto[]>([]);
  const [searching, setSearching] = useState(false);
  const [selectedCustomer, setSelectedCustomer] = useState<CustomerDto | null>(null);

  useEffect(() => {
    const q = debouncedQuery.trim();
    if (!q || selectedCustomer) {
      setResults([]);
      return;
    }
    let cancelled = false;
    setSearching(true);
    customerApi.list({ search: q, status: true, pageNumber: 1, pageSize: 8 })
      .then((res) => {
        if (cancelled) return;
        const items = Array.isArray(res) ? res : (res.items || []);
        setResults(items as CustomerDto[]);
      })
      .catch((err) => console.error('Failed to search customers', err))
      .finally(() => { if (!cancelled) setSearching(false); });
    return () => { cancelled = true; };
  }, [debouncedQuery, selectedCustomer]);

  const handleStartWalkin = async () => {
    try {
      setSubmitting(true);
      await sessionApi.start({ tableId: table.tableId, customerId: selectedCustomer?.customerId });
      onStarted();
    } catch (err) {
      console.error('Lỗi khi mở bàn', err);
      alert('Không thể mở bàn. Vui lòng thử lại.');
    } finally {
      setSubmitting(false);
    }
  };

  // --- Booking: lịch đặt hôm nay cho bàn này (hoặc chưa xếp bàn) ---
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [loadingBookings, setLoadingBookings] = useState(false);
  const [selectedBookingId, setSelectedBookingId] = useState<number | null>(null);

  useEffect(() => {
    if (tab !== 'booking') return;
    let cancelled = false;
    setLoadingBookings(true);
    bookingApi.list({ Status: 2 })
      .then((res) => {
        if (cancelled) return;
        const items: Booking[] = Array.isArray(res) ? res : ((res as { items?: Booking[] }).items || []);
        const today = getVietnamDateInputValue();
        const relevant = items
          .filter((b) => !b.hasSession)
          .filter((b) => b.tableId === table.tableId || b.tableId == null)
          .filter((b) => getVietnamDateInputValue(new Date(utcTimestampMs(b.startTimeUtc))) === today)
          .sort((a, b) => utcTimestampMs(a.startTimeUtc) - utcTimestampMs(b.startTimeUtc));
        setBookings(relevant);
      })
      .catch((err) => console.error('Failed to load bookings', err))
      .finally(() => { if (!cancelled) setLoadingBookings(false); });
    return () => { cancelled = true; };
  }, [tab, table.tableId]);

  const handleStartFromBooking = async () => {
    if (selectedBookingId == null) return;
    try {
      setSubmitting(true);
      // Q3: dùng bookingApi.startSession (xử lý trừ cọc + đổi trạng thái booking).
      await bookingApi.startSession(selectedBookingId, table.tableId);
      onStarted();
    } catch (err) {
      console.error('Lỗi khi nhận bàn', err);
      alert('Không thể nhận bàn. Vui lòng thử lại.');
    } finally {
      setSubmitting(false);
    }
  };

  const tabBtnStyle = (active: boolean): React.CSSProperties => ({
    flex: 1,
    padding: '10px',
    border: 'none',
    borderBottom: active ? '2px solid #2563eb' : '2px solid transparent',
    background: 'transparent',
    color: active ? '#2563eb' : '#64748b',
    fontWeight: 600,
    cursor: 'pointer',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    gap: '0.4rem',
  });

  return (
    <Modal title={`Mở Bàn — ${table.tableName}`} onClose={onClose} size="medium">
      <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
        {/* Tabs */}
        <div style={{ display: 'flex', borderBottom: '1px solid #e2e8f0' }}>
          <button style={tabBtnStyle(tab === 'walkin')} onClick={() => setTab('walkin')}>
            <User size={16} /> Khách vãng lai
          </button>
          <button style={tabBtnStyle(tab === 'booking')} onClick={() => setTab('booking')}>
            <Calendar size={16} /> Từ booking
          </button>
        </div>

        {tab === 'walkin' ? (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
            {selectedCustomer ? (
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '12px', background: '#eff6ff', border: '1px solid #bfdbfe', borderRadius: '8px' }}>
                <div>
                  <div style={{ fontWeight: 600 }}>{selectedCustomer.fullName}</div>
                  <div style={{ fontSize: '0.85rem', color: '#64748b' }}>
                    {selectedCustomer.phoneNumber || 'Không có SĐT'}
                    {typeof selectedCustomer.loyaltyPoints === 'number' && ` · ${selectedCustomer.loyaltyPoints.toLocaleString()} điểm`}
                  </div>
                </div>
                <button
                  onClick={() => { setSelectedCustomer(null); setQuery(''); }}
                  style={{ background: 'transparent', border: 'none', cursor: 'pointer', color: '#64748b', display: 'flex' }}
                  title="Bỏ chọn khách"
                >
                  <X size={18} />
                </button>
              </div>
            ) : (
              <div style={{ position: 'relative' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px', border: '1px solid #cbd5e1', borderRadius: '8px', padding: '0 10px' }}>
                  <Search size={16} color="#94a3b8" />
                  <input
                    value={query}
                    onChange={(e) => setQuery(e.target.value)}
                    placeholder="Tìm khách theo tên / SĐT (tùy chọn)"
                    style={{ flex: 1, border: 'none', outline: 'none', padding: '10px 0', fontSize: '0.95rem', background: 'transparent' }}
                  />
                  {searching && <span style={{ fontSize: '0.75rem', color: '#94a3b8' }}>...</span>}
                </div>
                {results.length > 0 && (
                  <div style={{ marginTop: '6px', border: '1px solid #e2e8f0', borderRadius: '8px', maxHeight: '200px', overflowY: 'auto', background: 'white' }}>
                    {results.map((c) => (
                      <button
                        key={c.customerId}
                        onClick={() => { setSelectedCustomer(c); setResults([]); }}
                        style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-start', width: '100%', textAlign: 'left', padding: '10px 12px', border: 'none', borderBottom: '1px solid #f1f5f9', background: 'white', cursor: 'pointer' }}
                      >
                        <span style={{ fontWeight: 500 }}>{c.fullName}</span>
                        <span style={{ fontSize: '0.8rem', color: '#64748b' }}>{c.phoneNumber || 'Không có SĐT'}</span>
                      </button>
                    ))}
                  </div>
                )}
                {debouncedQuery.trim() && !searching && results.length === 0 && (
                  <div style={{ marginTop: '6px', fontSize: '0.85rem', color: '#94a3b8' }}>Không tìm thấy khách phù hợp.</div>
                )}
              </div>
            )}

            <p style={{ fontSize: '0.8rem', color: '#94a3b8', margin: 0 }}>
              Không chọn khách → mở bàn cho <strong>khách vãng lai</strong>.
            </p>

            <button
              onClick={handleStartWalkin}
              disabled={submitting}
              style={{ padding: '12px', background: '#3b82f6', color: 'white', border: 'none', borderRadius: '8px', fontWeight: 600, cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem', opacity: submitting ? 0.7 : 1, fontSize: '1rem' }}
            >
              <Play size={18} fill="currentColor" /> {submitting ? 'Đang mở bàn...' : 'Mở Bàn Ngay'}
            </button>
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
            {loadingBookings ? (
              <div className="text-center py-6 text-gray-500">Đang tải lịch đặt...</div>
            ) : bookings.length === 0 ? (
              <div style={{ textAlign: 'center', padding: '24px 12px', color: '#64748b' }}>
                Bàn này chưa có lịch đặt phù hợp hôm nay.<br />
                <span style={{ fontSize: '0.85rem', color: '#94a3b8' }}>Hãy dùng tab &quot;Khách vãng lai&quot;.</span>
              </div>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', maxHeight: '280px', overflowY: 'auto' }}>
                {bookings.map((b) => {
                  const selected = selectedBookingId === b.bookingId;
                  return (
                    <button
                      key={b.bookingId}
                      onClick={() => setSelectedBookingId(b.bookingId)}
                      style={{ textAlign: 'left', padding: '12px', border: `1px solid ${selected ? '#2563eb' : '#e2e8f0'}`, borderRadius: '8px', background: selected ? '#eff6ff' : 'white', cursor: 'pointer', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}
                    >
                      <div>
                        <div style={{ fontWeight: 600, display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                          {b.customerName || 'Khách vãng lai'}
                          {selected && <Check size={16} color="#2563eb" />}
                        </div>
                        <div style={{ fontSize: '0.85rem', color: '#64748b' }}>
                          {formatVietnamTime(b.startTimeUtc)} · {b.numberOfGuests || 1} khách
                          {b.tableId == null && ' · Chưa xếp bàn'}
                        </div>
                      </div>
                      <span style={{ fontSize: '0.8rem', color: '#94a3b8' }}>{b.bookingCode || `#${b.bookingId}`}</span>
                    </button>
                  );
                })}
              </div>
            )}

            {bookings.length > 0 && (
              <button
                onClick={handleStartFromBooking}
                disabled={submitting || selectedBookingId == null}
                style={{ padding: '12px', background: selectedBookingId == null ? '#e2e8f0' : '#16a34a', color: selectedBookingId == null ? '#94a3b8' : 'white', border: 'none', borderRadius: '8px', fontWeight: 600, cursor: selectedBookingId == null ? 'not-allowed' : 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem', opacity: submitting ? 0.7 : 1, fontSize: '1rem' }}
              >
                <Play size={18} fill="currentColor" /> {submitting ? 'Đang nhận bàn...' : 'Nhận Bàn'}
              </button>
            )}
          </div>
        )}
      </div>
    </Modal>
  );
}
