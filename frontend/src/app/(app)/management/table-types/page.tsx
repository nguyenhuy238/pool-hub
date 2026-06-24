"use client";
import { useState } from "react";
import { venueApi } from "@/lib/api/endpoints";
import { getTotalPages } from "@/lib/api/client";
import { DataTable, PageHeader, SmartForm, StateBlock, useList, useLoad, ListControls, Pagination } from "@/components/ui";
import type { TableType } from "@/types";

export default function TableTypesPage() {
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const { data, loading, error, reload } = useLoad(() => 
    venueApi.tableTypes({ Search: params.search || undefined, PageNumber: params.pageNumber, PageSize: params.pageSize }), 
    [params]
  );
  
  const tableTypes = useList<TableType>(data);

  return (
    <>
      <PageHeader title="Quản lý loại bàn" description="Danh sách các loại bàn như bida lỗ, bida phăng và snooker." />
      
      <SmartForm<TableType> 
        title="Tạo loại bàn mới"
        initial={{}} 
        fields={[
          { name: "name", label: "Tên Loại", required: true }, 
          { name: "code", label: "Mã Code" }, 
          { name: "description", label: "Mô tả" }, 
          { name: "defaultCapacity", label: "Sức chứa mặc định", type: "number" }
        ]} 
        onSubmit={async (value) => { 
          await venueApi.createTableType(value); 
          reload(); 
        }} 
      />
      
      <br />
      <h2>Danh sách Loại bàn</h2>
      <ListControls search={params.search} pageNumber={params.pageNumber} pageSize={params.pageSize} onChange={setParams} />
      
      <StateBlock loading={loading} error={error} empty={!loading && !tableTypes.length} />
      
      <DataTable 
        rows={tableTypes.map(t => ({ ...t, id: t.tableTypeId })) as unknown as Record<string, unknown>[]} 
        columns={[
          { key: "tableTypeId", label: "ID" },
          { key: "code", label: "Mã" },
          { key: "name", label: "Tên Loại bàn" }, 
          { key: "description", label: "Mô tả" }, 
          { key: "defaultCapacity", label: "Sức chứa" }
        ]} 
        actions={(row) => (
          <button className="danger-btn" onClick={() => venueApi.deleteTableType(Number(row.tableTypeId)).then(() => reload())}>Xóa</button>
        )}
      />
      
      <Pagination 
        pageNumber={params.pageNumber} 
        totalPages={getTotalPages(data, params.pageSize)}
        onChange={(page) => setParams(prev => ({ ...prev, pageNumber: page }))} 
      />
    </>
  );
}
