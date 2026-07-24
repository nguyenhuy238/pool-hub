"use client";

import { FormEvent, useMemo, useState } from "react";
import { Eye, Copy, CheckCircle, XCircle, RefreshCcw, Banknote, WalletCards, AlertTriangle } from "lucide-react";
import { depositRefundApi } from "@/lib/api/endpoints";
import { ApiError, getTotalPages } from "@/lib/api/client";
import { dateTime, money } from "@/lib/status";
import { getRefundMethodLabel, getRefundReasonLabel, getRefundStatusLabel, maskedAccount, refundStatusTone } from "@/lib/refunds";
import { Badge, ConfirmDialog, DataTable, Modal, PageHeader, Pagination, SearchFilterBar, StateBlock, useList, useLoad } from "@/components/ui";
import { useToast } from "@/components/toast";
import { useAuth } from "@/components/auth-provider";
import type { DepositRefundBankInfo, DepositRefundDetail, DepositRefundListItem } from "@/types";

const statusOptions = [
  ["", "Tất cả"],
  ["1", "Chờ khách cung cấp thông tin"],
  ["2", "Chờ duyệt"],
  ["3", "Đã duyệt"],
  ["4", "Đang xử lý"],
  ["5", "Sẵn sàng nhận tiền mặt"],
  ["6", "Thành công"],
  ["7", "Thất bại"],
  ["8", "Bị từ chối"],
  ["9", "Đã hủy"]
];

function apiMessage(err: unknown) {
  if (err instanceof ApiError) {
    if (err.status === 409) return "Dữ liệu đã được người khác cập nhật. Vui lòng tải lại.";
    if (err.status === 403) return "Bạn không có quyền thực hiện thao tác này.";
    return [err.message, ...err.errors].filter(Boolean).join(" ");
  }
  return err instanceof Error ? err.message : "Thao tác thất bại.";
}

