import { expect, test } from "@playwright/test";
import { getGuestCapacityError, getSelectedTablesCapacity } from "../src/lib/bookingCapacity";

test.describe("booking guest capacity", () => {
  const tables = [
    { tableId: 1, capacity: 4 },
    { tableId: 2, capacity: 6 },
    { tableId: 3, capacity: 8 }
  ];

  test("uses the total capacity of all selected tables", () => {
    expect(getSelectedTablesCapacity(tables, [1, 3])).toBe(12);
  });

  test("rejects a guest count above the selected capacity", () => {
    expect(getGuestCapacityError(7, 6)).toBe(
      "Không thể đặt bàn: số lượng khách vượt quá sức chứa tối đa của bàn đã chọn (6 khách)."
    );
  });

  test("accepts a guest count equal to the selected capacity", () => {
    expect(getGuestCapacityError(6, 6)).toBeNull();
  });
});
