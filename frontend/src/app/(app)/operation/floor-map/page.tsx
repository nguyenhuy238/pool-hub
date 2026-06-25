"use client";

import { useEffect, useMemo, useState } from "react";
import { venueApi } from "@/lib/api/endpoints";
import { label, tableStatus } from "@/lib/status";
import { Badge, ConfirmDialog, PageHeader, StateBlock, useLoad } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { VenueTableLayoutItem, Zone, TableType } from "@/types";
import { TableFormModal } from "../../management/venue-tables/page";
import "./floor-map.css";

function getTableTypeColors(name: string) {
  const n = name.toLowerCase();
  if (n.includes('vip')) return { color: '#8b5cf6', background: '#ede9fe' };
  if (n.includes('standard')) return { color: '#10b981', background: '#d1fae5' };
  if (n.includes('carom')) return { color: '#f59e0b', background: '#fef3c7' };
  if (n.includes('snooker')) return { color: '#ef4444', background: '#fee2e2' };
  return { color: '#6b7280', background: '#f3f4f6' };
}

export default function FloorMapPage() {
  const toast = useToast();
  const [selected, setSelected] = useState<(VenueTableLayoutItem & { zoneName?: string }) | null>(null);
  const [activeFloorId, setActiveFloorId] = useState<number | null>(null);
  
  const [editingTable, setEditingTable] = useState<any | "new" | null>(null);
  const [deletingTable, setDeletingTable] = useState<any | null>(null);
  const [tableTypes, setTableTypes] = useState<TableType[]>([]);
  const [zones, setZones] = useState<Zone[]>([]);
  
  useEffect(() => {
    venueApi.tableTypes({ PageSize: 200 }).then(res => {
      setTableTypes(Array.isArray(res) ? res : res.items || []);
    });
    venueApi.zones({ PageSize: 200 }).then(res => {
      setZones(Array.isArray(res) ? res : res.items || []);
    });
  }, []);

  const { data: layoutRes, loading, error, reload } = useLoad(async () => {
    return await venueApi.layout();
  }, []);

  const floors = useMemo(() => layoutRes?.floors ?? [], [layoutRes?.floors]);
  
  useEffect(() => {
    if (floors.length > 0 && activeFloorId === null) {
      setActiveFloorId(floors[0].floorId);
    }
  }, [activeFloorId, floors]);

  const activeFloor = floors.find(f => f.floorId === activeFloorId) || floors[0];

  async function removeTable() {
    if (!deletingTable) return;
    try {
      await venueApi.deleteTable(deletingTable.tableId);
      toast("Đã xóa bàn chơi.", "success");
      setDeletingTable(null);
      setSelected(null);
      reload();
    } catch (err: any) {
      toast(err.message || "Không thể xóa bàn.", "error");
    }
  }

  return (
    <div className="layout-container">
      <PageHeader title="Sơ đồ cơ sở" description="Theo dõi và quản lý bàn chơi." action={<button className="primary-btn" onClick={() => setEditingTable("new")}>Tạo bàn mới</button>} />
      
      <StateBlock loading={loading} error={error} empty={!loading && (!floors.length || !layoutRes?.totalTables)} />

      {!loading && layoutRes && (
        <div className="stats-bar">
          <div className="stat-item">
            <span className="stat-label">Tổng số bàn</span>
            <span className="stat-value">{layoutRes.totalTables}</span>
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
                    className="venue-table-card"
                    onClick={() => setSelected({ ...table, zoneId: zone.zoneId, zoneName: zone.zoneName } as any)}
                    style={{ borderLeftColor: getTableTypeColors(table.tableTypeName).color }}
                  >
                    <div className="table-header">
                      <div>
                        <h4 className="table-code">{table.tableCode}</h4>
                        <p className="table-name">{table.tableName}</p>
                      </div>
                    </div>
                    
                    <div className="table-footer">
                      <span className="table-type" style={{ 
                        ...getTableTypeColors(table.tableTypeName), 
                        padding: '2px 8px', borderRadius: '12px', fontWeight: 600 
                      }}>
                        {table.tableTypeName}
                      </span>
                      <span style={{ fontSize: '12px', color: 'var(--muted)' }}>{table.capacity} khách</span>
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
            <div>
              <label>Khu vực</label>
              <strong>{selected.zoneName ?? "-"}</strong>
            </div>
            <div>
              <label>Trạng thái</label>
              <strong>{label(tableStatus, selected.operationalStatus)}</strong>
            </div>
          </div>

          <div className="actions">
            <button className="secondary-btn" style={{flex: 1}} onClick={() => setEditingTable(selected)}>Chỉnh sửa</button>
            <button className="danger-btn" onClick={() => setDeletingTable(selected)}>Xóa</button>
            <button className="ghost-btn" onClick={() => setSelected(null)}>Đóng</button>
          </div>
        </div>
      ) : null}
      {editingTable ? <TableFormModal table={editingTable === "new" ? null : editingTable} zones={zones} tableTypes={tableTypes} onClose={() => setEditingTable(null)} onSaved={async () => { setEditingTable(null); await reload(); }} /> : null}
      {deletingTable ? <ConfirmDialog title="Xóa bàn chơi" message={`Xóa bàn “${deletingTable.tableName}”?`} confirmLabel="Xóa" danger onCancel={() => setDeletingTable(null)} onConfirm={removeTable} /> : null}
    </div>
  );
}