export default function DepositRefundsPage() {
  const toast = useToast();
  const { hasAnyRole } = useAuth();
  const isManager = hasAnyRole(["Admin", "Manager"]);
  const canProcessRefund = hasAnyRole(["Admin", "Staff"]);
  const [query, setQuery] = useState({ search: "", status: "", refundMethod: "", reason: "", pageNumber: 1, pageSize: 20 });
  const [selected, setSelected] = useState<DepositRefundDetail | null>(null);
  const [confirm, setConfirm] = useState<{ title: string; message: string; action: () => Promise<void>; danger?: boolean } | null>(null);
  const [rejecting, setRejecting] = useState<DepositRefundDetail | null>(null);
  const [requestingUpdate, setRequestingUpdate] = useState<DepositRefundDetail | null>(null);
  const [bankProcessing, setBankProcessing] = useState<DepositRefundDetail | null>(null);
  const [bankInfo, setBankInfo] = useState<DepositRefundBankInfo | null>(null);
  const [failing, setFailing] = useState<DepositRefundDetail | null>(null);
  const [cashPickup, setCashPickup] = useState<DepositRefundDetail | null>(null);
  const [busy, setBusy] = useState(false);

  const { data, loading, error, reload } = useLoad(() => depositRefundApi.list({
    Search: query.search || undefined,
    Status: query.status ? Number(query.status) : undefined,
    RefundMethod: query.refundMethod ? Number(query.refundMethod) : undefined,
    Reason: query.reason || undefined,
    PageNumber: query.pageNumber,
    PageSize: query.pageSize
  }), [query]);

  const rows = useList<DepositRefundListItem>(data);
  const totalPages = getTotalPages(data, query.pageSize);

  async function run(action: () => Promise<unknown>, success: string) {
    setBusy(true);
    try {
      await action();
      toast(success, "success");
      setSelected(null);
      setBankInfo(null);
      await reload(true);
    } catch (err) {
      toast(apiMessage(err), "error");
    } finally {
      setBusy(false);
    }
  }

  function openDetail(row: DepositRefundListItem) {
    depositRefundApi.detail(row.bookingDepositRefundId)
      .then(setSelected)
      .catch((err) => toast(apiMessage(err), "error"));
  }

  const columns = useMemo(() => [
    { key: "refundCode", label: "Mã hoàn" },
    { key: "bookingCode", label: "Mã booking" },
    { key: "customerName", label: "Khách hàng", render: (row: Record<string, unknown>) => (
      <div>
        <strong>{String(row.customerName || "Khách public")}</strong>
        <div className="table-subtext">{String(row.customerPhoneMasked || "-")} · {String(row.customerEmailMasked || "-")}</div>
      </div>
    ) },
    { key: "amount", label: "Số tiền", render: (row: Record<string, unknown>) => <strong>{money(Number(row.amount || 0))}</strong> },
    { key: "reason", label: "Lý do", render: (row: Record<string, unknown>) => getRefundReasonLabel(String(row.reason || "")) },
    { key: "refundMethod", label: "Phương thức", render: (row: Record<string, unknown>) => getRefundMethodLabel(Number(row.refundMethod || 0)) },
    { key: "status", label: "Trạng thái", render: (row: Record<string, unknown>) => <Badge tone={refundStatusTone[Number(row.status) as keyof typeof refundStatusTone] || "neutral"}>{getRefundStatusLabel(Number(row.status))}</Badge> },
    { key: "createdAtUtc", label: "Ngày tạo", render: (row: Record<string, unknown>) => dateTime(String(row.createdAtUtc || "")) }
  ], []);

  return (
    <>
      <PageHeader title="Hoàn cọc" description="Theo dõi, duyệt và xử lý các yêu cầu hoàn tiền cọc." />
      <SearchFilterBar>
        <label><span>Tìm kiếm</span><input value={query.search} onChange={(e) => setQuery({ ...query, search: e.target.value, pageNumber: 1 })} placeholder="Mã hoàn, booking, email, số điện thoại" /></label>
        <label><span>Trạng thái</span><select value={query.status} onChange={(e) => setQuery({ ...query, status: e.target.value, pageNumber: 1 })}>{statusOptions.map(([value, text]) => <option key={value} value={value}>{text}</option>)}</select></label>
        <label><span>Phương thức</span><select value={query.refundMethod} onChange={(e) => setQuery({ ...query, refundMethod: e.target.value, pageNumber: 1 })}><option value="">Tất cả</option><option value="1">Chuyển khoản</option><option value="2">Tiền mặt</option></select></label>
        <label><span>Số dòng</span><select value={query.pageSize} onChange={(e) => setQuery({ ...query, pageSize: Number(e.target.value), pageNumber: 1 })}><option value={10}>10</option><option value={20}>20</option><option value={50}>50</option></select></label>
      </SearchFilterBar>
      <StateBlock loading={loading} error={error} empty={!loading && rows.length === 0} />
      <DataTable
        rows={rows as unknown as Record<string, unknown>[]}
        columns={columns}
        actions={(row) => <button className="ghost-btn compact" onClick={() => openDetail(row as unknown as DepositRefundListItem)}><Eye size={14} /> Chi tiết</button>}
      />
      <Pagination pageNumber={query.pageNumber} totalPages={totalPages} onChange={(pageNumber) => setQuery({ ...query, pageNumber })} />

      {selected ? (
        <Modal title="Chi tiết hoàn cọc" size="large" onClose={() => { setSelected(null); setBankInfo(null); }}>
          <RefundDetail refund={selected} />
          <div className="refund-next-panel">
            <div>
              <h2>Hành động tiếp theo</h2>
              <p>{getNextActionHint(selected)}</p>
            </div>
            {selected.status === 1 ? (
              <div className="inline-alert warning">
                Yêu cầu này đang chờ khách mở link hoàn cọc, xác minh email và chọn phương thức nhận tiền. Sau khi khách gửi thông tin, trạng thái sẽ chuyển sang “Chờ duyệt” và Manager sẽ thấy nút Duyệt/Từ chối.
              </div>
            ) : null}
            <div className="refund-action-row">
              {isManager && selected.status === 2 ? (
                <>
                  <button className="primary-btn" onClick={() => setConfirm({ title: "Duyệt hoàn cọc", message: `Xác nhận duyệt ${selected.refundCode} với số tiền ${money(selected.amount)}?`, action: () => run(() => depositRefundApi.approve(selected.bookingDepositRefundId), "Đã duyệt yêu cầu hoàn cọc.") })}><CheckCircle size={16} /> Duyệt</button>
                  <button className="danger-btn" onClick={() => setRejecting(selected)}><XCircle size={16} /> Từ chối</button>
                  <button className="ghost-btn" onClick={() => setRequestingUpdate(selected)}><RefreshCcw size={16} /> Yêu cầu cập nhật</button>
                </>
              ) : null}
              {canProcessRefund && selected.status === 3 && selected.refundMethod === 1 ? (
                <button className="primary-btn" onClick={() => run(() => depositRefundApi.markProcessing(selected.bookingDepositRefundId), "Đã chuyển sang trạng thái đang xử lý.")}><Banknote size={16} /> Bắt đầu xử lý</button>
              ) : null}
              {canProcessRefund && selected.status === 4 && selected.refundMethod === 1 ? (
                <>
                  <button className="secondary-btn" onClick={() => depositRefundApi.bankInfo(selected.bookingDepositRefundId).then(setBankInfo).catch((err) => toast(apiMessage(err), "error"))}><Eye size={16} /> Xem thông tin chuyển khoản</button>
                  <button className="primary-btn" onClick={() => setBankProcessing(selected)}><CheckCircle size={16} /> Hoàn tất chuyển khoản</button>
                  <button className="danger-btn" onClick={() => setFailing(selected)}><XCircle size={16} /> Thất bại</button>
                </>
              ) : null}
              {canProcessRefund && selected.status === 3 && selected.refundMethod === 2 ? (
                <button className="primary-btn" onClick={() => setConfirm({ title: "Chuẩn bị tiền mặt", message: `Xác nhận chuẩn bị ${money(selected.amount)} để khách đến nhận?`, action: () => run(() => depositRefundApi.prepareCashPickup(selected.bookingDepositRefundId), "Đã chuẩn bị tiền mặt và gửi mã cho khách.") })}><WalletCards size={16} /> Chuẩn bị tiền mặt</button>
              ) : null}
              {canProcessRefund && selected.status === 5 && selected.refundMethod === 2 ? (
                <button className="primary-btn" onClick={() => setCashPickup(selected)}><CheckCircle size={16} /> Xác nhận khách đã nhận</button>
              ) : null}
            </div>
          </div>
          {bankInfo ? <BankInfoPanel info={bankInfo} /> : null}
        </Modal>
      ) : null}

      {confirm ? <ConfirmDialog title={confirm.title} message={confirm.message} danger={confirm.danger} busy={busy} onCancel={() => setConfirm(null)} onConfirm={async () => { await confirm.action(); setConfirm(null); }} /> : null}
      {rejecting ? <ReasonModal title="Từ chối hoàn cọc" label="Lý do từ chối" submitLabel="Từ chối" danger onClose={() => setRejecting(null)} onSubmit={(reason) => run(() => depositRefundApi.reject(rejecting.bookingDepositRefundId, { reason }), "Đã từ chối yêu cầu hoàn cọc.").then(() => setRejecting(null))} /> : null}
      {requestingUpdate ? <ReasonModal title="Yêu cầu khách cập nhật" label="Ghi chú cần cập nhật" submitLabel="Gửi yêu cầu" onClose={() => setRequestingUpdate(null)} onSubmit={(reason) => run(() => depositRefundApi.requestCustomerUpdate(requestingUpdate.bookingDepositRefundId, { reason }), "Đã gửi link cập nhật mới cho khách.").then(() => setRequestingUpdate(null))} /> : null}
      {failing ? <ReasonModal title="Đánh dấu hoàn tiền thất bại" label="Nguyên nhân thất bại" submitLabel="Xác nhận thất bại" danger onClose={() => setFailing(null)} onSubmit={(reason) => run(() => depositRefundApi.markFailed(failing.bookingDepositRefundId, { reason }), "Đã ghi nhận hoàn tiền thất bại.").then(() => setFailing(null))} /> : null}
      {bankProcessing ? <CompleteBankModal refund={bankProcessing} busy={busy} onClose={() => setBankProcessing(null)} onSubmit={(payload) => run(() => depositRefundApi.completeBankTransfer(bankProcessing.bookingDepositRefundId, payload), "Đã xác nhận hoàn tất chuyển khoản.").then(() => setBankProcessing(null))} /> : null}
      {cashPickup ? <CompleteCashModal refund={cashPickup} busy={busy} onClose={() => setCashPickup(null)} onSubmit={(payload) => run(() => depositRefundApi.completeCashPickup(cashPickup.bookingDepositRefundId, payload), "Đã xác nhận khách nhận tiền mặt.").then(() => setCashPickup(null))} /> : null}
    </>
  );
}

