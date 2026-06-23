"use client";

import { useState } from "react";
import { bookingApi, venueApi } from "@/lib/api/endpoints";
import { dateTime, label, bookingStatus } from "@/lib/status";
import { Badge, DataTable, ListControls, PageHeader, StateBlock, useList, useLoad, Pagination } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { Booking } from "@/types";
import { BookingModal } from "./BookingModal";

export default function BookingsPage() {
  const toast = useToast();
  const [status, setStatus] = useState("");
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const [isModalOpen, setIsModalOpen] = useState(false);
  
  // Load tables, table types, and bookings concurrently
  const { data, loading, error, reload } = useLoad(async () => {
    const [bookingsRes, tablesRes, typesRes] = await Promise.all([
      bookingApi.list({ Status: status || undefined, Search: params.search, PageNumber: params.pageNumber, PageSize: params.pageSize }),
      venueApi.tables({ pageSize: 500 }),
      venueApi.tableTypes({ pageSize: 100 })
    ]);
    
    return {
      bookings: bookingsRes,
      tables: Array.isArray(tablesRes) ? tablesRes : (tablesRes && 'items' in tablesRes ? tablesRes.items : []),
      tableTypes: Array.isArray(typesRes) ? typesRes : (typesRes && 'items' in typesRes ? typesRes.items : [])
    };
  }, [status, params]);

  const rows = useList<Booking>(data?.bookings);
  const tables = data?.tables || [];
  const tableTypes = data?.tableTypes || [];

  const tableOptions = tables.map(t => ({ value: String(t.tableId), label: `${t.tableName} (${t.tableCode})` }));
  const typeOptions = tableTypes.map(t => ({ value: String(t.tableTypeId), label: t.name }));

  async function action(fn: Promise<unknown>, message: string) {
    await fn.then(() => toast(message, "success")).catch((err) => toast(err.message, "error"));
    reload();
  }

  return (
    <>
      <PageHeader 
        title="Quản lý đặt bàn"
        description="Theo dõi và tạo lịch đặt bàn mới cho khách hàng." 
        action={
          <div style={{display: 'flex', gap: '12px'}}>
            <a href="/operation/bookings/calendar" className="primary-btn" style={{background: '#123b63', textDecoration: 'none'}}>📅 Xem Lịch (Calendar)</a>
            <select value={status} onChange={(e) => setStatus(e.target.value)} style={{padding: '8px', borderRadius: '6px', border: '1px solid var(--line)'}}>
              <option value="">Tất cả trạng thái</option>
              <option value="1">Chờ xác nhận (Pending)</option>
              <option value="2">Đã xác nhận (Confirmed)</option>
              <option value="3">Đã hủy (Cancelled)</option>
              <option value="4">Hoàn thành (Completed)</option>
            </select>
          </div>
        } 
      />
      <ListControls search={params.search} pageNumber={params.pageNumber} pageSize={params.pageSize} onChange={setParams} />
      
      <div style={{ padding: "0 24px", marginBottom: "16px" }}>
        <button className="primary-btn" onClick={() => setIsModalOpen(true)}>+ Tạo Booking Nhanh</button>
      </div>

      {isModalOpen && (
        <BookingModal 
          onClose={() => setIsModalOpen(false)} 
          onSuccess={() => {
            setIsModalOpen(false);
            reload();
          }} 
          tables={tables} 
        />
      )}
      
      <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
      <DataTable 
        rows={rows as unknown as Record<string, unknown>[]} 
        columns={[
          { key: "bookingCode", label: "Mã Booking" },
          { key: "customerName", label: "Khách hàng", render: (row) => (
            <div>
              <strong style={{color: 'var(--ink)'}}>{String(row.customerName || 'Khách vãng lai')}</strong><br/>
              <span style={{fontSize: '13px', color: 'var(--muted)'}}>{String(row.phoneNumber || '-')}</span>
            </div>
          )},
          { key: "tableId", label: "Bàn / Loại bàn", render: (row) => {
             if (row.tableId) {
               const t = tables.find(x => x.tableId === Number(row.tableId));
               return t ? <strong>{t.tableName}</strong> : `Bàn #${row.tableId}`;
             }
             if (row.tableTypeId) {
               const tt = tableTypes.find(x => x.tableTypeId === Number(row.tableTypeId));
               return tt ? <span>Loại: {tt.name}</span> : `Loại #${row.tableTypeId}`;
             }
             return <span style={{color: 'var(--muted)'}}>Chưa xếp bàn</span>;
          }},
          { key: "startTimeUtc", label: "Bắt đầu", render: (row) => dateTime(String(row.startTimeUtc)) },
          { key: "endTimeUtc", label: "Kết thúc", render: (row) => dateTime(String(row.endTimeUtc)) },
          { key: "status", label: "Trạng thái", render: (row) => <Badge tone={Number(row.status) === 3 ? "red" : Number(row.status) === 2 ? "green" : Number(row.status) === 4 ? "blue" : "yellow"}>{label(bookingStatus, Number(row.status))}</Badge> }
        ]} 
        actions={(row) => (
          <div style={{display: 'flex', gap: '8px'}}>
            <button 
              className="primary-btn" 
              style={{
                padding: '6px 12px', 
                fontSize: '13px',
                visibility: Number(row.status) === 1 ? 'visible' : 'hidden'
              }} 
              onClick={() => {
                if (Number(row.status) === 1) {
                  action(bookingApi.confirm(Number(row.bookingId)), "Đã xác nhận booking.");
                }
              }}
            >
              Xác nhận
            </button>
            {(Number(row.status) === 1 || Number(row.status) === 2) && (
              <button 
                className="danger-btn" 
                style={{padding: '6px 12px', fontSize: '13px'}} 
                onClick={() => action(bookingApi.cancel(Number(row.bookingId)), "Đã hủy booking.")}
              >
                Hủy
              </button>
            )}
          </div>
        )} 
      />
      <Pagination 
        pageNumber={params.pageNumber} 
        totalPages={(data?.bookings as any)?.totalCount ? Math.ceil((data?.bookings as any).totalCount / params.pageSize) : 102}
        onChange={(page) => setParams(prev => ({ ...prev, pageNumber: page }))} 
      />
    </>
  );
}
