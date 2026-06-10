"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { ApiError, unwrapList } from "@/lib/api/client";
import { useToast } from "@/components/toast";
import type { SelectOption } from "@/types";

export function PageHeader({ title, description, action }: { title: string; description?: string; action?: React.ReactNode }) {
  return (
    <div className="page-header">
      <div>
        <h1>{title}</h1>
        {description ? <p>{description}</p> : null}
      </div>
      {action}
    </div>
  );
}

export function StateBlock({ loading, error, empty }: { loading?: boolean; error?: string | null; empty?: boolean }) {
  if (loading) return <div className="state-card">Đang tải dữ liệu...</div>;
  if (error) return <div className="state-card error">{error}</div>;
  if (empty) return <div className="state-card">Chưa có dữ liệu phù hợp.</div>;
  return null;
}

export function Badge({ children, tone = "neutral" }: { children: React.ReactNode; tone?: "green" | "blue" | "yellow" | "red" | "purple" | "neutral" }) {
  return <span className={`badge ${tone}`}>{children}</span>;
}

export function ListControls({ search, pageNumber, pageSize, onChange, extra }: {
  search: string;
  pageNumber: number;
  pageSize: number;
  onChange: (next: { search: string; pageNumber: number; pageSize: number }) => void;
  extra?: React.ReactNode;
}) {
  return (
    <div className="card list-controls">
      <label><span>Tìm kiếm</span><input value={search} onChange={(event) => onChange({ search: event.target.value, pageNumber: 1, pageSize })} placeholder="Nhập từ khóa" /></label>
      <label><span>Trang</span><input type="number" min={1} value={pageNumber} onChange={(event) => onChange({ search, pageNumber: Number(event.target.value), pageSize })} /></label>
      <label><span>Số dòng</span><select value={pageSize} onChange={(event) => onChange({ search, pageNumber: 1, pageSize: Number(event.target.value) })}><option value={10}>10</option><option value={20}>20</option><option value={50}>50</option></select></label>
      {extra}
    </div>
  );
}

export function DataTable<T extends Record<string, unknown>>({ rows, columns, actions }: {
  rows: T[];
  columns: { key: keyof T | string; label: string; render?: (row: T) => React.ReactNode }[];
  actions?: (row: T) => React.ReactNode;
}) {
  return (
    <div className="table-wrap">
      <table>
        <thead><tr>{columns.map((column) => <th key={String(column.key)}>{column.label}</th>)}{actions ? <th>Thao tác</th> : null}</tr></thead>
        <tbody>
          {rows.map((row, index) => (
            <tr key={String(row.id || row[columns[0].key] || index)}>
              {columns.map((column) => <td key={String(column.key)}>{column.render ? column.render(row) : String(row[column.key] ?? "-")}</td>)}
              {actions ? <td className="actions">{actions(row)}</td> : null}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function useLoad<T>(loader: () => Promise<T>, deps: React.DependencyList = []) {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function reload() {
    setLoading(true);
    setError(null);
    try {
      setData(await loader());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được dữ liệu.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    reload();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  return { data, loading, error, reload };
}

export function SmartForm<T extends Record<string, unknown>>({ title, fields, initial, submitLabel = "Lưu", onSubmit }: {
  title: string;
  fields: { name: keyof T; label: string; type?: string; options?: SelectOption[]; required?: boolean }[];
  initial: Partial<T>;
  submitLabel?: string;
  onSubmit: (value: Partial<T>) => Promise<void>;
}) {
  const toast = useToast();
  const [value, setValue] = useState<Partial<T>>(initial);
  const [saving, setSaving] = useState(false);

  useEffect(() => setValue(initial), [initial]);

  async function submit(event: FormEvent) {
    event.preventDefault();
    const missing = fields.find((field) => field.required && (value[field.name] === undefined || value[field.name] === null || value[field.name] === ""));
    if (missing) {
      toast(`Vui lòng nhập ${missing.label}.`, "error");
      return;
    }
    setSaving(true);
    try {
      await onSubmit(value);
      toast("Thao tác thành công.", "success");
    } catch (err) {
      const message = err instanceof ApiError ? [err.message, ...err.errors].filter(Boolean).join(" ") : "Thao tác thất bại.";
      toast(message, "error");
    } finally {
      setSaving(false);
    }
  }

  return (
    <form className="card form-grid" onSubmit={submit}>
      <h2>{title}</h2>
      {fields.map((field) => (
        <label key={String(field.name)}>
          <span>{field.label}</span>
          {field.options ? (
            <select value={String(value[field.name] ?? "")} required={field.required} onChange={(event) => setValue((current) => ({ ...current, [field.name]: event.target.value }))}>
              <option value="">Chọn</option>
              {field.options.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
            </select>
          ) : (
            <input type={field.type || "text"} required={field.required} value={String(value[field.name] ?? "")} onChange={(event) => {
              const raw = event.target.value;
              const next = field.type === "number" ? Number(raw) : field.type === "checkbox" ? event.currentTarget.checked : raw;
              setValue((current) => ({ ...current, [field.name]: next }));
            }} />
          )}
        </label>
      ))}
      <button className="primary-btn" disabled={saving}>{saving ? "Đang lưu..." : submitLabel}</button>
    </form>
  );
}

export function useList<T>(source: T[] | { items?: T[]; data?: T[] } | null | undefined) {
  return useMemo(() => unwrapList<T>(source), [source]);
}