function RefundDetail({ refund }: { refund: DepositRefundDetail }) {
  return (
    <div className="refund-action-grid">
      <div className="refund-detail-head">
        <div>
          <h3>{refund.refundCode}</h3>
          <p className="muted-text">{refund.bookingCode} · {getRefundReasonLabel(refund.reason)}</p>
        </div>
        <Badge tone={refundStatusTone[refund.status]}>{getRefundStatusLabel(refund.status)}</Badge>
      </div>
      <div className="refund-detail-grid">
        <Info label="Khách hàng" value={refund.customerName || "Khách public"} />
        <Info label="Email" value={refund.customerEmail || refund.customerEmailMasked || "-"} />
        <Info label="Điện thoại" value={refund.customerPhone || refund.customerPhoneMasked || "-"} />
        <Info label="Số tiền yêu cầu hoàn" value={money(refund.amount)} strong />
        <Info label="Phương thức" value={getRefundMethodLabel(refund.refundMethod)} />
        <Info label="Tài khoản" value={maskedAccount(refund.bankAccountLast4)} />
        <Info label="Mã giao dịch hoàn" value={refund.manualTransferCode || "-"} />
        <Info label="Ghi chú" value={refund.note || refund.rejectReason || refund.failureReason || "-"} />
      </div>
      <div className="card">
        <h2>Timeline</h2>
        <div className="refund-timeline">
          {buildTimeline(refund).map((item) => <Timeline key={item.label} {...item} />)}
        </div>
      </div>
    </div>
  );
}

