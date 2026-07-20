import type { Booking, BookingTableInfo, VenueTable, VenueTableLayoutItem } from "@/types";

type TableLookupItem = Partial<BookingTableInfo> & { tableId: number };

export function normalizeTableIds(tableIds: Array<number | string | null | undefined>) {
  return Array.from(new Set(tableIds.map(Number).filter((id) => Number.isFinite(id) && id > 0)));
}

export function getBookingTables(booking: Partial<Booking>, tableCache: TableLookupItem[] = []): BookingTableInfo[] {
  if (booking.tables?.length) {
    return booking.tables.map((table) => ({
      tableId: Number(table.tableId),
      tableCode: table.tableCode,
      tableName: table.tableName,
      tableTypeId: table.tableTypeId,
      tableTypeName: table.tableTypeName,
      zoneId: table.zoneId,
      zoneName: table.zoneName,
      floorId: table.floorId,
      floorName: table.floorName,
      capacity: table.capacity
    }));
  }

  const ids = normalizeTableIds([...(booking.tableIds || []), booking.tableId]);
  return ids.map((tableId) => {
    const cached = tableCache.find((table) => Number(table.tableId) === tableId);
    return {
      tableId,
      tableCode: cached?.tableCode,
      tableName: cached?.tableName || (booking.tableId === tableId ? booking.tableName : undefined),
      tableTypeId: cached?.tableTypeId || booking.tableTypeId,
      tableTypeName: cached?.tableTypeName,
      zoneId: cached?.zoneId,
      zoneName: cached?.zoneName,
      floorId: cached?.floorId,
      floorName: cached?.floorName,
      capacity: cached?.capacity
    };
  });
}

export function tableDisplayName(table: Partial<BookingTableInfo> | VenueTable | VenueTableLayoutItem | undefined) {
  if (!table) return "Chua xep ban";
  return table.tableCode
    ? `${table.tableCode}${table.tableName ? ` - ${table.tableName}` : ""}`
    : table.tableName || `Ban #${table.tableId}`;
}

export function compactBookingTablesLabel(booking: Partial<Booking>, tableCache: TableLookupItem[] = []) {
  const tables = getBookingTables(booking, tableCache);
  if (!tables.length) return "Chua xep ban";
  const labels = tables.map((table) => table.tableCode || table.tableName || `#${table.tableId}`);
  return labels.length <= 2 ? labels.join(", ") : `${labels.slice(0, 2).join(", ")} +${labels.length - 2}`;
}
