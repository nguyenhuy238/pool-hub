"use client";

import { useMemo, useState } from "react";
import { bookingApi, invoiceApi, sessionApi, venueApi } from "@/lib/api/endpoints";
import { unwrapList } from "@/lib/api/client";
import { label, tableStatus } from "@/lib/status";
import { Badge, PageHeader, StateBlock, useLoad } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { Booking, Floor, Session, VenueTable, Zone } from "@/types";

export default function FloorMapPage() {
  const toast = useToast();
  const [selected, setSelected] = useState<VenueTable | null>(null);
  const { data, loading, error, reload } = useLoad(async () => {
    const [floors, zones, tables, sessions, bookings] = await Promise.all([venueApi.floors(), venueApi.zones(), venueApi.tables(), sessionApi.list(), bookingApi.list()]);
    const sessionList = unwrapList(sessions);
    const activeDetails = await Promise.all(sessionList.filter((session) => session.status === 1).map((session) => sessionApi.detail(session.sessionId).catch(() => session)));
    return { floors: unwrapList(floors), zones: unwrapList(zones), tables: unwrapList(tables), sessions: activeDetails, bookings: unwrapList(bookings) };
  }, []);

  const activeByTable = useMemo(() => {
    const map = new Map<number, Session>();
    (data?.sessions || []).forEach((session) => session.assignments?.forEach((assignment) => {
      if (!assignment.endedAtUtc) map.set(assignment.tableId, session);
    }));
    return map;
  }, [data]);

  const floors = data?.floors || [];
  const zones = data?.zones || [];
  const tables = data?.tables || [];
  const bookings = data?.bookings || [];

  async function start(tableId: number) {
    await sessionApi.start({ tableId }).then(() => toast("Đã mở phiên chơi.", "success")).catch((err) => toast(err.message, "error"));
    reload();
  }

  async function end(sessionId: number) {
    if (!confirm("Đóng phiên chơi này?")) return;
    await sessionApi.end(sessionId).then(() => toast("Đã đóng session.", "success")).catch((err) => toast(err.message, "error"));
    reload();
  }

  async function generateInvoice(sessionId: number) {
    await invoiceApi.generate(sessionId).then(() => toast("Đã tạo hóa đơn.", "success")).catch((err) => toast(err.message, "error"));
    reload();
  }

  return (
    <>
      <PageHeader title="Floor Map" description="Sơ đồ bàn theo tầng và khu vực, đồng bộ bàn, booking và session." />
      <StateBlock loading={loading} error={error} empty={!loading && !data?.tables.length} />
      {(floors as Floor[]).map((floor) => (
        <section className="card" key={floor.floorId} style={{ marginBottom: 16 }}>
          <h2>{floor.name}</h2>
          {(zones as Zone[]).filter((zone) => zone.floorId === floor.floorId).map((zone) => (
            <div key={zone.zoneId} style={{ marginTop: 14 }}>
              <h3>{zone.name}</h3>
              <div className="floor-grid">
                {(tables as VenueTable[]).filter((table) => table.zoneId === zone.zoneId).map((table) => {
                  const active = activeByTable.get(table.tableId);
                  const reserved = (bookings as Booking[]).some((booking) => booking.tableId === table.tableId && [1, 2].includes(booking.status));
                  const statusText = active ? "In Use" : reserved ? "Reserved" : label(tableStatus, table.operationalStatus);
                  const tone = active ? "blue" : reserved ? "yellow" : table.operationalStatus >= 3 ? "red" : "green";
                  return (
                    <button className={`card table-card ${active ? "in-use" : reserved ? "reserved" : "available"}`} key={table.tableId} onClick={() => setSelected(table)}>
                      <h3>{table.tableName}</h3>
                      <p>{table.tableCode} · {table.capacity} khách</p>
                      <Badge tone={tone}>{statusText}</Badge>
                      {active ? <p>{active.sessionCode || `Session #${active.sessionId}`}</p> : null}
                    </button>
                  );
                })}
              </div>
            </div>
          ))}
        </section>
      ))}
      {selected ? <div className="card" style={{ position: "fixed", right: 20, top: 96, width: "min(420px, calc(100vw - 40px))", zIndex: 30 }}>
        <h2>{selected.tableName}</h2>
        <p>Mã bàn: {selected.tableCode}</p>
        <p>Sức chứa: {selected.capacity}</p>
        <div className="actions">
          {activeByTable.get(selected.tableId) ? <>
            <button className="danger-btn" onClick={() => end(activeByTable.get(selected.tableId)!.sessionId)}>Kết thúc session</button>
            <button className="secondary-btn" onClick={() => generateInvoice(activeByTable.get(selected.tableId)!.sessionId)}>Tạo invoice</button>
          </> : <button className="primary-btn" onClick={() => start(selected.tableId)}>Start Session</button>}
          <button className="ghost-btn" onClick={() => setSelected(null)}>Đóng</button>
        </div>
      </div> : null}
    </>
  );
}