function BankInfoPanel({ info }: { info: DepositRefundBankInfo }) {
  const toast = useToast();
  const copy = (value?: string) => {
    if (!value) return;
    navigator.clipboard.writeText(value).then(() => toast("Đã copy.", "success")).catch(() => toast("Không thể copy.", "error"));
  };
  return (
    <div className="inline-alert warning" style={{ marginTop: 14 }}>
      <strong><AlertTriangle size={16} /> Dữ liệu nhạy cảm.</strong> Hành động xem thông tin đã được ghi audit.
      <div className="refund-detail-grid" style={{ marginTop: 12 }}>
        <Info label="Ngân hàng" value={`${info.bankName || "-"} (${info.bankCode || "-"})`} />
        <Info label="Số tài khoản" value={info.accountNumber || "-"} />
        <Info label="Tên chủ tài khoản" value={info.accountHolderName || "-"} />
      </div>
      <div className="refund-action-row" style={{ marginTop: 10 }}>
        <button className="ghost-btn compact" onClick={() => copy(info.accountNumber)}><Copy size={14} /> Copy STK</button>
        <button className="ghost-btn compact" onClick={() => copy(info.accountHolderName)}><Copy size={14} /> Copy tên</button>
      </div>
    </div>
  );
}

function ReasonModal({ title, label, submitLabel, danger, onClose, onSubmit }: { title: string; label: string; submitLabel: string; danger?: boolean; onClose: () => void; onSubmit: (reason: string) => Promise<void> }) {
  const toast = useToast();
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState(false);
  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!reason.trim()) {
      toast(`Vui lòng nhập ${label.toLowerCase()}.`, "error");
      return;
    }
    setBusy(true);
    await onSubmit(reason.trim()).finally(() => setBusy(false));
  }
  return (
    <Modal title={title} onClose={onClose} size="small">
      <form className="form-stack" onSubmit={submit}>
        <label><span>{label}</span><textarea rows={4} value={reason} onChange={(e) => setReason(e.target.value)} /></label>
        <div className="modal-actions"><button className="ghost-btn" type="button" onClick={onClose}>Hủy</button><button className={danger ? "danger-btn" : "primary-btn"} disabled={busy}>{busy ? "Đang xử lý..." : submitLabel}</button></div>
      </form>
    </Modal>
  );
}

