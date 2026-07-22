import type { BookingTableInfo } from "@/types";
import { tableDisplayName } from "@/lib/bookingTables";

export type MultiTableSelectorTable = BookingTableInfo & {
  operationalStatus?: number;
  isActive?: boolean;
  estimatedAmount?: number;
};

type Props = {
  tables: MultiTableSelectorTable[];
  selectedTableIds: number[];
  unavailableTableIds?: number[];
  conflictingTableIds?: number[];
  loading?: boolean;
  disabled?: boolean;
  onToggle: (tableId: number) => void;
  onRemove?: (tableId: number) => void;
  onClear?: () => void;
};

function getStatus(table: MultiTableSelectorTable, unavailable: boolean, conflict: boolean) {
  if (conflict) return { label: "Trùng lịch", disabled: true, className: "conflict" };
  if (unavailable) return { label: "Không trống", disabled: true, className: "conflict" };
  if (table.isActive === false) return { label: "Ngừng hoạt động", disabled: true, className: "disabled" };
  if (table.operationalStatus === 4) return { label: "Bảo trì", disabled: true, className: "disabled" };
  if (table.operationalStatus === 5) return { label: "Ngừng hoạt động", disabled: true, className: "disabled" };
  if (table.operationalStatus === 2) return { label: "Đang có khách", disabled: true, className: "disabled" };
  if (table.operationalStatus === 3) return { label: "Đã đặt trước", disabled: true, className: "disabled" };
  return { label: "Có thể chọn", disabled: false, className: "available" };
}

export function SelectedTablesSummary({
  tables,
  selectedTableIds,
  onRemove,
  onClear
}: {
  tables: MultiTableSelectorTable[];
  selectedTableIds: number[];
  onRemove?: (tableId: number) => void;
  onClear?: () => void;
}) {
  const selected = selectedTableIds
    .map((tableId) => tables.find((table) => Number(table.tableId) === tableId) || { tableId })
    .filter(Boolean) as MultiTableSelectorTable[];

  return (
    <div className="multi-table-summary" aria-live="polite">
      <div className="multi-table-summary-head">
        <strong>Đã chọn {selected.length} bàn</strong>
        {selected.length && onClear ? <button type="button" className="ghost-btn compact" onClick={onClear}>Bỏ chọn tất cả</button> : null}
      </div>
      {selected.length ? (
        <div className="multi-table-chips">
          {selected.map((table) => (
            <span className="multi-table-chip" key={table.tableId}>
              {tableDisplayName(table)}
              {onRemove ? <button type="button" aria-label={`Bo ${tableDisplayName(table)}`} onClick={() => onRemove(Number(table.tableId))}>x</button> : null}
            </span>
          ))}
        </div>
      ) : (
        <div className="inline-note">Chưa chọn bàn nào.</div>
      )}
    </div>
  );
}

export function MultiTableSelector({
  tables,
  selectedTableIds,
  unavailableTableIds = [],
  conflictingTableIds = [],
  loading,
  disabled,
  onToggle
}: Props) {
  const selectedSet = new Set(selectedTableIds);
  const unavailableSet = new Set(unavailableTableIds);
  const conflictSet = new Set(conflictingTableIds);

  if (loading) {
    return <div className="multi-table-empty">Đang tải danh sách bàn...</div>;
  }

  if (!tables.length) {
    return <div className="multi-table-empty">Không có bàn phù hợp. Hãy đổi thời gian hoặc bộ lọc.</div>;
  }

  return (
    <div className="multi-table-grid">
      {tables.map((table) => {
        const tableId = Number(table.tableId);
        const selected = selectedSet.has(tableId);
        const status = getStatus(table, unavailableSet.has(tableId), conflictSet.has(tableId));
        const buttonDisabled = disabled || (status.disabled && !selected);
        return (
          <button
            key={tableId}
            type="button"
            className={`multi-table-card ${selected ? "selected" : ""} ${status.className}`}
            aria-pressed={selected}
            disabled={buttonDisabled}
            onClick={() => onToggle(tableId)}
          >
            <span className="multi-table-card-top">
              <strong>{table.tableCode || `#${tableId}`}</strong>
              <span>{selected ? "Đã chọn" : status.label}</span>
            </span>
            <span>{table.tableName || "Bàn"}</span>
            <small>{table.tableTypeName || "Loại bàn"}{table.capacity ? ` - ${table.capacity} người` : ""}</small>
            {table.zoneName || table.floorName ? <small>{[table.floorName, table.zoneName].filter(Boolean).join(" - ")}</small> : null}
            {table.estimatedAmount ? <small>{table.estimatedAmount.toLocaleString("vi-VN")} d</small> : null}
          </button>
        );
      })}
    </div>
  );
}
