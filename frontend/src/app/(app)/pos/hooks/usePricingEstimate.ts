import { useEffect, useState } from 'react';
import { pricingApi } from '@/lib/api/endpoints';
import type { PricingPlanRule } from '@/types';

/**
 * Nạp bảng đơn giá TẠM TÍNH theo `tableTypeId` (lấy rule active đầu tiên cho mỗi loại bàn).
 * Chỉ dùng để hiển thị nhanh tiền giờ trên thẻ bàn — con số chính xác vẫn lấy từ
 * `sessionApi.summary` khi mở drawer / thanh toán. (Xem quyết định Q2 trong POS_IMPROVEMENT_PLAN.md.)
 */
export function usePricingEstimate() {
  const [rateByType, setRateByType] = useState<Map<number, number>>(new Map());

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const res = await pricingApi.rules({ isActive: true });
        const items: PricingPlanRule[] = Array.isArray(res) ? res : ((res as { items?: PricingPlanRule[] }).items || []);
        const map = new Map<number, number>();
        for (const r of items) {
          if (r.isActive === false) continue;
          if (!map.has(r.tableTypeId) && typeof r.hourlyRate === 'number') {
            map.set(r.tableTypeId, r.hourlyRate);
          }
        }
        if (!cancelled) setRateByType(map);
      } catch (err) {
        console.error('Failed to load pricing rules for estimate', err);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  return rateByType;
}

/**
 * Ước tính tiền giờ = đơn giá/giờ × số giờ đã chơi. Trả về `null` nếu chưa biết đơn giá
 * (để UI hiển thị "Đang chơi" thay vì một con số sai).
 */
export function estimateTimeAmount(
  rateByType: Map<number, number>,
  tableTypeId: number,
  elapsedMs: number
): number | null {
  const hourly = rateByType.get(tableTypeId);
  if (!hourly) return null;
  const hours = Math.max(0, elapsedMs) / 3_600_000;
  return Math.round(hourly * hours);
}
