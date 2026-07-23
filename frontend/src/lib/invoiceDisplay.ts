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
