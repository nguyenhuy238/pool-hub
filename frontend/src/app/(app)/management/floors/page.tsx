"use client";
import { useState } from "react";
import { venueApi } from "@/lib/api/endpoints";
import { DataTable, PageHeader, SmartForm, StateBlock, useList, useLoad, ListControls, Pagination } from "@/components/ui";
import type { Floor } from "@/types";

export default function FloorsPage() {
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const { data, loading, error, reload } = useLoad(() => 
    venueApi.floors({ Search: params.search || undefined, PageNumber: params.pageNumber, PageSize: params.pageSize }), 
    [params]
  );
  
  const floors = useList<Floor>(data);

  return (
    <>
      <PageHeader title="Quản lý tầng" description="Danh sách các tầng trong cơ sở." />
      
      <SmartForm<Floor> 
        title="Tạo tầng mới"
        initial={{}} 
        fields={[
          { name: "name", label: "Tên Tầng", required: true }, 
          { name: "description", label: "Mô tả" }, 
          { name: "displayOrder", label: "Thứ tự hiển thị", type: "number" }
        ]} 
        onSubmit={async (value) => { 
          await venueApi.createFloor(value); 
          reload(); 
        }} 
      />
      
      <br />
      <h2>Danh sách Tầng</h2>
      <ListControls search={params.search} pageNumber={params.pageNumber} pageSize={params.pageSize} onChange={setParams} />
      
      <StateBlock loading={loading} error={error} empty={!loading && !floors.length} />
      
      <DataTable 
        rows={floors.map(f => ({ ...f, id: f.floorId })) as unknown as Record<string, unknown>[]} 
        columns={[
          { key: "floorId", label: "ID" },
          { key: "name", label: "Tên Tầng" }, 
          { key: "description", label: "Mô tả" }, 
          { key: "displayOrder", label: "Thứ tự" }
        ]} 
        actions={(row) => (
          <button className="danger-btn" onClick={() => venueApi.deleteFloor(Number(row.floorId)).then(() => reload())}>Xóa</button>
        )}
      />
      
      <Pagination 
        pageNumber={params.pageNumber} 
        totalPages={(data as any)?.totalCount ? Math.ceil((data as any).totalCount / params.pageSize) : 102}
        onChange={(page) => setParams(prev => ({ ...prev, pageNumber: page }))} 
      />
    </>
  );
}
