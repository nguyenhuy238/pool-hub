"use client";
import { useState } from "react";
import { venueApi } from "@/lib/api/endpoints";
import { getTotalPages } from "@/lib/api/client";
import { Badge, DataTable, PageHeader, SmartForm, StateBlock, useList, useLoad, ListControls, Pagination } from "@/components/ui";
import type { VenueTable, Zone, TableType } from "@/types";

const STATUS_OPTIONS = [
  { value: "1", label: "Sẵn sàng" },
  { value: "2", label: "Đang chơi" },
  { value: "3", label: "Đã đặt" },
  { value: "4", label: "Bảo trì" }
];

export default function VenueTablesPage() {
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const { data, loading, error, reload } = useLoad(async () => ({
    tables: await venueApi.tables({ Search: params.search || undefined, PageNumber: params.pageNumber, PageSize: params.pageSize }),
    zones: await venueApi.zones({ PageSize: 100 }),
    tableTypes: await venueApi.tableTypes({ PageSize: 100 })
  }), [params]);
  
  const tables = useList<VenueTable>(data?.tables);
  const zones = useList<Zone>(data?.zones);
  const tableTypes = useList<TableType>(data?.tableTypes);
  
  const zoneOptions = zones.map(z => ({ value: String(z.zoneId), label: z.name }));
  const typeOptions = tableTypes.map(t => ({ value: String(t.tableTypeId), label: t.name }));

  const getStatusBadge = (status: number) => {
    switch (status) {
      case 1: return <Badge tone="green">Sẵn sàng</Badge>;
      case 2: return <Badge tone="blue">Đang chơi</Badge>;
      case 3: return <Badge tone="yellow">Đã đặt</Badge>;
      case 4: return <Badge tone="red">Bảo trì</Badge>;
      default: return <Badge tone="neutral">Không xác định</Badge>;
    }
  };

  return (
    <>
      <PageHeader title="Quản lý bàn chơi" description="Danh sách các bàn chơi trong cơ sở." />
      
      <SmartForm<VenueTable> 
        title="Tạo bàn mới"
        initial={{ operationalStatus: 1 } as any} 
        fields={[
          { name: "zoneId", label: "Khu vực", options: zoneOptions, required: true },
          { name: "tableTypeId", label: "Loại bàn", options: typeOptions, required: true },
          { name: "tableCode", label: "Mã bàn (VD: T01)", required: true }, 
          { name: "tableName", label: "Tên bàn", required: true }, 
          { name: "capacity", label: "Sức chứa (người)", type: "number" },
          { name: "operationalStatus", label: "Trạng thái hoạt động", options: STATUS_OPTIONS, required: true }
        ]} 
        onSubmit={async (value) => { 
          await venueApi.createTable({ 
            ...value, 
            zoneId: Number(value.zoneId),
            tableTypeId: Number(value.tableTypeId),
            operationalStatus: Number(value.operationalStatus),
            capacity: value.capacity ? Number(value.capacity) : undefined
          }); 
          reload(); 
        }} 
      />
      
      <br />
      <h2>Danh sách Bàn chơi</h2>
      <ListControls search={params.search} pageNumber={params.pageNumber} pageSize={params.pageSize} onChange={setParams} />
      
      <StateBlock loading={loading} error={error} empty={!loading && !tables.length} />
      
      <DataTable 
        rows={tables.map(t => ({ ...t, id: t.tableId })) as unknown as Record<string, unknown>[]} 
        columns={[
          { key: "tableId", label: "ID" },
          { key: "tableCode", label: "Mã bàn", render: (row) => <strong>{row.tableCode as string}</strong> },
          { key: "tableName", label: "Tên bàn" }, 
          { key: "zoneId", label: "Khu vực", render: (row) => zones.find(z => z.zoneId === Number(row.zoneId))?.name || String(row.zoneId) },
          { key: "tableTypeId", label: "Loại bàn", render: (row) => tableTypes.find(t => t.tableTypeId === Number(row.tableTypeId))?.name || String(row.tableTypeId) },
          { key: "capacity", label: "Sức chứa" },
          { key: "operationalStatus", label: "Trạng thái", render: (row) => getStatusBadge(Number(row.operationalStatus)) }
        ]} 
        actions={(row) => (
          <button className="danger-btn" onClick={() => venueApi.deleteTable(Number(row.tableId)).then(() => reload())}>Xóa</button>
        )}
      />
      
      <Pagination 
        pageNumber={params.pageNumber} 
        totalPages={getTotalPages(data?.tables, params.pageSize)}
        onChange={(page) => setParams(prev => ({ ...prev, pageNumber: page }))} 
      />
    </>
  );
}
