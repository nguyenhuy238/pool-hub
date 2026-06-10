"use client";
import { CrudPage } from "@/components/crud-pages";
import { venueApi } from "@/lib/api/endpoints";
import type { Zone } from "@/types";
export default function ZonesPage() { return <CrudPage<Zone> title="Zones" idKey="zoneId" load={venueApi.zones} create={venueApi.createZone} remove={venueApi.deleteZone} fields={[{ name: "floorId", label: "Floor ID", type: "number", required: true }, { name: "name", label: "Tên", required: true }, { name: "description", label: "Mô tả" }, { name: "displayOrder", label: "Thứ tự", type: "number" }]} columns={[{ key: "floorId", label: "Floor" }, { key: "name", label: "Tên" }, { key: "displayOrder", label: "Thứ tự" }]} />; }
