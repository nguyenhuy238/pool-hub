"use client";

import { useState } from "react";
import { venueApi, sessionApi, invoiceApi } from "@/lib/api/endpoints";
import { label, tableStatus } from "@/lib/status";
import { Badge, PageHeader, StateBlock, useLoad } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { VenueTableLayoutItem } from "@/types";
import "./floor-map.css";

export default function FloorMapPage() {
  const toast = useToast();
  const [selected, setSelected] = useState<VenueTableLayoutItem | null>(null);
  const [activeFloorId, setActiveFloorId] = useState<number | null>(null);

  const { data: layoutRes, loading, error, reload } = useLoad(async () => {
    return await venueApi.layout();
  }, []);

  const floors = layoutRes?.floors || [];
  
  // Set default active floor when data is loaded
  if (floors.length > 0 && activeFloorId === null) {
    setActiveFloorId(floors[0].floorId);
  }

  const activeFloor = floors.find(f => f.floorId === activeFloorId) || floors[0];

  async function startSession(tableId: number) {
    await sessionApi.start({ tableId }).then(() => toast("Đã mở phiên chơi.", "success")).catch((err) => toast(err.message, "error"));
    reload();
    setSelected(null);
  }

  async function endSession(sessionId: number) {
    if (!confirm("Đóng phiên chơi này?")) return;
    await sessionApi.end(sessionId).then(() => toast("Đã đóng session.", "success")).catch((err) => toast(err.message, "error"));
    reload();
    setSelected(null);
  }

  async function generateInvoice(sessionId: number) {
    await invoiceApi.generate(sessionId).then(() => toast("Đã tạo hóa đơn.", "success")).catch((err) => toast(err.message, "error"));
    reload();
  }

  const getTone = (status: number) => {
    switch(status) {
      case 1: return "green";
      case 2: return "blue";
      case 3: return "yellow";
      case 4: return "red";
      default: return "neutral";
    }
  };

  return (
    <div className="layout-container">
      <PageHeader title="Sơ đồ cơ sở" description="Theo dõi trạng thái bàn và phiên chơi theo thời gian thực." />
      
      <StateBlock loading={loading} error={error} empty={!loading && floors.length === 0} />

      {!loading && layoutRes && (
        <div className="stats-bar">
          <div className="stat-item">
            <span className="stat-label">Tổng số bàn</span>
            <span className="stat-value">{layoutRes.totalTables}</span>
          </div>
          <div className="stat-item">
            <span className="stat-label">Đang trống</span>
            <span className="stat-value available">{layoutRes.availableTables}</span>
          </div>
          <div className="stat-item">
            <span className="stat-label">Đang có khách</span>
            <span className="stat-value occupied">{layoutRes.occupiedTables}</span>
          </div>
        </div>
      )}

      {floors.length > 0 && (
        <div className="floor-tabs">
          {floors.map((floor) => (
            <button 
              key={floor.floorId} 
              className={`floor-tab ${floor.floorId === activeFloorId ? 'active' : ''}`}
              onClick={() => setActiveFloorId(floor.floorId)}
            >
              {floor.floorName}
            </button>
          ))}
        </div>
      )}

      {activeFloor && (
        <div className="zones-container">
          {activeFloor.zones.map(zone => (
            <div key={zone.zoneId} className="zone-section">
              <div className="zone-header">
                <h3>{zone.zoneName}</h3>
                {zone.description && <span className="zone-desc">{zone.description}</span>}
              </div>
              
              <div className="tables-grid">
                {zone.tables.map(table => (
                  <div 
                    key={table.tableId} 
                    className={`venue-table-card status-${table.operationalStatus}`}
                    onClick={() => setSelected(table)}
                  >
                    <div className="table-header">
                      <div>
                        <h4 className="table-code">{table.tableCode}</h4>
                        <p className="table-name">{table.tableName}</p>
                      </div>
                      <Badge tone={getTone(table.operationalStatus)}>
                        {label(tableStatus, table.operationalStatus)}
                      </Badge>
                    </div>
                    
                    <div className="table-footer">
                      <span className="table-type">{table.tableTypeName} • {table.capacity} khách</span>
                      {table.activeSessionId && (
                        <span className="active-session-badge">Session #{table.activeSessionId}</span>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}

      {selected ? (
        <div className="card" style={{ position: "fixed", right: 24, bottom: 24, width: "min(380px, calc(100vw - 48px))", zIndex: 100, boxShadow: "0 12px 40px rgba(0,0,0,0.15)" }}>
          <h2 style={{margin: "0 0 4px 0", fontSize: 20}}>{selected.tableName}</h2>
          <p style={{color: "var(--muted)", margin: "0 0 16px 0"}}>{selected.tableTypeName}</p>
          
          <div className="form-grid" style={{marginBottom: 20}}>
            <div>
              <label>Mã bàn</label>
              <strong>{selected.tableCode}</strong>
            </div>
            <div>
              <label>Sức chứa</label>
              <strong>{selected.capacity} người</strong>
            </div>
          </div>

          <div className="actions">
            {selected.activeSessionId ? (
              <>
                <button className="danger-btn" style={{flex: 1}} onClick={() => endSession(selected.activeSessionId!)}>Đóng phiên</button>
                <button className="secondary-btn" onClick={() => generateInvoice(selected.activeSessionId!)}>Tạo Invoice</button>
              </>
            ) : (
              <button className="primary-btn" style={{flex: 1}} onClick={() => startSession(selected.tableId)}>Bắt đầu phiên chơi</button>
            )}
            <button className="ghost-btn" onClick={() => setSelected(null)}>Đóng</button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
