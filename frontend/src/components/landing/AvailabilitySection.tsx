"use client";

import { useEffect, useMemo, useState } from "react";
import { availabilityApi, type LandingAvailability } from "@/lib/api/availabilityApi";
import type { VenueTableLayoutItem } from "@/types";

const statusMap: Record<number, { label: string; className: string }> = {
  1: { label: "Trống", className: "available" },
  2: { label: "Đang chơi", className: "in-use" },
  3: { label: "Đã đặt", className: "reserved" },
  4: { label: "Bảo trì", className: "maintenance" }
};

export function AvailabilitySection() {
  const [data, setData] = useState<LandingAvailability | null>(null);
  const [loading, setLoading] = useState(true);
  const [filters, setFilters] = useState({ date: new Date().toISOString().slice(0, 10), time: "19:00", tableTypeId: "", guests: 4 });

  useEffect(() => {
    availabilityApi.getAvailability().then(setData).finally(() => setLoading(false));
  }, []);

  const tables = useMemo(() => {
    const items: VenueTableLayoutItem[] = [];
    data?.layout.floors.forEach((floor) => floor.zones.forEach((zone) => items.push(...zone.tables)));
    return filters.tableTypeId ? items.filter((item) => item.tableTypeId === Number(filters.tableTypeId)) : items;
  }, [data, filters.tableTypeId]);

  return (
    <section className="landing-section muted-band" id="availability">
      <div className="section-heading split-heading">
        <div>
          <p className="eyebrow">Kiểm tra bàn trống</p>
          <h2>Xem nhanh trạng thái bàn trước khi gửi yêu cầu</h2>
        </div>
        <a className="primary-btn" href="#booking">Đặt bàn</a>
      </div>
      <div className="availability-shell">
        <div className="availability-filters">
          <label><span>Ngày</span><input type="date" value={filters.date} min={new Date().toISOString().slice(0, 10)} onChange={(e) => setFilters({ ...filters, date: e.target.value })} /></label>
          <label><span>Giờ</span><input type="time" value={filters.time} onChange={(e) => setFilters({ ...filters, time: e.target.value })} /></label>
          <label><span>Loại bàn</span><select value={filters.tableTypeId} onChange={(e) => setFilters({ ...filters, tableTypeId: e.target.value })}><option value="">Tất cả</option>{data?.tableTypes.map((item) => <option key={item.tableTypeId} value={item.tableTypeId}>{item.name}</option>)}</select></label>
          <label><span>Số người</span><input type="number" min={1} max={20} value={filters.guests} onChange={(e) => setFilters({ ...filters, guests: Number(e.target.value) })} /></label>
        </div>
        {loading ? <div className="state-card">Đang tải sơ đồ bàn...</div> : null}
        {!loading && !tables.length ? <div className="state-card">Không có bàn phù hợp với bộ lọc.</div> : null}
        <div className="availability-grid">
          {tables.map((table) => {
            const status = statusMap[table.operationalStatus] || statusMap[1];
            return (
              <article className={`table-tile ${status.className}`} key={table.tableId}>
                <strong>{table.tableName}</strong>
                <span>{table.tableTypeName} - {table.capacity} người</span>
                <em>{status.label}</em>
              </article>
            );
          })}
        </div>
      </div>
      {data?.usingMock ? <p className="data-note">Đang dùng sơ đồ mẫu. Backend nên bổ sung GET /api/bookings/availability?date=&time=&tableTypeId= để tính bàn trống theo khung giờ.</p> : null}
    </section>
  );
}
