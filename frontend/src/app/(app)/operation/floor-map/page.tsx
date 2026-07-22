"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { ApiError } from "@/lib/api/client";
import { invoiceApi, sessionApi, venueApi } from "@/lib/api/endpoints";
import { dateTime, label, tableStatus } from "@/lib/status";
import { Badge, ConfirmDialog, Modal, PageHeader, SearchFilterBar, StateBlock, useLoad } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { VenueFloorLayoutItem, VenueTableLayoutItem, VenueZoneLayoutItem } from "@/types";
import "./floor-map.css";

type SelectedTable = VenueTableLayoutItem & {
  floorId: number;
  floorName: string;
  zoneId: number;
  zoneName: string;
};

const statusOptions = [
  { value: "", label: "Tất cả trạng thái" },
  { value: "1", label: "Sẵn sàng" },
  { value: "2", label: "Đang có khách" },
  { value: "3", label: "Đã đặt trước" },
  { value: "4", label: "Bảo trì" },
  { value: "5", label: "Ngừng hoạt động" }
];

const statusTone = (status: number): "green" | "blue" | "yellow" | "red" | "neutral" => {
  if (status === 1) return "green";
  if (status === 2) return "blue";
  if (status === 3) return "yellow";
  if (status === 4) return "red";
  return "neutral";
};

