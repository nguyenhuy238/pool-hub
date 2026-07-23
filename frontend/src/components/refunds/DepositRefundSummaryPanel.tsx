"use client";

import type { DepositRefundSummary } from "@/types";
import { getRefundMethodLabel, getRefundReasonLabel, getRefundStatusLabel } from "@/lib/refunds";
import { dateTime, money } from "@/lib/status";

type Props = {
  summary?: DepositRefundSummary | null;
  compact?: boolean;
  variant?: "full" | "bill";
};

export function DepositRefundSummaryPanel({ summary, compact = false, variant = "full" }: Props) {
  if (!summary) return null;

  if (variant === "bill") {
    const billRows = [
      ["Thanh toán bằng cọc", summary.appliedAmount],
      ["Phần cọc còn lại", summary.forfeitedAmount],
      ["Đang chờ hoàn", summary.pendingRefundAmount],
      ["Đã hoàn thực tế", summary.refundedAmount],
    ] as const;
    const visibleRows = billRows.filter(([, value]) => Number(value || 0) > 0);
    const hasRefundRequests = Boolean(summary.refundRequests?.length);
    if (!visibleRows.length && !hasRefundRequests) return null;

    return (
      <div className="deposit-refund-summary bill">
        {visibleRows.map(([label, value]) => (
          <div key={label} className="deposit-refund-bill-row">
            <span>{label}</span>
            <strong>{money(value)}</strong>
          </div>
        ))}
        {Number(summary.forfeitedAmount || 0) > 0 ? (
          <p className="deposit-refund-note">Theo chính sách, phần cọc còn lại không được hoàn sau khi khách đã nhận bàn.</p>
        ) : null}
        {hasRefundRequests ? <RefundRequestList summary={summary} compact /> : null}
      </div>
    );
  }

  const rows = [
    ["Tiền cọc đã thanh toán", summary.paidAmount],
    ["Tiền cọc đã áp dụng", summary.appliedAmount],
    ["Tiền cọc bị mất", summary.forfeitedAmount],
    ["Tiền đang chờ hoàn", summary.pendingRefundAmount],
    ["Tiền đã hoàn thực tế", summary.refundedAmount],
    ["Số dư cọc còn lại", summary.refundableBalance],
  ] as const;

  if (compact) {
    return (
      <div className="deposit-refund-summary compact">
        {rows.map(([label, value]) => (
          Number(value || 0) > 0 ? <span key={label}>{label}: <strong>{money(value)}</strong></span> : null
        ))}
      </div>
    );
  }

  return (
    <div className="deposit-refund-summary">
      <div className="deposit-refund-grid">
        {rows.map(([label, value]) => (
          <div key={label} className="deposit-refund-metric">
            <span>{label}</span>
            <strong>{money(value)}</strong>
          </div>
        ))}
      </div>
      {summary.refundRequests?.length ? <RefundRequestList summary={summary} /> : null}
    </div>
  );
}

function RefundRequestList({ summary, compact = false }: { summary: DepositRefundSummary; compact?: boolean }) {
  return (
    <div className={`deposit-refund-requests${compact ? " compact-list" : ""}`}>
      <h4>Yêu cầu hoàn cọc</h4>
      <div className="deposit-refund-request-list">
        {summary.refundRequests.map((refund) => (
          <div key={refund.bookingDepositRefundId} className="deposit-refund-request">
            <div>
              <strong>{refund.refundCode}</strong>
              <span>{getRefundReasonLabel(refund.reason)} · {getRefundMethodLabel(refund.refundMethod)}</span>
            </div>
            <div>
              <strong>{money(refund.amount)}</strong>
              <span>{getRefundStatusLabel(refund.status)} · {refund.succeededAtUtc ? dateTime(refund.succeededAtUtc) : dateTime(refund.createdAtUtc)}</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
