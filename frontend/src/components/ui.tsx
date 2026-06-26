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
  if (loading) return <div className="state-card loading-state"><span className="spinner" />Đang tải dữ liệu...</div>;
  if (error) return <div className="state-card error">{error}</div>;
  if (empty) return <div className="state-card">Chưa có dữ liệu phù hợp.</div>;
  return null;
}

export function Modal({ title, children, onClose, size = "medium" }: {
  title: string;
  children: React.ReactNode;
  onClose: () => void;
  size?: "small" | "medium" | "large";
}) {
  useEffect(() => {
    const close = (event: KeyboardEvent) => event.key === "Escape" && onClose();
    window.addEventListener("keydown", close);
    return () => window.removeEventListener("keydown", close);
  }, [onClose]);

  return (
    <div className="modal-backdrop" role="presentation" onMouseDown={(event) => event.target === event.currentTarget && onClose()}>
      <section className={`modal-card modal-${size}`} role="dialog" aria-modal="true" aria-label={title}>
        <div className="modal-head"><h2>{title}</h2><button className="icon-btn" type="button" onClick={onClose} aria-label="Đóng">×</button></div>
        {children}
      </section>
    </div>
  );
}

export function ConfirmDialog({ title, message, confirmLabel = "Xác nhận", danger = false, busy = false, onConfirm, onCancel }: {
  title: string;
  message: string;
  confirmLabel?: string;
  danger?: boolean;
  busy?: boolean;
  onConfirm: () => void | Promise<void>;
  onCancel: () => void;
}) {
  return (
    <Modal title={title} onClose={onCancel} size="small">
      <p className="modal-message">{message}</p>
      <div className="modal-actions">
        <button className="ghost-btn" type="button" onClick={onCancel} disabled={busy}>Hủy</button>
        <button className={danger ? "danger-btn" : "primary-btn"} type="button" onClick={onConfirm} disabled={busy}>{busy ? "Đang xử lý..." : confirmLabel}</button>
      </div>
    </Modal>
  );
}

const sensitiveKeyPattern = /(password|token|secret|hash|authorization|cookie)/i;

function maskSensitive(value: unknown): unknown {
  if (Array.isArray(value)) return value.map(maskSensitive);
  if (!value || typeof value !== "object") return value;

  return Object.fromEntries(Object.entries(value).map(([key, item]) => [
    key,
    sensitiveKeyPattern.test(key) ? "***" : maskSensitive(item)
  ]));
}

export function JsonPreview({ value }: { value?: string }) {
  if (!value) return <span className="muted-text">Không có dữ liệu</span>;
  try {
    return <pre className="json-preview">{JSON.stringify(maskSensitive(JSON.parse(value)), null, 2)}</pre>;
  } catch {
    return <pre className="json-preview">{value}</pre>;
  }
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
      <label><span>Số dòng</span><select value={pageSize} onChange={(event) => onChange({ search, pageNumber: 1, pageSize: Number(event.target.value) })}><option value={10}>10</option><option value={20}>20</option><option value={50}>50</option></select></label>
      {extra}
    </div>
  );
}

export function SearchFilterBar({ children }: { children: React.ReactNode }) {
  return <div className="card filter-grid">{children}</div>;
}

export function Pagination({ pageNumber, totalPages = 1, onChange }: {
  pageNumber: number;
  totalPages?: number;
  onChange: (page: number) => void;
}) {
  totalPages = Math.max(1, Math.floor(Number(totalPages) || 1));
  pageNumber = Math.min(Math.max(1, pageNumber), totalPages);
  if (totalPages <= 1) return null;

  const getPageNumbers = () => {
    const pages: (number | string)[] = [];
    if (totalPages <= 7) {
      for (let i = 1; i <= totalPages; i++) pages.push(i);
    } else {
      if (pageNumber <= 4) {
        pages.push(1, 2, 3, 4, 5, '...', totalPages);
      } else if (pageNumber >= totalPages - 3) {
        pages.push(1, '...', totalPages - 4, totalPages - 3, totalPages - 2, totalPages - 1, totalPages);
      } else {
        pages.push(1, '...', pageNumber - 1, pageNumber, pageNumber + 1, '...', totalPages);
      }
    }
    return pages;
  };

  return (
    <div className="pagination" style={{ display: 'flex', gap: '6px', alignItems: 'center', marginTop: '16px', justifyContent: 'center' }}>
      <button 
        style={{ padding: '6px 12px', border: '1px solid #dce7e2', borderRadius: '4px', background: 'white', cursor: pageNumber === 1 ? 'not-allowed' : 'pointer', color: pageNumber === 1 ? '#aaa' : '#333' }}
        disabled={pageNumber === 1} 
        onClick={() => onChange(pageNumber - 1)}
      >
        &lt;
      </button>
      {getPageNumbers().map((p, i) => (
        <button 
          key={i} 
          style={{ 
            padding: '6px 12px', 
            border: p === '...' ? 'none' : '1px solid #dce7e2', 
            borderRadius: '4px', 
            background: p === pageNumber ? '#0f5d4b' : 'white', 
            color: p === pageNumber ? 'white' : '#333',
            cursor: p === '...' ? 'default' : 'pointer',
            fontWeight: p === pageNumber ? 'bold' : 'normal'
          }}
          disabled={p === '...'}
          onClick={() => typeof p === 'number' && onChange(p)}
        >
          {p}
        </button>
      ))}
      <button 
        style={{ padding: '6px 12px', border: '1px solid #dce7e2', borderRadius: '4px', background: 'white', cursor: pageNumber === totalPages ? 'not-allowed' : 'pointer', color: pageNumber === totalPages ? '#aaa' : '#333' }}
        disabled={pageNumber === totalPages} 
        onClick={() => onChange(pageNumber + 1)}
      >
        &gt;
      </button>
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
        <thead><tr>{columns.map((column) => <th key={String(column.key)}>{column.label}</th>)}{actions ? <th style={{ width: 160, minWidth: 160 }}>Thao tác</th> : null}</tr></thead>
        <tbody>
          {rows.map((row, index) => (
            <tr key={String(row.id || row[columns[0].key] || index)}>
              {columns.map((column) => <td key={String(column.key)}>{column.render ? column.render(row) : String(row[column.key] ?? "-")}</td>)}
              {actions ? <td style={{ width: 160, minWidth: 160, verticalAlign: "middle" }}>{actions(row)}</td> : null}
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

export function useDebouncedValue<T>(value: T, delayMs = 350) {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(value), delayMs);
    return () => window.clearTimeout(timer);
  }, [value, delayMs]);

  return debounced;
}

export function SmartForm<T extends Record<string, unknown>>({ title, fields, initial, submitLabel = "Lưu", onSubmit }: {
  title: string;
  fields: { name: keyof T; label: string; type?: string; options?: SelectOption[]; required?: boolean; step?: string | number }[];
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
            <input type={field.type || "text"} step={field.step} required={field.required} value={String(value[field.name] ?? "")} onChange={(event) => {
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
