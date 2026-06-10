"use client";

import { venueApi } from "@/lib/api/endpoints";
import { label, tableStatus } from "@/lib/status";
import { Badge, PageHeader, StateBlock, useList, useLoad } from "@/components/ui";
import type { VenueTable } from "@/types";

export default function TablesPage() {
  const { data, loading, error } = useLoad(() => venueApi.tables(), []);
  const tables = useList<VenueTable>(data);
  return (
    <section className="section">
      <PageHeader title="Bàn chơi" description="Danh sách bàn công khai từ hệ thống vận hành." />
      <StateBlock loading={loading} error={error} empty={!loading && !tables.length} />
      <div className="floor-grid">
        {tables.map((table) => <div className="card table-card" key={table.tableId}><h3>{table.tableName}</h3><p>{table.tableCode}</p><Badge tone="green">{label(tableStatus, table.operationalStatus)}</Badge></div>)}
      </div>
    </section>
  );
}
