"use client";

import React, { createContext, useContext, useState } from 'react';
import type { VenueTableLayoutItem } from '@/types';

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
