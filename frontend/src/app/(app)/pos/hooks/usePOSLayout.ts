import { useEffect, useState } from 'react';
import { venueApi } from '@/lib/api/endpoints';
import type { VenueTableLayoutItem, VenueLayoutResponse } from '@/types';

/** Làm phẳng cây layout (floors -> zones -> tables) thành mảng bàn. */
export function flattenLayout(res: VenueLayoutResponse): VenueTableLayoutItem[] {
  const tables: VenueTableLayoutItem[] = [];
  res.floors?.forEach((f) => f.zones?.forEach((z) => z.tables?.forEach((t) => tables.push(t))));
  return tables;
}

/**
 * Tải sơ đồ bàn từ `venueApi.layout()` và làm phẳng thành danh sách bàn.
 * Tự nạp lại mỗi khi `refreshTrigger` thay đổi (kể cả khi có sự kiện SignalR).
 */
export function usePOSLayout(refreshTrigger: number) {
  const [tables, setTables] = useState<VenueTableLayoutItem[]>([]);
  const [meta, setMeta] = useState<VenueLayoutResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        setLoading(true);
        const res = await venueApi.layout();
        if (cancelled) return;
        setTables(flattenLayout(res));
        setMeta(res);
        setError(null);
      } catch (err) {
        if (cancelled) return;
        console.error('Failed to fetch venue layout', err);
        setError('Không tải được sơ đồ bàn.');
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [refreshTrigger]);

  return { tables, meta, loading, error };
}
