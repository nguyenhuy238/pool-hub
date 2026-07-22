export type TableCapacityOption = {
  tableId: number;
  capacity: number;
};

export function getSelectedTablesCapacity(
  tables: TableCapacityOption[],
  selectedTableIds: number[]
) {
  const selectedIds = new Set(selectedTableIds.map(Number));

  return tables.reduce((total, table) => {
    if (!selectedIds.has(Number(table.tableId))) return total;
    const capacity = Number(table.capacity);
    return total + (Number.isFinite(capacity) && capacity > 0 ? capacity : 0);
  }, 0);
}

export function getGuestCapacityError(numberOfGuests: number, maximumCapacity: number) {
  if (!Number.isFinite(numberOfGuests) || numberOfGuests <= 0) {
    return "Số lượng khách phải lớn hơn 0.";
  }

  if (maximumCapacity > 0 && numberOfGuests > maximumCapacity) {
    return `Không thể đặt bàn: số lượng khách vượt quá sức chứa tối đa của bàn đã chọn (${maximumCapacity} khách).`;
  }

  return null;
}
