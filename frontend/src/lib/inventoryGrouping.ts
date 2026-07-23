import type { InventoryTransaction } from "@/types";

export type InventoryProductGroup = {
  productId: number;
  productName: string;
  quantity: number;
  transactionCount: number;
  unitCost?: number;
  latestAtUtc: string;
};

export type InventoryInvoiceGroup = {
  key: string;
  invoiceId?: number;
  invoiceCode?: string;
  orderId?: number;
  referenceType?: string;
  title: string;
  products: InventoryProductGroup[];
  transactionCount: number;
  netQuantity: number;
  latestAtUtc: string;
};

function groupKey(item: InventoryTransaction) {
  if (item.invoiceId) return `invoice:${item.invoiceId}`;
  if (item.orderId || (item.referenceType === "ORDER" && item.referenceId)) {
    return `order:${item.orderId || item.referenceId}`;
  }
  return `transaction:${item.inventoryTransactionId}`;
}

function groupTitle(item: InventoryTransaction) {
  if (item.invoiceCode) return item.invoiceCode;
  if (item.invoiceId) return `Hóa đơn #${item.invoiceId}`;
  if (item.orderId || (item.referenceType === "ORDER" && item.referenceId)) {
    return `Đơn hàng #${item.orderId || item.referenceId}`;
  }
  return "Điều chỉnh tồn kho";
}

export function groupInventoryTransactions(rows: InventoryTransaction[]): InventoryInvoiceGroup[] {
  const groups = new Map<string, InventoryInvoiceGroup>();

  rows.forEach((item) => {
    const key = groupKey(item);
    const group = groups.get(key) || {
      key,
      invoiceId: item.invoiceId,
      invoiceCode: item.invoiceCode,
      orderId: item.orderId,
      referenceType: item.referenceType,
      title: groupTitle(item),
      products: [],
      transactionCount: 0,
      netQuantity: 0,
      latestAtUtc: item.createdAtUtc
    };

    const product = group.products.find((entry) => entry.productId === item.productId);
    if (product) {
      product.quantity += Number(item.quantity || 0);
      product.transactionCount += 1;
      if (item.unitCost !== undefined) product.unitCost = item.unitCost;
      if (item.createdAtUtc > product.latestAtUtc) product.latestAtUtc = item.createdAtUtc;
    } else {
      group.products.push({
        productId: item.productId,
        productName: item.productName,
        quantity: Number(item.quantity || 0),
        transactionCount: 1,
        unitCost: item.unitCost,
        latestAtUtc: item.createdAtUtc
      });
    }

    group.transactionCount += 1;
    group.netQuantity += Number(item.quantity || 0);
    if (item.createdAtUtc > group.latestAtUtc) group.latestAtUtc = item.createdAtUtc;
    groups.set(key, group);
  });

  return Array.from(groups.values())
    .map((group) => ({
      ...group,
      products: group.products.sort((a, b) => a.productName.localeCompare(b.productName, "vi"))
    }))
    .sort((a, b) => b.latestAtUtc.localeCompare(a.latestAtUtc));
}

