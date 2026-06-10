export const bookingStatus: Record<number, string> = {
  1: "Pending",
  2: "Confirmed",
  3: "Cancelled",
  4: "Completed",
  5: "NoShow"
};

export const sessionStatus: Record<number, string> = {
  1: "Active",
  2: "Closed",
  3: "Transferred"
};

export const tableStatus: Record<number, string> = {
  1: "Available",
  2: "In Use",
  3: "Reserved",
  4: "Maintenance",
  5: "Inactive"
};

export function label(values: Record<number, string>, status?: number) {
  if (status === undefined || status === null) return "Unknown";
  return values[status] || `Status ${status}`;
}

export function money(value?: number) {
  return new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 }).format(value || 0);
}

export function dateTime(value?: string) {
  if (!value) return "-";
  return new Intl.DateTimeFormat("vi-VN", { dateStyle: "short", timeStyle: "short" }).format(new Date(value));
}
