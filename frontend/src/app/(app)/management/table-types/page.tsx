"use client";
import { CrudPage } from "@/components/crud-pages";
import { venueApi } from "@/lib/api/endpoints";
import type { TableType } from "@/types";
export default function TableTypesPage() { return <CrudPage<TableType> title="Table Types" idKey="tableTypeId" load={venueApi.tableTypes} create={venueApi.createTableType} remove={venueApi.deleteTableType} fields={[{ name: "name", label: "Tên", required: true }, { name: "code", label: "Code", required: true }, { name: "description", label: "Mô tả" }, { name: "defaultCapacity", label: "Sức chứa", type: "number" }]} columns={[{ key: "name", label: "Tên" }, { key: "code", label: "Code" }, { key: "defaultCapacity", label: "Sức chứa" }]} />; }
