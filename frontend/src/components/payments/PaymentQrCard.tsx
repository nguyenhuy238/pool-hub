"use client";

import type { ReactNode } from "react";
import { money } from "@/lib/status";

export type PaymentQrCardProps = {
  title?: string;
  qrUrl?: string;
  bankName?: string;
  bankCode?: string;
  accountNumber?: string;
  accountName?: string;
  amount: number;
  transferContent: string;
  note?: string;
  onCopy?: (message: string) => void;
};

export function PaymentQrCard({
  title = "Quét mã QR để thanh toán",
  qrUrl,
  bankName,
  bankCode,
  accountNumber,
  accountName,
  amount,
  transferContent,
  note,
  onCopy
}: PaymentQrCardProps) {
  const copy = async (value: string, message: string) => {
    if (!value) return;
    await navigator.clipboard.writeText(value);
    onCopy?.(message);
  };

  return (
    <div style={{ background: "#f8fbfa", border: "1.5px solid #0f5d4b", borderRadius: 12, padding: 20, display: "flex", flexDirection: "column", alignItems: "center", gap: 14 }}>
      <div style={{ fontWeight: 700, fontSize: 15, color: "#0f5d4b", display: "flex", alignItems: "center", gap: 6 }}>
        {title}
      </div>

      {qrUrl ? (
        <div style={{ background: "white", padding: 12, borderRadius: 12, boxShadow: "0 4px 16px rgba(0,0,0,0.08)", border: "1px solid var(--line)" }}>
          <img src={qrUrl} alt={title} style={{ width: "100%", maxWidth: 300, display: "block", borderRadius: 8 }} />
        </div>
      ) : (
        <div className="inline-alert error">Chưa cấu hình QR chuyển khoản. Vui lòng liên hệ nhân viên.</div>
      )}

      <div style={{ width: "100%", fontSize: 13, background: "white", padding: 14, borderRadius: 8, border: "1px solid var(--line)", display: "grid", gap: 8 }}>
        <InfoRow label="Ngân hàng" value={bankName || bankCode || "-"} />
        <InfoRow
          label="Số tài khoản"
          value={accountNumber || "-"}
        />
        <InfoRow label="Chủ tài khoản" value={accountName || "-"} />
        <InfoRow label="Số tiền" value={money(amount)} strongColor="#0f5d4b" />
        <InfoRow
          label="Nội dung chuyển khoản"
          value={transferContent || "-"}
          strongColor="#d9534f"
          topBorder
          action={transferContent ? <button type="button" className="ghost-btn compact" style={{ padding: "2px 8px", fontSize: 12 }} onClick={() => copy(transferContent, "Đã sao chép nội dung chuyển khoản.")}>Sao chép</button> : null}
        />
      </div>

      {note ? <p style={{ fontSize: 12, color: "var(--muted)", margin: 0, textAlign: "center" }}>{note}</p> : null}
    </div>
  );
}

function InfoRow({ label, value, action, strongColor, topBorder }: { label: string; value: string; action?: ReactNode; strongColor?: string; topBorder?: boolean }) {
  return (
    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 12, borderTop: topBorder ? "1px dashed var(--line)" : undefined, paddingTop: topBorder ? 8 : undefined, marginTop: topBorder ? 4 : undefined }}>
      <span style={{ color: "var(--muted)" }}>{label}:</span>
      <div style={{ display: "flex", gap: 8, alignItems: "center", justifyContent: "flex-end", textAlign: "right", minWidth: 0 }}>
        <strong style={{ color: strongColor || "var(--ink)", fontSize: strongColor ? 15 : undefined, wordBreak: "break-word" }}>{value}</strong>
        {action}
      </div>
    </div>
  );
}
