import { expect, test } from "@playwright/test";
import { groupInventoryTransactions } from "../src/lib/inventoryGrouping";
import type { InventoryTransaction } from "../src/types";

const transaction = (overrides: Partial<InventoryTransaction>): InventoryTransaction => ({
  inventoryTransactionId: 1,
  productId: 1,
  productName: "Coca",
  transactionType: 4,
  quantity: -1,
  createdAtUtc: "2026-07-23T08:00:00Z",
  ...overrides
});

test.describe("inventory transaction grouping", () => {
  test("merges the same product inside an invoice", () => {
    const groups = groupInventoryTransactions([
      transaction({ inventoryTransactionId: 1, invoiceId: 7, invoiceCode: "INV007", quantity: -2 }),
      transaction({ inventoryTransactionId: 2, invoiceId: 7, invoiceCode: "INV007", quantity: -3 })
    ]);

    expect(groups).toHaveLength(1);
    expect(groups[0].products).toHaveLength(1);
    expect(groups[0].products[0].quantity).toBe(-5);
    expect(groups[0].products[0].transactionCount).toBe(2);
  });

  test("keeps different invoices in separate groups", () => {
    const groups = groupInventoryTransactions([
      transaction({ inventoryTransactionId: 1, invoiceId: 7, invoiceCode: "INV007" }),
      transaction({ inventoryTransactionId: 2, invoiceId: 8, invoiceCode: "INV008" })
    ]);

    expect(groups.map((group) => group.title).sort()).toEqual(["INV007", "INV008"]);
  });

  test("groups order movements before an invoice is generated", () => {
    const groups = groupInventoryTransactions([
      transaction({ inventoryTransactionId: 1, orderId: 12, referenceType: "ORDER", referenceId: 12 }),
      transaction({ inventoryTransactionId: 2, productId: 2, productName: "Nước suối", orderId: 12, referenceType: "ORDER", referenceId: 12 })
    ]);

    expect(groups).toHaveLength(1);
    expect(groups[0].title).toBe("Đơn hàng #12");
    expect(groups[0].products).toHaveLength(2);
  });
});

