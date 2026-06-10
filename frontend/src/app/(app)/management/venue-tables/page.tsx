"use client";
import { CrudPage } from "@/components/crud-pages";
import { venueApi } from "@/lib/api/endpoints";
import type { VenueTable } from "@/types";
export default function VenueTablesPage() { return <CrudPage<VenueTable> title="Venue Tables" idKey="tableId" load={venueApi.tables} create={venueApi.createTable} remove={venueApi.deleteTable} fields={[{ name: "zoneId", label: "Zone ID", type: "number", required: true }, { name: "tableTypeId", label: "Table Type ID", type: "number", required: true }, { name: "tableCode", label: "Mã bàn", required: true }, { name: "tableName", label: "Tên bàn", required: true }, { name: "capacity", label: "Sức chứa", type: "number" }, { name: "operationalStatus", label: "Trạng thái", type: "number" }]} columns={[{ key: "tableCode", label: "Mã" }, { key: "tableName", label: "Tên" }, { key: "capacity", label: "Sức chứa" }, { key: "operationalStatus", label: "Trạng thái" }]} />; }
