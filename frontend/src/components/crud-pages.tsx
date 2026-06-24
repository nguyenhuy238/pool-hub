"use client";

import { useState } from "react";
import { DataTable, ListControls, PageHeader, SmartForm, StateBlock, useList, useLoad } from "@/components/ui";

type CrudConfig<T extends Record<string, unknown>> = {
  title: string;
  description?: string;
  idKey: keyof T;
  load: (params?: Record<string, string | number | undefined>) => Promise<T[] | { items?: T[]; data?: T[] }>;
  create: (value: Partial<T>) => Promise<unknown>;
  remove?: (id: number) => Promise<unknown>;
  fields: { name: keyof T; label: string; type?: string; required?: boolean }[];
  columns: { key: keyof T | string; label: string; render?: (row: Record<string, unknown>) => React.ReactNode }[];
};

export function CrudPage<T extends Record<string, unknown>>(config: CrudConfig<T>) {
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20 });
  const { data, loading, error, reload } = useLoad(() => config.load({ Search: params.search, PageNumber: params.pageNumber, PageSize: params.pageSize }), [params]);
  const rows = useList<T>(data);
  return (
    <>
      <PageHeader title={config.title} description={config.description} />
      <ListControls search={params.search} pageNumber={params.pageNumber} pageSize={params.pageSize} onChange={setParams} />
      <SmartForm<T> title={`Tạo ${config.title}`} initial={{}} fields={config.fields} onSubmit={async (value) => { await config.create(value); reload(); }} />
      <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
      <DataTable rows={rows as unknown as Record<string, unknown>[]} columns={config.columns.map((column) => ({ ...column, key: String(column.key) }))} actions={config.remove ? (row) => <button className="danger-btn" onClick={() => config.remove?.(Number(row[String(config.idKey)])).then(() => reload())}>Xóa</button> : undefined} />
    </>
  );
}
