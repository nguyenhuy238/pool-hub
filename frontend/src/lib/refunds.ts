import type { DepositRefundMethod, DepositRefundReason, DepositRefundStatus } from "@/types";

export const refundStatusLabel: Record<DepositRefundStatus, string> = {
  1: "Chờ khách cung cấp thông tin",
  2: "Chờ duyệt",
  3: "Đã duyệt",
  4: "Đang xử lý",
  5: "Sẵn sàng nhận tiền mặt",
  6: "Đã hoàn thành công",
  7: "Hoàn tiền thất bại",
  8: "Bị từ chối",
  9: "Đã hủy"
};

export const refundStatusTone: Record<DepositRefundStatus, "green" | "blue" | "yellow" | "red" | "purple" | "neutral"> = {
  1: "yellow",
  2: "yellow",
  3: "blue",
  4: "purple",
  5: "purple",
  6: "green",
  7: "red",
  8: "red",
  9: "neutral"
};

export const refundMethodLabel: Record<DepositRefundMethod, string> = {
  1: "Chuyển khoản ngân hàng",
  2: "Nhận tiền mặt tại quán"
};

export const refundReasonLabel: Record<DepositRefundReason | string, string> = {
  CustomerCancelledInTime: "Khách hủy đúng hạn",
  CustomerCancelledLate: "Khách hủy muộn",
  VenueFault: "Lỗi từ phía quán",
  BookingRejected: "Booking bị từ chối",
  DuplicateDeposit: "Thanh toán cọc trùng",
  DepositExcess: "Cọc dư sau khi trừ hóa đơn",
  ManualAdjustment: "Điều chỉnh thủ công",
  Other: "Khác"
};

export function getRefundStatusLabel(status?: number) {
  return refundStatusLabel[status as DepositRefundStatus] || `Trạng thái ${status ?? "-"}`;
}

export function getRefundMethodLabel(method?: number | null) {
  return method ? refundMethodLabel[method as DepositRefundMethod] || `Phương thức ${method}` : "Chưa chọn";
}

export function getRefundReasonLabel(reason?: string | null) {
  return reason ? refundReasonLabel[reason] || reason : "-";
}

export function maskedAccount(last4?: string | null) {
  return last4 ? `******${last4}` : "-";
}
