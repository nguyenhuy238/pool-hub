"use client";

import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { VenueTableLayoutItem, VenueLayoutResponse } from '@/types';
import * as signalR from '@microsoft/signalr';
import { API_BASE_URL } from '@/lib/api/client';
import { usePOSLayout } from './hooks/usePOSLayout';
import { usePricingEstimate } from './hooks/usePricingEstimate';

interface POSContextType {
  // Sơ đồ bàn (tải tập trung tại Provider, dùng chung cho các cột)
  tables: VenueTableLayoutItem[];
  layoutMeta: VenueLayoutResponse | null;
  layoutLoading: boolean;
  layoutError: string | null;
  // Bàn đang chọn — được DERIVE từ `tables` nên luôn tươi sau mỗi lần refresh (fix stale table)
  selectedTable: VenueTableLayoutItem | null;
  setSelectedTable: (table: VenueTableLayoutItem | null) => void;
  isDrawerOpen: boolean;
  setIsDrawerOpen: (isOpen: boolean) => void;
  refreshTrigger: number;
  triggerRefresh: () => void;
  // Đơn giá tạm tính theo tableTypeId (để ước tính tiền giờ trên thẻ bàn)
  rateByType: Map<number, number>;
}

const POSContext = createContext<POSContextType | undefined>(undefined);

export function POSProvider({ children }: { children: React.ReactNode }) {
  const [refreshTrigger, setRefreshTrigger] = useState(0);
  const triggerRefresh = useCallback(() => setRefreshTrigger((prev) => prev + 1), []);

  const { tables, meta: layoutMeta, loading: layoutLoading, error: layoutError } = usePOSLayout(refreshTrigger);
  const rateByType = usePricingEstimate();

  const [selectedTableId, setSelectedTableId] = useState<number | null>(null);
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);

  // Giữ nguyên chữ ký cũ `setSelectedTable(table)` nhưng chỉ lưu id; bàn thật được derive bên dưới.
  const setSelectedTable = useCallback((table: VenueTableLayoutItem | null) => {
    setSelectedTableId(table?.tableId ?? null);
  }, []);

  // Derive bàn đang chọn từ danh sách mới nhất => sau khi layout nạp lại, bàn tự có activeSessionId mới.
  const selectedTable = useMemo(
    () => (selectedTableId == null ? null : tables.find((t) => t.tableId === selectedTableId) ?? null),
    [tables, selectedTableId]
  );

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/pos`)
      .withAutomaticReconnect()
      .build();

    connection.on("ReceiveTableUpdate", () => triggerRefresh());
    connection.on("ReceiveBookingUpdate", () => triggerRefresh());
    connection.on("ReceiveSessionUpdate", () => triggerRefresh());
    connection.on("ReceiveRefreshPos", () => triggerRefresh());

    connection.start()
      .then(() => console.log("SignalR Connected to POS Hub"))
      .catch(err => console.error("SignalR Connection Error: ", err));

    return () => {
      connection.stop();
    };
  }, [triggerRefresh]);

  return (
    <POSContext.Provider
      value={{
        tables,
        layoutMeta,
        layoutLoading,
        layoutError,
        selectedTable,
        setSelectedTable,
        isDrawerOpen,
        setIsDrawerOpen,
        refreshTrigger,
        triggerRefresh,
        rateByType,
      }}
    >
      {children}
    </POSContext.Provider>
  );
}

export function usePOS() {
  const context = useContext(POSContext);
  if (!context) {
    throw new Error('usePOS must be used within a POSProvider');
  }
  return context;
}
