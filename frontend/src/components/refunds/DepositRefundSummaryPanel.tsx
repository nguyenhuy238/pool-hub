"use client";

import type { DepositRefundSummary } from "@/types";
import { getRefundMethodLabel, getRefundReasonLabel, getRefundStatusLabel } from "@/lib/refunds";
import { dateTime, money } from "@/lib/status";

type Props = {
  summary?: DepositRefundSummary | null;
  compact?: boolean;
};

export function DepositRefundSummaryPanel({ summary, compact = false }: Props) {
  if (!summary) return null;

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
      {summary.refundRequests?.length ? (
        <div className="deposit-refund-requests">
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
      ) : null}
    </div>
  );
}
