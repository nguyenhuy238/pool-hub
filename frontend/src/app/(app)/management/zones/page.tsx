"use client";
import { useState } from "react";
import { venueApi } from "@/lib/api/endpoints";
import { getTotalPages } from "@/lib/api/client";
import { DataTable, PageHeader, SmartForm, StateBlock, useList, useLoad, ListControls, Pagination } from "@/components/ui";
import type { Zone, Floor } from "@/types";

export default function ZonesPage() {
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const { data, loading, error, reload } = useLoad(async () => ({
    zones: await venueApi.zones({ Search: params.search || undefined, PageNumber: params.pageNumber, PageSize: params.pageSize }),
    floors: await venueApi.floors({ PageSize: 100 })
  }), [params]);
  
  const zones = useList<Zone>(data?.zones);
  const floors = useList<Floor>(data?.floors);
  
  const floorOptions = floors.map(f => ({ value: String(f.floorId), label: f.name }));

  return (
    <>
      <PageHeader title="Quản lý khu vực" description="Danh sách các khu vực trực thuộc từng tầng." />
      
      <SmartForm<Zone> 
        title="Tạo khu vực mới"
        initial={{}} 
        fields={[
          { name: "floorId", label: "Tầng", options: floorOptions, required: true },
          { name: "name", label: "Tên Khu vực", required: true }, 
          { name: "description", label: "Mô tả" }, 
          { name: "displayOrder", label: "Thứ tự hiển thị", type: "number" }
        ]} 
        onSubmit={async (value) => { 
          await venueApi.createZone({ ...value, floorId: Number(value.floorId) }); 
          reload(); 
        }} 
      />
      
      <br />
      <h2>Danh sách Khu vực</h2>
      <ListControls search={params.search} pageNumber={params.pageNumber} pageSize={params.pageSize} onChange={setParams} />
      
      <StateBlock loading={loading} error={error} empty={!loading && !zones.length} />
      
      <DataTable 
        rows={zones.map(z => ({ ...z, id: z.zoneId })) as unknown as Record<string, unknown>[]} 
        columns={[
          { key: "zoneId", label: "ID" },
          { key: "floorId", label: "Tầng", render: (row) => floors.find(f => f.floorId === Number(row.floorId))?.name || String(row.floorId) },
          { key: "name", label: "Tên Khu vực" }, 
          { key: "description", label: "Mô tả" }, 
          { key: "displayOrder", label: "Thứ tự" }
        ]} 
        actions={(row) => (
          <button className="danger-btn" onClick={() => venueApi.deleteZone(Number(row.zoneId)).then(() => reload())}>Xóa</button>
        )}
      />
      
      <Pagination 
        pageNumber={params.pageNumber} 
        totalPages={getTotalPages(data?.zones, params.pageSize)}
        onChange={(page) => setParams(prev => ({ ...prev, pageNumber: page }))} 
      />
    </>
  );
}
