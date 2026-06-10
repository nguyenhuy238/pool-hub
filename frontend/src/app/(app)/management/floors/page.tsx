"use client";
import { CrudPage } from "@/components/crud-pages";
import { venueApi } from "@/lib/api/endpoints";
import type { Floor } from "@/types";
export default function FloorsPage() { return <CrudPage<Floor> title="Floors" idKey="floorId" load={venueApi.floors} create={venueApi.createFloor} remove={venueApi.deleteFloor} fields={[{ name: "name", label: "Tên", required: true }, { name: "description", label: "Mô tả" }, { name: "displayOrder", label: "Thứ tự", type: "number" }]} columns={[{ key: "name", label: "Tên" }, { key: "description", label: "Mô tả" }, { key: "displayOrder", label: "Thứ tự" }]} />; }