export default function FloorMapPage() {
  const router = useRouter();
  const toast = useToast();
  const [selected, setSelected] = useState<SelectedTable | null>(null);
  const [endingSessionId, setEndingSessionId] = useState<number | null>(null);
  const [activeFloorId, setActiveFloorId] = useState<number | null>(null);
  const [filters, setFilters] = useState({ zoneId: "", status: "", tableTypeId: "", search: "" });
  const [lastUpdated, setLastUpdated] = useState<string | null>(null);

  const { data: layout, loading, error, reload } = useLoad(async () => {
    try {
      const response = await venueApi.layout();
      setLastUpdated(new Date().toISOString());
      return response;
    } catch (err) {
      throw new Error(getFloorMapErrorMessage(err));
    }
  }, []);

  const floors = useMemo(() => layout?.floors ?? [], [layout?.floors]);

  useEffect(() => {
    if (floors.length > 0 && activeFloorId === null) setActiveFloorId(floors[0].floorId);
  }, [activeFloorId, floors]);

  const activeFloor = floors.find((floor) => floor.floorId === activeFloorId) ?? floors[0] ?? null;
  const allTables = useMemo(() => flattenTables(floors), [floors]);
  const tableTypeOptions = useMemo(() => {
    const map = new Map<number, string>();
    allTables.forEach((table) => map.set(table.tableTypeId, table.tableTypeName));
    return [...map.entries()].sort((a, b) => a[1].localeCompare(b[1]));
  }, [allTables]);

  const visibleZones = useMemo(() => {
    if (!activeFloor) return [];
    const keyword = filters.search.trim().toLowerCase();
    return activeFloor.zones
      .filter((zone) => !filters.zoneId || zone.zoneId === Number(filters.zoneId))
      .map((zone) => ({
        ...zone,
        tables: zone.tables.filter((table) => {
          if (filters.status && table.operationalStatus !== Number(filters.status)) return false;
          if (filters.tableTypeId && table.tableTypeId !== Number(filters.tableTypeId)) return false;
          if (keyword && !`${table.tableCode} ${table.tableName}`.toLowerCase().includes(keyword)) return false;
          return true;
        })
      }))
      .filter((zone) => zone.tables.length > 0 || (!filters.status && !filters.tableTypeId && !keyword));
  }, [activeFloor, filters]);

  async function refresh() {
    try {
      await reload();
      toast("Đã làm mới sơ đồ bàn.", "success");
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể làm mới sơ đồ bàn.", "error");
    }
  }

  async function startSession(table: SelectedTable) {
    if (table.operationalStatus !== 1 && table.operationalStatus !== 3) {
      toast("Chỉ có thể mở phiên trên bàn sẵn sàng hoặc đã đặt trước.", "error");
      return;
    }

    try {
      const session = await sessionApi.start({ tableId: table.tableId, bookingId: table.nextBookingId });
      toast("Đã mở phiên chơi.", "success");
      setSelected(null);
      router.push(`/operation/sessions/${session.sessionId}`);
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể mở phiên chơi.", "error");
    }
  }

  async function endSession(sessionId: number) {
    try {
      const result = await sessionApi.end(sessionId);
      const invoiceId = result?.invoiceId ?? result?.InvoiceId;
      toast("Đã kết thúc phiên chơi.", "success");
      setSelected(null);
      router.push(invoiceId ? `/operation/invoices?invoiceId=${invoiceId}` : "/operation/invoices");
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể kết thúc phiên chơi.", "error");
    } finally {
      setEndingSessionId(null);
    }
  }

  async function generateInvoice(sessionId: number) {
    try {
      await invoiceApi.generate(sessionId);
      toast("Đã tạo hóa đơn.", "success");
      await reload();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể tạo hóa đơn.", "error");
    }
  }

  return (
    <div className="floor-map-page">
      <PageHeader
        title="Sơ đồ bàn vận hành"
        description="Theo dõi trạng thái bàn, mở phiên chơi và xử lý phiên đang hoạt động."
        action={<button className="primary-btn" type="button" onClick={refresh} disabled={loading}>{loading ? "Đang tải..." : "Làm mới"}</button>}
      />

      <StateBlock loading={loading} error={error} empty={!loading && !floors.length} />

      {layout ? (
        <>
          <div className="floor-map-summary">
            <Metric label="Tổng số bàn" value={layout.totalTables} />
            <Metric label="Sẵn sàng" value={layout.availableTables} tone="available" />
            <Metric label="Đang có khách" value={layout.occupiedTables} tone="occupied" />
            <Metric label="Đã đặt trước" value={layout.reservedTables ?? 0} tone="reserved" />
            <Metric label="Bảo trì" value={layout.maintenanceTables ?? 0} tone="maintenance" />
            <Metric label="Ngừng hoạt động" value={layout.inactiveTables ?? 0} tone="inactive" />
            <div className="updated-at">Cập nhật: {lastUpdated ? dateTime(lastUpdated) : dateTime(layout.fetchedAtUtc)}</div>
          </div>

          <div className="status-legend">
            {statusOptions.slice(1).map((item) => (
              <span key={item.value} className={`legend-item status-${item.value}`}><i />{item.label}</span>
            ))}
          </div>

          <div className="floor-tabs" role="tablist" aria-label="Tầng">
            {floors.map((floor) => (
              <button
                key={floor.floorId}
                className={`floor-tab ${floor.floorId === activeFloor?.floorId ? "active" : ""}`}
                type="button"
                onClick={() => {
                  setActiveFloorId(floor.floorId);
                  setFilters((current) => ({ ...current, zoneId: "" }));
                }}
              >
                {floor.floorName}
              </button>
            ))}
          </div>

          <SearchFilterBar>
            <label><span>Khu vực</span><select value={filters.zoneId} onChange={(event) => setFilters({ ...filters, zoneId: event.target.value })}><option value="">Tất cả khu vực</option>{activeFloor?.zones.map((zone) => <option key={zone.zoneId} value={zone.zoneId}>{zone.zoneName}</option>)}</select></label>
            <label><span>Trạng thái</span><select value={filters.status} onChange={(event) => setFilters({ ...filters, status: event.target.value })}>{statusOptions.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}</select></label>
            <label><span>Loại bàn</span><select value={filters.tableTypeId} onChange={(event) => setFilters({ ...filters, tableTypeId: event.target.value })}><option value="">Tất cả loại bàn</option>{tableTypeOptions.map(([id, name]) => <option key={id} value={id}>{name}</option>)}</select></label>
            <label><span>Tìm kiếm</span><input value={filters.search} placeholder="Mã hoặc tên bàn" onChange={(event) => setFilters({ ...filters, search: event.target.value })} /></label>
          </SearchFilterBar>

          {layout.totalTables === 0 ? <div className="state-card">Chưa có bàn nào trong sơ đồ. Kiểm tra dữ liệu bàn và khu vực trong hệ thống.</div> : null}
          {activeFloor && visibleZones.length === 0 ? <div className="state-card">Không có bàn phù hợp với bộ lọc hiện tại.</div> : null}

          {activeFloor ? (
            <div className="zones-container">
              {visibleZones.map((zone) => (
                <ZoneSection key={zone.zoneId} floor={activeFloor} zone={zone} onSelect={setSelected} />
              ))}
            </div>
          ) : null}
        </>
      ) : null}

      {selected ? (
        <TableDetailModal
          table={selected}
          onClose={() => setSelected(null)}
          onStart={() => startSession(selected)}
          onEnd={() => selected.activeSessionId && setEndingSessionId(selected.activeSessionId)}
          onInvoice={() => selected.activeSessionId && generateInvoice(selected.activeSessionId)}
        />
      ) : null}

      {endingSessionId ? (
        <ConfirmDialog
          title="Kết thúc phiên chơi"
          message="Xác nhận kết thúc phiên chơi trên bàn này?"
          confirmLabel="Kết thúc phiên"
          danger
          onCancel={() => setEndingSessionId(null)}
          onConfirm={() => endSession(endingSessionId)}
        />
      ) : null}
    </div>
  );
}

function flattenTables(floors: VenueFloorLayoutItem[]): SelectedTable[] {
  return floors.flatMap((floor) =>
    floor.zones.flatMap((zone) =>
      zone.tables.map((table) => ({
        ...table,
        floorId: floor.floorId,
        floorName: floor.floorName,
        zoneId: zone.zoneId,
        zoneName: zone.zoneName
      }))
    )
  );
}

function Metric({ label: title, value, tone }: { label: string; value: number; tone?: string }) {
  return <div className="metric-tile"><span>{title}</span><strong className={tone}>{value}</strong></div>;
}

