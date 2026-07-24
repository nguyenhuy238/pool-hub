import type { VenueTableLayoutItem } from '@/types';
import { utcTimestampMs } from '@/lib/dateTime';

/** Trạng thái hiển thị của một bàn trên màn hình POS. */
export type POSTableStatus = 'empty' | 'active' | 'reserved' | 'maintenance';

/**
 * Phân loại trạng thái bàn dùng CHUNG cho cả thanh lọc và thẻ bàn, để hai nơi không lệch nhau.
 * - `maintenance`: operationalStatus 4 (Bảo trì) hoặc 5 (Ngừng hoạt động).
 * - `active`: đang có phiên chơi.
 * - `reserved`: đang trống nhưng có booking sắp tới trong <= 30 phút.
 * - `empty`: còn lại.
 */
export function getPOSTableStatus(table: VenueTableLayoutItem, now = Date.now()): POSTableStatus {
  if (table.operationalStatus === 4 || table.operationalStatus === 5) return 'maintenance';
  if (table.activeSessionId) return 'active';

  const nextMs = table.nextBookingStartTimeUtc ? utcTimestampMs(table.nextBookingStartTimeUtc) : NaN;
  const minsToNext = Number.isNaN(nextMs) ? null : (nextMs - now) / 60000;
  if (minsToNext !== null && minsToNext <= 30) return 'reserved';

  return 'empty';
}
