export function openInvoiceDisplay(invoiceId: number) {
  if (typeof window === "undefined" || !Number.isFinite(invoiceId) || invoiceId <= 0) return null;
  const display = window.open(
    `/invoice-display/${invoiceId}`,
    "poolhub-customer-invoice",
    "popup=yes,width=880,height=980,resizable=yes,scrollbars=yes"
  );
  display?.focus();
  return display;
}

// Mở trang Quản lý Hóa đơn (dành cho nhân viên) và tự bật popup chi tiết đúng hóa đơn để thu tiền.
// Trang /operation/invoices đã hỗ trợ query param ?invoiceId= để auto mở chi tiết.
export function openInvoicePage(invoiceId: number) {
  if (typeof window === "undefined" || !Number.isFinite(invoiceId) || invoiceId <= 0) return null;
  const win = window.open(`/operation/invoices?invoiceId=${invoiceId}`, "poolhub-invoice-page");
  win?.focus();
  return win;
}