function ZoneSection({ floor, zone, onSelect }: {
  floor: VenueFloorLayoutItem;
  zone: VenueZoneLayoutItem;
  onSelect: (table: SelectedTable) => void;
}) {
  return (
    <section className="zone-section">
      <div className="zone-header">
        <div><h2>{zone.zoneName}</h2>{zone.description ? <p>{zone.description}</p> : null}</div>
        <span>{zone.tables.length} bàn</span>
      </div>
      {zone.tables.length ? (
        <div className="tables-grid">
          {zone.tables.map((table) => (
            <button
              key={table.tableId}
              type="button"
              className={`venue-table-card status-${table.operationalStatus}`}
              onClick={() => onSelect({ ...table, floorId: floor.floorId, floorName: floor.floorName, zoneId: zone.zoneId, zoneName: zone.zoneName })}
            >
              <div className="table-card-head">
                <div><strong>{table.tableCode}</strong><span>{table.tableName}</span></div>
                <Badge tone={statusTone(table.operationalStatus)}>{label(tableStatus, table.operationalStatus)}</Badge>
              </div>
              <div className="table-card-meta">
                <span>{table.tableTypeName}</span>
                <span>{table.capacity} khách</span>
              </div>
              {table.activeSessionId ? <div className="table-card-note">Phiên #{table.activeSessionId}{table.activeSessionStartedAtUtc ? ` - bắt đầu ${dateTime(table.activeSessionStartedAtUtc)}` : ""}</div> : null}
              {!table.activeSessionId && table.nextBookingId ? <div className="table-card-note">Booking {table.nextBookingCode ?? `#${table.nextBookingId}`} - {dateTime(table.nextBookingStartTimeUtc)}</div> : null}
            </button>
          ))}
        </div>
      ) : <div className="state-card">Khu vực này chưa có bàn.</div>}
    </section>
  );
}

function TableDetailModal({ table, onClose, onStart, onEnd, onInvoice }: {
  table: SelectedTable;
  onClose: () => void;
  onStart: () => void;
  onEnd: () => void;
  onInvoice: () => void;
}) {
  const canStart = table.operationalStatus === 1 || table.operationalStatus === 3;
  const isOccupied = Boolean(table.activeSessionId);

  return (
    <Modal title={`${table.tableCode} - ${table.tableName}`} onClose={onClose} size="medium">
      <div className="detail-grid">
        <div><span>Tầng</span><strong>{table.floorName}</strong></div>
        <div><span>Khu vực</span><strong>{table.zoneName}</strong></div>
        <div><span>Loại bàn</span><strong>{table.tableTypeName}</strong></div>
        <div><span>Sức chứa</span><strong>{table.capacity} khách</strong></div>
        <div><span>Trạng thái</span><strong>{label(tableStatus, table.operationalStatus)}</strong></div>
        <div><span>Session</span><strong>{table.activeSessionId ? `#${table.activeSessionId}` : "-"}</strong></div>
        <div><span>Bắt đầu</span><strong>{table.activeSessionStartedAtUtc ? dateTime(table.activeSessionStartedAtUtc) : "-"}</strong></div>
      </div>

      {table.nextBookingId ? (
        <div className="inline-note">
          Booking gần nhất: {table.nextBookingCode ?? `#${table.nextBookingId}`} lúc {dateTime(table.nextBookingStartTimeUtc)}.
        </div>
      ) : null}

      {!canStart && !isOccupied ? (
        <div className="inline-alert error">Không thể mở phiên trên bàn đang bảo trì hoặc ngừng hoạt động.</div>
      ) : null}

      <div className="modal-actions">
        {isOccupied ? (
          <>
            <Link className="ghost-btn" href={`/operation/sessions/${table.activeSessionId}`}>Xem session</Link>
            <Link className="secondary-btn" href={`/operation/orders?sessionId=${table.activeSessionId}`}>Thêm order</Link>
            <button className="ghost-btn" type="button" onClick={onInvoice}>Tạo hóa đơn</button>
            <button className="danger-btn" type="button" onClick={onEnd}>Kết thúc phiên</button>
          </>
        ) : (
          <button className="primary-btn" type="button" disabled={!canStart} onClick={onStart}>Mở phiên chơi</button>
        )}
        <button className="ghost-btn" type="button" onClick={onClose}>Đóng</button>
      </div>
    </Modal>
  );
}

function getFloorMapErrorMessage(err: unknown) {
  if (err instanceof ApiError) {
    if (err.status === 401) return "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
    if (err.status === 403) return "Bạn không có quyền xem sơ đồ bàn.";
    if (err.status === 404) return "Endpoint sơ đồ bàn chưa được cấu hình hoặc frontend/backend chưa đồng bộ.";
    if (err.status === 500) return "Không thể tải sơ đồ bàn. Vui lòng thử lại.";
    if (err.status === 0) return "Không kết nối được backend. Vui lòng kiểm tra API server.";
  }

  return err instanceof Error ? err.message : "Không thể tải sơ đồ bàn. Vui lòng thử lại.";
}