function CompleteBankModal({ refund, busy, onClose, onSubmit }: { refund: DepositRefundDetail; busy: boolean; onClose: () => void; onSubmit: (payload: { manualTransferCode: string; note?: string }) => Promise<void> }) {
  const toast = useToast();
  const [manualTransferCode, setManualTransferCode] = useState("");
  const [note, setNote] = useState("");
  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!manualTransferCode.trim()) {
      toast("Vui lòng nhập mã giao dịch hoàn tiền.", "error");
      return;
    }
    await onSubmit({ manualTransferCode: manualTransferCode.trim(), note: note.trim() || undefined });
  }
  return (
    <Modal title="Hoàn tất chuyển khoản" onClose={onClose} size="small">
      <form className="form-stack" onSubmit={submit}>
        <div className="inline-alert warning">Chỉ xác nhận sau khi tiền đã thực sự được chuyển cho khách. Sau khi xác nhận, số tiền {money(refund.amount)} sẽ được ghi nhận là đã hoàn.</div>
        <label><span>Mã giao dịch hoàn tiền</span><input value={manualTransferCode} onChange={(e) => setManualTransferCode(e.target.value)} /></label>
        <label><span>Ghi chú</span><textarea rows={3} value={note} onChange={(e) => setNote(e.target.value)} /></label>
        <div className="modal-actions"><button className="ghost-btn" type="button" onClick={onClose}>Hủy</button><button className="primary-btn" disabled={busy}>Xác nhận hoàn tất</button></div>
      </form>
    </Modal>
  );
}

function CompleteCashModal({ refund, busy, onClose, onSubmit }: { refund: DepositRefundDetail; busy: boolean; onClose: () => void; onSubmit: (payload: { cashPickupCode: string; bookingCode: string; phoneLast4: string; note?: string }) => Promise<void> }) {
  const toast = useToast();
  const [cashPickupCode, setCashPickupCode] = useState("");
  const [bookingCode, setBookingCode] = useState(refund.bookingCode || "");
  const [phoneLast4, setPhoneLast4] = useState("");
  const [note, setNote] = useState("");
  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!cashPickupCode.trim() || !bookingCode.trim() || !/^\d{4}$/.test(phoneLast4)) {
      toast("Vui lòng nhập mã nhận tiền, mã booking và 4 số cuối điện thoại.", "error");
      return;
    }
    await onSubmit({ cashPickupCode: cashPickupCode.trim(), bookingCode: bookingCode.trim(), phoneLast4, note: note.trim() || undefined });
  }
  return (
    <Modal title="Xác nhận nhận tiền mặt" onClose={onClose} size="small">
      <form className="form-stack" onSubmit={submit}>
        <div className="inline-alert warning">Chỉ xác nhận sau khi khách đã thực sự nhận đủ {money(refund.amount)} tiền mặt.</div>
        <label><span>Mã nhận tiền</span><input autoComplete="one-time-code" value={cashPickupCode} onChange={(e) => setCashPickupCode(e.target.value.replace(/\D/g, "").slice(0, 6))} /></label>
        <label><span>Mã booking</span><input value={bookingCode} onChange={(e) => setBookingCode(e.target.value)} /></label>
        <label><span>4 số cuối điện thoại</span><input inputMode="numeric" value={phoneLast4} onChange={(e) => setPhoneLast4(e.target.value.replace(/\D/g, "").slice(0, 4))} /></label>
        <label><span>Ghi chú</span><textarea rows={3} value={note} onChange={(e) => setNote(e.target.value)} /></label>
        <div className="modal-actions"><button className="ghost-btn" type="button" onClick={onClose}>Hủy</button><button className="primary-btn" disabled={busy}>Xác nhận đã trả tiền</button></div>
      </form>
    </Modal>
  );
}

