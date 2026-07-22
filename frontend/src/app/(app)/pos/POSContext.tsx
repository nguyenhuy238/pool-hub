"use client";

import React, { createContext, useContext, useState, useEffect } from 'react';
import type { VenueTableLayoutItem } from '@/types';
import * as signalR from '@microsoft/signalr';
import { API_BASE_URL } from '@/lib/api/client';

interface POSContextType {
  selectedTable: VenueTableLayoutItem | null;
  setSelectedTable: (table: VenueTableLayoutItem | null) => void;
  isDrawerOpen: boolean;
  setIsDrawerOpen: (isOpen: boolean) => void;
  refreshTrigger: number;
  triggerRefresh: () => void;
}

const POSContext = createContext<POSContextType | undefined>(undefined);

export function POSProvider({ children }: { children: React.ReactNode }) {
  const [selectedTable, setSelectedTable] = useState<VenueTableLayoutItem | null>(null);
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [refreshTrigger, setRefreshTrigger] = useState(0);

  const triggerRefresh = () => setRefreshTrigger(prev => prev + 1);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/pos`)
      .withAutomaticReconnect()
      .build();

    connection.on("ReceiveTableUpdate", (tableId) => {
      triggerRefresh();
    });

    connection.on("ReceiveBookingUpdate", (bookingId) => {
      triggerRefresh();
    });

    connection.on("ReceiveSessionUpdate", (sessionId) => {
      triggerRefresh();
    });

    connection.on("ReceiveRefreshPos", () => {
      triggerRefresh();
    });

    connection.start()
      .then(() => console.log("SignalR Connected to POS Hub"))
      .catch(err => console.error("SignalR Connection Error: ", err));

    return () => {
      connection.stop();
    };
  }, []);

  return (
    <POSContext.Provider value={{ selectedTable, setSelectedTable, isDrawerOpen, setIsDrawerOpen, refreshTrigger, triggerRefresh }}>
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
