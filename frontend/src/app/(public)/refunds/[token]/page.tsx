"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { publicDepositRefundApi } from "@/lib/api/endpoints";
import { ApiError } from "@/lib/api/client";
import { money, dateTime } from "@/lib/status";
import { getRefundMethodLabel, getRefundReasonLabel, getRefundStatusLabel, maskedAccount } from "@/lib/refunds";
import { Badge, StateBlock } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { PublicDepositRefund } from "@/types";

type PageProps = { params: { token: string } };

const bankOptions = [
  { code: "VCB", name: "Vietcombank" },
  { code: "TCB", name: "Techcombank" },
  { code: "BIDV", name: "BIDV" },
  { code: "ACB", name: "ACB" },
  { code: "MB", name: "MB Bank" },
  { code: "VPB", name: "VPBank" },
  { code: "OTHER", name: "Ngân hàng khác" }
];

function friendlyError(err: unknown) {
  if (err instanceof ApiError) {
    if (err.status === 401) return "Liên kết hoặc mã xác minh không hợp lệ hoặc đã hết hạn.";
    if (err.status === 404) return "Không tìm thấy yêu cầu hoàn cọc. Vui lòng kiểm tra lại liên kết.";
    if (err.status === 409) return "Yêu cầu hoàn cọc đã được cập nhật. Vui lòng tải lại trang.";
    return [err.message, ...err.errors].filter(Boolean).join(" ");
  }
  return err instanceof Error ? err.message : "Không thể xử lý yêu cầu.";
}