function Info({ label, value, strong = false }: { label: string; value?: string; strong?: boolean }) {
  return <div className="refund-info-row"><span>{label}</span>{strong ? <strong>{value || "-"}</strong> : <b>{value || "-"}</b>}</div>;
}

function Timeline({ label, value, state, description }: { label: string; value?: string; state: "done" | "current" | "pending" | "failed"; description?: string }) {
  return (
    <div className={`refund-timeline-item ${state}`}>
      <i />
      <span>
        <strong>{label}</strong>
        <small>{value || description || "Chưa đến bước này"}</small>
      </span>
    </div>
  );
}

function buildTimeline(refund: DepositRefundDetail) {
  const isTerminalFailed = [7, 8, 9].includes(refund.status);
  return [
    {
      label: "Tạo yêu cầu",
      value: dateTime(refund.createdAtUtc),
      state: "done" as const
    },
    {
      label: "Khách gửi thông tin",
      value: refund.status > 1 ? "Đã gửi thông tin nhận hoàn" : undefined,
      state: refund.status > 1 ? "done" as const : refund.status === 1 ? "current" as const : "pending" as const,
      description: "Chờ khách xác minh và chọn phương thức nhận tiền"
    },
    {
      label: "Manager duyệt",
      value: dateTime(refund.approvedAtUtc),
      state: refund.approvedAtUtc ? "done" as const : refund.status === 2 ? "current" as const : isTerminalFailed ? "failed" as const : "pending" as const,
      description: "Chờ Manager duyệt hoặc từ chối"
    },
    {
      label: refund.refundMethod === 2 ? "Chuẩn bị tiền mặt" : "Staff xử lý",
      value: refund.status === 5 ? "Sẵn sàng nhận tiền mặt" : dateTime(refund.processingAtUtc),
      state: refund.processingAtUtc || refund.status === 5 ? "done" as const : [3, 4].includes(refund.status) ? "current" as const : isTerminalFailed ? "failed" as const : "pending" as const,
      description: refund.refundMethod === 2 ? "Chờ Staff chuẩn bị và xác nhận nhận tiền mặt" : "Chờ Staff chuyển khoản và nhập mã giao dịch"
    },
    {
      label: "Hoàn tất",
      value: dateTime(refund.succeededAtUtc),
      state: refund.status === 6 ? "done" as const : isTerminalFailed ? "failed" as const : "pending" as const,
      description: isTerminalFailed ? getRefundStatusLabel(refund.status) : "Chỉ hoàn tất khi tiền đã trả thật cho khách"
    }
  ];
}

function getNextActionHint(refund: DepositRefundDetail) {
  if (refund.status === 1) return "Chưa cần thao tác nội bộ. Khách cần mở link email, nhập mã xác minh và chọn cách nhận tiền.";
  if (refund.status === 2) return "Manager kiểm tra thông tin và chọn Duyệt, Từ chối hoặc yêu cầu khách cập nhật.";
  if (refund.status === 3 && refund.refundMethod === 1) return "Staff bắt đầu xử lý, xem thông tin chuyển khoản khi cần, rồi chuyển tiền ngoài hệ thống.";
  if (refund.status === 3 && refund.refundMethod === 2) return "Staff chuẩn bị tiền mặt để hệ thống gửi mã nhận tiền cho khách.";
  if (refund.status === 4) return "Staff chỉ bấm Hoàn tất chuyển khoản sau khi đã chuyển tiền thật cho khách.";
  if (refund.status === 5) return "Khách đến quầy, Staff nhập mã nhận tiền, mã booking và 4 số cuối điện thoại để hoàn tất.";
  if (refund.status === 6) return "Yêu cầu đã hoàn tất. RefundedAmount đã được ghi nhận.";
  if ([7, 8, 9].includes(refund.status)) return "Yêu cầu đã kết thúc, không còn hành động tiếp theo.";
  return "Không có hành động phù hợp ở trạng thái hiện tại.";
}
