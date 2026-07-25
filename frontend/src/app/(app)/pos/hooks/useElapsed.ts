import { useEffect, useState } from 'react';
import { utcTimestampMs } from '@/lib/dateTime';

/** Định dạng khoảng thời gian mili-giây -> "HH:MM:SS". */
export function formatDuration(ms: number) {
  const totalSeconds = Math.max(0, Math.floor(ms / 1000));
  const h = Math.floor(totalSeconds / 3600).toString().padStart(2, '0');
  const m = Math.floor((totalSeconds % 3600) / 60).toString().padStart(2, '0');
  const s = (totalSeconds % 60).toString().padStart(2, '0');
  return `${h}:${m}:${s}`;
}

/**
 * Đếm thời gian trôi qua (ms) tính từ một mốc UTC.
 * Khi `active` = false, đồng hồ dừng và giữ nguyên giá trị cuối (dùng cho lúc kết thúc phiên).
 */
export function useElapsed(startUtc?: string | null, active = true) {
  const [elapsed, setElapsed] = useState(0);

  useEffect(() => {
    if (!startUtc || !active) return;
    const startMs = utcTimestampMs(startUtc);
    if (Number.isNaN(startMs)) return;

    const tick = () => setElapsed(Date.now() - startMs);
    tick(); // cập nhật ngay lần đầu
    const iv = setInterval(tick, 1000);
    return () => clearInterval(iv);
  }, [startUtc, active]);

  return elapsed;
}