export default function PublicRefundPage({ params }: PageProps) {
  const token = params.token;
  const toast = useToast();
  const [refund, setRefund] = useState<PublicDepositRefund | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [countdown, setCountdown] = useState(0);
  const [verificationCode, setVerificationCode] = useState("");
  const [phoneLast4, setPhoneLast4] = useState("");
  const [method, setMethod] = useState<"BankTransfer" | "CashAtVenue">("BankTransfer");
  const [bankCode, setBankCode] = useState("VCB");
  const [bankName, setBankName] = useState("Vietcombank");
  const [accountNumber, setAccountNumber] = useState("");
  const [confirmAccountNumber, setConfirmAccountNumber] = useState("");
  const [accountHolderName, setAccountHolderName] = useState("");

  const canSubmitMethod = refund?.status === 1 && refund.isVerified;

  useEffect(() => {
    let active = true;
    publicDepositRefundApi.get(token)
      .then((data) => active && setRefund(data))
      .catch((err) => active && setError(friendlyError(err)))
      .finally(() => active && setLoading(false));
    return () => { active = false; };
  }, [token]);

  useEffect(() => {
    if (countdown <= 0) return;
    const timer = window.setTimeout(() => setCountdown((value) => Math.max(0, value - 1)), 1000);
    return () => window.clearTimeout(timer);
  }, [countdown]);

  useEffect(() => {
    const bank = bankOptions.find((item) => item.code === bankCode);
    if (bank) setBankName(bank.name);
  }, [bankCode]);

  const accountError = useMemo(() => {
    if (method !== "BankTransfer") return "";
    if (!accountNumber || !confirmAccountNumber || !accountHolderName.trim()) return "Vui lòng nhập đầy đủ thông tin tài khoản.";
    if (!/^[0-9A-Za-z.-]{4,32}$/.test(accountNumber)) return "Số tài khoản chỉ nên gồm chữ, số, dấu chấm hoặc gạch ngang.";
    if (accountNumber !== confirmAccountNumber) return "Hai số tài khoản không khớp.";
    return "";
  }, [accountHolderName, accountNumber, confirmAccountNumber, method]);

  async function sendCode() {
    setBusy(true);
    try {
      await publicDepositRefundApi.sendVerificationCode(token);
      setCountdown(60);
      toast("Đã gửi mã xác minh tới email booking.", "success");
    } catch (err) {
      toast(friendlyError(err), "error");
    } finally {
      setBusy(false);
    }
  }

  async function verify(event: FormEvent) {
    event.preventDefault();
    if (!/^\d{6}$/.test(verificationCode.trim())) {
      toast("Mã xác minh phải gồm 6 chữ số.", "error");
      return;
    }
    if (!/^\d{4}$/.test(phoneLast4.trim())) {
      toast("Vui lòng nhập đúng 4 số cuối điện thoại.", "error");
      return;
    }
    setBusy(true);
    try {
      setRefund(await publicDepositRefundApi.verify(token, { verificationCode: verificationCode.trim(), phoneLast4: phoneLast4.trim() }));
      setVerificationCode("");
      toast("Xác minh thành công.", "success");
    } catch (err) {
      toast(friendlyError(err), "error");
    } finally {
      setBusy(false);
    }
  }

  async function submitMethod(event: FormEvent) {
    event.preventDefault();
    if (method === "BankTransfer" && accountError) {
      toast(accountError, "error");
      return;
    }
    setBusy(true);
    try {
      const payload = method === "CashAtVenue"
        ? { refundMethod: "CashAtVenue" as const }
        : {
            refundMethod: "BankTransfer" as const,
            bankCode,
            bankName,
            accountNumber,
            confirmAccountNumber,
            accountHolderName: accountHolderName.trim().toUpperCase()
          };
      setRefund(await publicDepositRefundApi.submitMethod(token, payload));
      setAccountNumber("");
      setConfirmAccountNumber("");
      setAccountHolderName("");
      toast("Đã gửi thông tin nhận hoàn cọc.", "success");
    } catch (err) {
      toast(friendlyError(err), "error");
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="refund-public-page">
      <section className="card refund-public-card">
        <div className="refund-public-head">
          <div>
            <p className="eyebrow">PoolHub Refund</p>
            <h1>Hoàn cọc đặt bàn</h1>
            <p>Trang bảo mật dành cho khách public, không cần tài khoản PoolHub.</p>
          </div>
          {refund ? <Badge tone={refund.status === 6 ? "green" : refund.status >= 7 ? "red" : "yellow"}>{getRefundStatusLabel(refund.status)}</Badge> : null}
        </div>

        <StateBlock loading={loading} error={error} />
        {refund ? (
          <div className="refund-public-grid">
            <div className="refund-summary">
              <Info label="Mã hoàn" value={refund.refundCode} />
              <Info label="Mã booking" value={refund.bookingCode || "-"} />
              <Info label="Số tiền hoàn" value={money(refund.amount)} strong />
              <Info label="Lý do" value={getRefundReasonLabel(refund.reason)} />
              <Info label="Email" value={refund.customerEmailMasked || "-"} />
              <Info label="Điện thoại" value={refund.customerPhoneMasked || "-"} />
              <Info label="Hết hạn link" value={dateTime(refund.tokenExpiresAtUtc)} />
              <Info label="Phương thức" value={getRefundMethodLabel(refund.refundMethod)} />
              {refund.bankAccountLast4 ? <Info label="Tài khoản" value={maskedAccount(refund.bankAccountLast4)} /> : null}
            </div>

            {refund.status === 1 && !refund.isVerified ? (
              <form className="card form-stack" onSubmit={verify}>
                <h2>Xác minh khách nhận hoàn</h2>
                <p className="muted-text">Mã xác minh được gửi tới email đã dùng khi đặt bàn. Không lưu mã này trên trình duyệt.</p>
                <button className="secondary-btn" type="button" onClick={sendCode} disabled={busy || countdown > 0}>
                  {countdown > 0 ? `Gửi lại sau ${countdown}s` : "Gửi mã xác minh"}
                </button>
                <label><span>Mã xác minh 6 số</span><input inputMode="numeric" autoComplete="one-time-code" value={verificationCode} maxLength={6} onChange={(e) => setVerificationCode(e.target.value.replace(/\D/g, "").slice(0, 6))} /></label>
                <label><span>4 số cuối điện thoại</span><input inputMode="numeric" value={phoneLast4} maxLength={4} onChange={(e) => setPhoneLast4(e.target.value.replace(/\D/g, "").slice(0, 4))} /></label>
                <button className="primary-btn" disabled={busy} type="submit">{busy ? "Đang xử lý..." : "Xác minh"}</button>
              </form>
            ) : null}

            {canSubmitMethod ? (
              <form className="card form-stack" onSubmit={submitMethod}>
                <h2>Chọn phương thức nhận tiền</h2>
                <div className="refund-method-tabs">
                  <button type="button" className={method === "BankTransfer" ? "primary-btn" : "ghost-btn"} onClick={() => setMethod("BankTransfer")}>Chuyển khoản</button>
                  <button type="button" className={method === "CashAtVenue" ? "primary-btn" : "ghost-btn"} onClick={() => setMethod("CashAtVenue")}>Tiền mặt tại quán</button>
                </div>
                {method === "BankTransfer" ? (
                  <>
                    <label><span>Ngân hàng</span><select value={bankCode} onChange={(e) => setBankCode(e.target.value)}>{bankOptions.map((bank) => <option key={bank.code} value={bank.code}>{bank.name}</option>)}</select></label>
                    <label><span>Mã ngân hàng</span><input value={bankCode} onChange={(e) => setBankCode(e.target.value.trim().toUpperCase())} /></label>
                    <label><span>Số tài khoản</span><input autoComplete="off" value={accountNumber} onChange={(e) => setAccountNumber(e.target.value.trim())} /></label>
                    <label><span>Nhập lại số tài khoản</span><input autoComplete="off" value={confirmAccountNumber} onChange={(e) => setConfirmAccountNumber(e.target.value.trim())} /></label>
                    <label><span>Tên chủ tài khoản</span><input autoComplete="name" value={accountHolderName} onChange={(e) => setAccountHolderName(e.target.value)} /></label>
                    {accountError ? <p className="field-error">{accountError}</p> : null}
                  </>
                ) : (
                  <div className="inline-alert warning">
                    Yêu cầu cần được Manager duyệt. Khi tiền mặt sẵn sàng, bạn sẽ nhận email chứa mã nhận tiền. Vui lòng mang mã booking và cung cấp mã này tại quầy.
                  </div>
                )}
                <button className="primary-btn" disabled={busy} type="submit">{busy ? "Đang gửi..." : "Gửi thông tin hoàn cọc"}</button>
              </form>
            ) : null}

            {refund.status !== 1 ? (
              <div className="inline-alert success">
                Thông tin hoàn cọc đã được ghi nhận. Trạng thái hiện tại: <strong>{getRefundStatusLabel(refund.status)}</strong>.
              </div>
            ) : null}
          </div>
        ) : null}
      </section>
    </main>
  );
}

function Info({ label, value, strong = false }: { label: string; value?: string; strong?: boolean }) {
  return (
    <div className="refund-info-row">
      <span>{label}</span>
      {strong ? <strong>{value || "-"}</strong> : <b>{value || "-"}</b>}
    </div>
  );
}
