"use client";

import { FormEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { customerApi } from "@/lib/api/endpoints";
import { getTotalPages } from "@/lib/api/client";
import { Badge, ConfirmDialog, DataTable, Modal, PageHeader, Pagination, SearchFilterBar, StateBlock, useDebouncedValue } from "@/components/ui";
import { useToast } from "@/components/toast";
import { dateTime } from "@/lib/status";
import type { CustomerDto } from "@/types";

type CustomerForm = { fullName: string; phoneNumber: string; email: string; note: string };

const emptyForm: CustomerForm = { fullName: "", phoneNumber: "", email: "", note: "" };
const phonePattern = /^[0-9+\-\s().]{8,20}$/;

export default function CustomersPage() {
  const router = useRouter();
  const toast = useToast();
  const [params, setParams] = useState({ search: "", status: "", pageNumber: 1, pageSize: 20 });
  const debouncedSearch = useDebouncedValue(params.search, 350);
  const [data, setData] = useState<{ items?: CustomerDto[]; totalPages?: number; totalItems?: number; totalCount?: number; pageSize?: number } | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [creating, setCreating] = useState(false);
  const [statusAction, setStatusAction] = useState<CustomerDto | null>(null);
  const rows = data?.items ?? [];

  async function load() {
    setLoading(true);
    setError("");
    try {
      setData(await customerApi.list({
        search: debouncedSearch || undefined,
        status: params.status === "true" ? true : params.status === "false" ? false : undefined,
        pageNumber: params.pageNumber,
        pageSize: params.pageSize
      }) as typeof data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được khách hàng.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch, params.status, params.pageNumber, params.pageSize]);

  async function toggleStatus() {
    if (!statusAction) return;
    try {
      await customerApi.updateStatus(statusAction.customerId, !statusAction.status);
      toast(statusAction.status ? "Đã khóa khách hàng." : "Đã mở khóa khách hàng.", "success");
      setStatusAction(null);
      await load();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể cập nhật trạng thái.", "error");
    }
  }

  return <>
    <PageHeader title="Quản lý khách hàng" description="Quản lý thông tin và lịch sử đặt bàn của khách hàng." action={<button className="primary-btn" onClick={() => setCreating(true)}>Tạo mới</button>} />
    <SearchFilterBar>
      <label><span>Tìm kiếm</span><input value={params.search} onChange={(event) => setParams({ ...params, search: event.target.value, pageNumber: 1 })} placeholder="Tên hoặc số điện thoại" /></label>
      <label><span>Trạng thái</span><select value={params.status} onChange={(event) => setParams({ ...params, status: event.target.value, pageNumber: 1 })}><option value="">Tất cả</option><option value="true">Đang hoạt động</option><option value="false">Đã khóa</option></select></label>
      <label><span>Số dòng</span><select value={params.pageSize} onChange={(event) => setParams({ ...params, pageSize: Number(event.target.value), pageNumber: 1 })}><option>10</option><option>20</option><option>50</option></select></label>
    </SearchFilterBar>
    <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
    {!loading && rows.length ? <>
      <DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[
        { key: "fullName", label: "Họ và tên", render: (row) => <strong>{String(row.fullName)}</strong> },
        { key: "phoneNumber", label: "Số điện thoại", render: (row) => String(row.phoneNumber || "-") },
        { key: "email", label: "Email", render: (row) => String(row.email || "-") },
        { key: "totalBookings", label: "Số lần đặt bàn", render: (row) => <strong>{row.totalBookings as number}</strong> },
        { key: "status", label: "Trạng thái", render: (row) => <Badge tone={row.status ? "green" : "red"}>{row.status ? "Đang hoạt động" : "Đã khóa"}</Badge> },
        { key: "createdAtUtc", label: "Ngày tham gia", render: (row) => dateTime(String(row.createdAtUtc)) }
      ]} actions={(row) => {
        const customer = row as unknown as CustomerDto;
        return <div className="action-group">
          <button className="ghost-btn compact" onClick={() => router.push(`/management/customers/${customer.customerId}`)}>Chi tiết / Sửa</button>
          <button className={customer.status ? "danger-btn compact" : "ghost-btn compact"} onClick={() => setStatusAction(customer)}>{customer.status ? "Khóa" : "Mở khóa"}</button>
        </div>;
      }} />
      <Pagination pageNumber={params.pageNumber} totalPages={getTotalPages(data, params.pageSize)} onChange={(pageNumber) => setParams({ ...params, pageNumber })} />
    </> : null}
    {creating ? <CustomerFormModal onClose={() => setCreating(false)} onSaved={async () => { setCreating(false); await load(); }} /> : null}
    {statusAction ? <ConfirmDialog title={statusAction.status ? "Khóa khách hàng" : "Mở khóa khách hàng"} message={`${statusAction.status ? "Khóa" : "Mở khóa"} khách hàng “${statusAction.fullName}”?`} confirmLabel="Xác nhận" danger={statusAction.status} onCancel={() => setStatusAction(null)} onConfirm={toggleStatus} /> : null}
  </>;
}

function CustomerFormModal({ onClose, onSaved }: { onClose: () => void; onSaved: () => Promise<void> }) {
  const toast = useToast();
  const [form, setForm] = useState<CustomerForm>(emptyForm);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!form.fullName.trim()) return setError("Vui lòng nhập họ tên.");
    if (!form.phoneNumber.trim() || !phonePattern.test(form.phoneNumber.trim())) return setError("Số điện thoại không hợp lệ.");
    if (form.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) return setError("Email không hợp lệ.");
    setSaving(true);
    setError("");
    try {
      await customerApi.create({ fullName: form.fullName.trim(), phoneNumber: form.phoneNumber.trim(), email: form.email.trim() || undefined, note: form.note.trim() || undefined });
      toast("Đã tạo khách hàng.", "success");
      await onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể tạo khách hàng.");
    } finally {
      setSaving(false);
    }
  }
  return <Modal title="Tạo khách hàng" onClose={onClose}>
    <form className="form-stack" onSubmit={submit}>
      {error ? <div className="inline-alert error">{error}</div> : null}
      <label><span>Họ và tên</span><input value={form.fullName} onChange={(event) => setForm({ ...form, fullName: event.target.value })} /></label>
      <label><span>Số điện thoại</span><input value={form.phoneNumber} onChange={(event) => setForm({ ...form, phoneNumber: event.target.value })} /></label>
      <label><span>Email</span><input type="email" value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} /></label>
      <label><span>Ghi chú</span><textarea rows={3} value={form.note} onChange={(event) => setForm({ ...form, note: event.target.value })} /></label>
      <div className="modal-actions"><button type="button" className="ghost-btn" onClick={onClose}>Hủy</button><button className="primary-btn" disabled={saving}>{saving ? "Đang lưu..." : "Lưu"}</button></div>
    </form>
  </Modal>;
}
