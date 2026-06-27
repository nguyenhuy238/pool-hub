export const bookingStatus: Record<number, string> = {
  1: "Chờ xác nhận",
  2: "Đã xác nhận",
  3: "Đã hủy",
  4: "Đã hoàn thành",
  5: "Không đến"
};

export const sessionStatus: Record<number, string> = {
  1: "Đang hoạt động",
  2: "Đã kết thúc",
  3: "Đã chuyển bàn"
};

export const tableStatus: Record<number, string> = {
  1: "Sẵn sàng",
  2: "Đang có khách",
  3: "Đã đặt trước",
  4: "Bảo trì",
  5: "Ngừng hoạt động"
};

export function label(values: Record<number, string>, status?: number) {
  if (status === undefined || status === null) return "Không xác định";
  return values[status] || `Trạng thái ${status}`;
}

export const userStatus: Record<string, string> = {
  Active: "Đang hoạt động",
  Locked: "Đã khóa",
  Deleted: "Đã xóa"
};

export const paymentStatus: Record<number, string> = {
  1: "Chờ thanh toán",
  2: "Đã hoàn tất",
  3: "Thất bại",
  4: "Đã hoàn tiền"
};

export function money(value?: number) {
  return new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 }).format(value || 0);
}

export function dateTime(value?: string) {
  if (!value) return "-";
  return new Intl.DateTimeFormat("vi-VN", { dateStyle: "short", timeStyle: "short" }).format(new Date(value));
}
