"use client";

import { useState } from "react";
import { Badge, ConfirmDialog, DataTable, Modal, PageHeader, StateBlock, useLoad } from "@/components/ui";
import { paymentsApi } from "@/lib/api/endpoints";
import { useToast } from "@/components/toast";
import { useAuth } from "@/components/auth-provider";
import { ROLES } from "@/lib/auth/constants";
import type { PaymentMethod } from "@/types";

export default function PaymentMethodsPage() {
  const toast = useToast();
  const { hasRole } = useAuth();
  const isAdmin = hasRole(ROLES.ADMIN);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingMethod, setEditingMethod] = useState<PaymentMethod | null>(null);
  const [methodAction, setMethodAction] = useState<PaymentMethod | null>(null);
  const methods = useLoad(() => paymentsApi.methods(), []);

  // Form state
  const [name, setName] = useState("");
  const [code, setCode] = useState("");
  const [note, setNote] = useState("");
  const [isVietQR, setIsVietQR] = useState(false);
  const [bankCode, setBankCode] = useState("MB");
  const [accountNo, setAccountNo] = useState("");
  const [accountName, setAccountName] = useState("");
  const [isActive, setIsActive] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  const openCreateModal = () => {
    setEditingMethod(null);
    setName("");
    setCode("BANK");
    setNote("");
    setIsVietQR(false);
    setBankCode("MB");
    setAccountNo("");
    setAccountName("");
    setIsActive(true);
    setIsModalOpen(true);
  };

  const openEditModal = (row: PaymentMethod) => {
    setEditingMethod(row);
    setName(row.name);
    setCode(row.code);
    setIsActive(row.isActive ?? true);

    let parsedVietQR = false;
    try {
      if (row.description && row.description.startsWith("{")) {
        const parsed = JSON.parse(row.description);
        if (parsed.vietqr || parsed.accountNo) {
          parsedVietQR = true;
          setIsVietQR(true);
          setBankCode(parsed.bankCode || "MB");
          setAccountNo(parsed.accountNo || "");
          setAccountName(parsed.accountName || "");
          setNote(parsed.note || "");
        }
      }
    } catch {}

    if (!parsedVietQR) {
      setIsVietQR(row.code === "BANK" || row.name.toLowerCase().includes("chuyển khoản") || row.name.toLowerCase().includes("qr"));
      setNote(row.description || "");
      setBankCode("MB");
      setAccountNo("989420048989");
      setAccountName("POOLHUB");
    }

    setIsModalOpen(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim() || !code.trim()) {
      toast("Vui lòng nhập đầy đủ tên và mã phương thức.", "error");
      return;
    }

    if (isVietQR && (!accountNo.trim() || !accountName.trim())) {
      toast("Vui lòng nhập số tài khoản và chủ tài khoản cho VietQR.", "error");
      return;
    }

    let finalDescription = note.trim();
    if (isVietQR) {
      finalDescription = JSON.stringify({
        vietqr: true,
        bankCode: bankCode.trim().toUpperCase(),
        accountNo: accountNo.trim(),
        accountName: accountName.trim().toUpperCase(),
        note: note.trim()
      });
    }

    setSubmitting(true);
    try {
      if (editingMethod) {
        await paymentsApi.updateMethod(editingMethod.paymentMethodId, {
          name: name.trim(),
          code: code.trim().toUpperCase(),
          description: finalDescription,
          isActive
        });
        toast("Cập nhật phương thức thanh toán thành công.", "success");
      } else {
        await paymentsApi.createMethod({
          name: name.trim(),
          code: code.trim().toUpperCase(),
          description: finalDescription,
          isActive
        });
        toast("Tạo phương thức thanh toán thành công.", "success");
      }
      setIsModalOpen(false);
      methods.reload();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Thao tác thất bại.", "error");
    } finally {
      setSubmitting(false);
    }
  };

  return <>
    <PageHeader 
      title="Phương thức thanh toán" 
      description="Cấu hình tài khoản VietQR và quản lý trạng thái các cổng thanh toán trong hệ thống."
      action={
        isAdmin ? (
          <button className="primary-btn" onClick={openCreateModal}>
            + Tạo phương thức thanh toán
          </button>
        ) : undefined
      }
    />

    {isModalOpen && (
      <Modal title={editingMethod ? `Sửa phương thức: ${editingMethod.name}` : "Tạo phương thức thanh toán mới"} onClose={() => setIsModalOpen(false)}>
        <form onSubmit={handleSubmit} className="form-stack">
          <div className="form-grid">
            <label>
              <span>Tên phương thức <strong style={{ color: "red" }}>*</strong></span>
              <input value={name} onChange={e => setName(e.target.value)} placeholder="VD: Chuyển khoản VietQR, Tiền mặt..." required autoFocus />
            </label>
            <label>
              <span>Mã phương thức <strong style={{ color: "red" }}>*</strong></span>
              <input value={code} onChange={e => setCode(e.target.value.toUpperCase())} placeholder="VD: BANK, CASH, EWALLET..." required />
            </label>
          </div>

          <div style={{ background: "var(--soft)", padding: "14px", borderRadius: "10px", border: "1px solid var(--line)", margin: "8px 0" }}>
            <label style={{ display: "flex", gap: "10px", alignItems: "center", cursor: "pointer", fontWeight: 600, color: "var(--brand)" }}>
              <input type="checkbox" checked={isVietQR} onChange={e => setIsVietQR(e.target.checked)} style={{ width: "18px", height: "18px" }} />
              <span>⚡ Cấu hình kết nối nhận tiền qua cổng VietQR cho phương thức này</span>
            </label>

            {isVietQR && (
              <div style={{ display: "grid", gap: "12px", marginTop: "14px", paddingTop: "14px", borderTop: "1px dashed var(--line)" }}>
                <div className="form-grid">
                  <label>
                    <span>Ngân hàng thụ hưởng</span>
                    <select value={bankCode} onChange={e => setBankCode(e.target.value)}>
                      <option value="MB">MB - Ngân hàng Quân Đội</option>
                      <option value="VCB">VCB - Vietcombank</option>
                      <option value="TCB">TCB - Techcombank</option>
                      <option value="ICB">ICB - VietinBank</option>
                      <option value="BIDV">BIDV - Ngân hàng ĐT&PT</option>
                      <option value="ACB">ACB - Ngân hàng Á Châu</option>
                      <option value="VPB">VPB - VPBank</option>
                      <option value="TPB">TPB - TPBank</option>
                      <option value="STB">STB - Sacombank</option>
                      <option value="HDB">HDB - HDBank</option>
                      <option value="VIB">VIB - Ngân hàng Quốc Tế</option>
                      <option value="MSB">MSB - Ngân hàng Hàng Hải</option>
                    </select>
                  </label>
                  <label>
                    <span>Số tài khoản <strong style={{ color: "red" }}>*</strong></span>
                    <input value={accountNo} onChange={e => setAccountNo(e.target.value)} placeholder="VD: 989420048989" required={isVietQR} />
                  </label>
                </div>
                <label>
                  <span>Tên chủ tài khoản (Viết hoa không dấu) <strong style={{ color: "red" }}>*</strong></span>
                  <input value={accountName} onChange={e => setAccountName(e.target.value.toUpperCase())} placeholder="VD: POOLHUB" required={isVietQR} />
                </label>
              </div>
            )}
          </div>

          <label>
            <span>Ghi chú mô tả</span>
            <textarea rows={2} value={note} onChange={e => setNote(e.target.value)} placeholder="Mô tả hoặc hướng dẫn thêm..." />
          </label>

          {editingMethod && (
            <label style={{ display: "flex", gap: "8px", alignItems: "center", cursor: "pointer" }}>
              <input type="checkbox" checked={isActive} onChange={e => setIsActive(e.target.checked)} />
              <span>Đang hoạt động (Cho phép sử dụng thanh toán)</span>
            </label>
          )}

          <div className="modal-actions">
            <button type="button" className="ghost-btn" onClick={() => setIsModalOpen(false)}>Hủy</button>
            <button type="submit" className="primary-btn" disabled={submitting}>
              {submitting ? "Đang lưu..." : editingMethod ? "Lưu cấu hình" : "Tạo phương thức"}
            </button>
          </div>
        </form>
      </Modal>
    )}

    <StateBlock loading={methods.loading} error={methods.error} empty={!methods.loading && !methods.data?.length} />
    <DataTable 
      rows={methods.data || []} 
      columns={[
        { key: "code", label: "Mã", render: row => <strong style={{ color: "var(--brand)" }}>{row.code}</strong> }, 
        { key: "name", label: "Tên phương thức", render: row => <span style={{ fontWeight: 600 }}>{row.name}</span> }, 
        { key: "description", label: "Cấu hình / Mô tả", render: row => {
          try {
            if (row.description && row.description.startsWith("{")) {
              const parsed = JSON.parse(row.description);
              if (parsed.vietqr) {
                return (
                  <div style={{ display: "flex", flexDirection: "column", gap: "4px" }}>
                    <div style={{ display: "flex", gap: "6px", alignItems: "center" }}>
                      <span className="badge badge-primary" style={{ background: "#e4f7ec", color: "#0f5d4b", border: "1px solid #c2ebd5", padding: "2px 8px", borderRadius: "6px", fontSize: "12px", fontWeight: 700 }}>
                        ⚡ VietQR: {parsed.bankCode} - {parsed.accountNo}
                      </span>
                    </div>
                    <span style={{ fontSize: "13px", fontWeight: 600, color: "var(--ink)" }}>Chủ TK: {parsed.accountName}</span>
                    {parsed.note && <span style={{ fontSize: "12px", color: "var(--muted)" }}>{parsed.note}</span>}
                  </div>
                );
              }
            }
          } catch {}
          return row.description || <span style={{ color: "var(--muted)" }}>-</span>;
        }},
        { key: "isActive", label: "Trạng thái", render: row => <Badge tone={row.isActive ? "green" : "red"}>{row.isActive ? "Đang hoạt động" : "Ngừng hoạt động"}</Badge> }
      ]} 
      actions={row => (
        <div style={{ display: "flex", gap: "8px" }}>
          {isAdmin ? (
            <button className="secondary-btn compact" onClick={() => openEditModal(row as PaymentMethod)}>⚙️ Cấu hình</button>
          ) : null}
          <button className="ghost-btn compact" onClick={() => setMethodAction(row as PaymentMethod)}>
            {row.isActive ? "Tắt" : "Bật"}
          </button>
        </div>
      )} 
    />

    {methodAction ? (
      <ConfirmDialog 
        title={methodAction.isActive ? "Tắt phương thức thanh toán" : "Bật phương thức thanh toán"} 
        message={`${methodAction.isActive ? "Tắt" : "Bật"} phương thức “${methodAction.name}”?`} 
        confirmLabel="Xác nhận" 
        danger={methodAction.isActive} 
        onCancel={() => setMethodAction(null)} 
        onConfirm={async () => {
          try {
            await paymentsApi.methodStatus(methodAction.paymentMethodId, !methodAction.isActive);
            toast("Đã cập nhật phương thức thanh toán.", "success");
            setMethodAction(null);
            methods.reload();
          } catch (err) {
            toast(err instanceof Error ? err.message : "Không thể cập nhật phương thức.", "error");
          }
        }} 
      />
    ) : null}
  </>;
}
