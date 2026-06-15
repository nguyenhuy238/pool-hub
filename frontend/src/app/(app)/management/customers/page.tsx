"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { customerApi } from "@/lib/api/endpoints";
import { Badge, DataTable, ListControls, PageHeader, StateBlock, useList, useLoad, Pagination } from "@/components/ui";
import { dateTime } from "@/lib/status";
import type { CustomerDto } from "@/types";

export default function CustomersPage() {
  const router = useRouter();
  const [params, setParams] = useState({ search: "", status: "", pageNumber: 1, pageSize: 20 });
  
  const { data, loading, error } = useLoad(() => 
    customerApi.list({ 
      search: params.search || undefined, 
      status: params.status === "true" ? true : params.status === "false" ? false : undefined,
      pageNumber: params.pageNumber, 
      pageSize: params.pageSize 
    }), 
  [params]);
  
  const rows = useList<CustomerDto>(data);

  return (
    <>
      <PageHeader 
        title="Quản Lý Khách Hàng" 
        description="Quản lý thông tin và lịch sử đặt bàn của khách hàng." 
        action={
          <select value={params.status} onChange={(e) => setParams(p => ({ ...p, status: e.target.value, pageNumber: 1 }))}>
            <option value="">Tất cả trạng thái</option>
            <option value="true">Đang hoạt động (Active)</option>
            <option value="false">Đã khóa (Inactive)</option>
          </select>
        } 
      />
      <ListControls 
        search={params.search} 
        pageNumber={params.pageNumber} 
        pageSize={params.pageSize} 
        onChange={(next) => setParams(p => ({ ...p, ...next }))} 
      />
      
      <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
      
      <DataTable 
        rows={rows as unknown as Record<string, unknown>[]} 
        columns={[
          { key: "fullName", label: "Họ và tên" },
          { key: "phoneNumber", label: "Số điện thoại", render: (row) => (row.phoneNumber as string) || <span style={{color: 'var(--muted)'}}>N/A</span> },
          { key: "email", label: "Email", render: (row) => (row.email as string) || <span style={{color: 'var(--muted)'}}>N/A</span> },
          { key: "totalBookings", label: "Số lần đặt bàn", render: (row) => <strong>{row.totalBookings as number}</strong> },
          { key: "status", label: "Trạng thái", render: (row) => 
            <Badge tone={row.status ? "green" : "red"}>{row.status ? "Active" : "Inactive"}</Badge> 
          },
          { key: "createdAtUtc", label: "Ngày tham gia", render: (row) => dateTime(String(row.createdAtUtc)) }
        ]} 
        actions={(row) => (
          <button 
            className="secondary-btn" 
            onClick={() => router.push(`/management/customers/${row.customerId}`)}
          >
            Chi tiết / Sửa
          </button>
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
