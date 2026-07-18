import React from 'react';
import { POSProvider } from './POSContext';
import { BookingQueueColumn } from './components/BookingQueueColumn';
import { LiveTableMapColumn } from './components/LiveTableMapColumn';
import { ActionDrawerColumn } from './components/ActionDrawerColumn';
import styles from './pos.module.css';

export default function POSCommandCenter() {
  return (
    <POSProvider>
      <div className={styles.posContainer}>
        <BookingQueueColumn />
        <LiveTableMapColumn />
        <ActionDrawerColumn />
      </div>
    </POSProvider>
  );
}
