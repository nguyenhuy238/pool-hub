import React, { useState, useEffect, useRef } from 'react';
import './TableTimelinePicker.css';

export interface TimelineBooking {
  startTime: string; // HH:mm
  endTime: string;   // HH:mm
}

interface TableTimelinePickerProps {
  existingBookings: TimelineBooking[];
  selectedDate: string;
  startTime: string; // HH:mm
  durationHours: number;
  onTimeChange: (newStartTime: string, newDurationHours: number) => void;
  minDuration?: number;
  maxDuration?: number;
}

export function TableTimelinePicker({
  existingBookings,
  selectedDate,
  startTime,
  durationHours,
  onTimeChange,
  minDuration = 1,
  maxDuration = 6
}: TableTimelinePickerProps) {
  const containerRef = useRef<HTMLDivElement>(null);

  // Helper: Convert "HH:mm" to minutes from 00:00
  const timeToMinutes = (timeStr: string) => {
    const [h, m] = timeStr.split(':').map(Number);
    return h * 60 + (m || 0);
  };

  // Helper: Convert minutes to "HH:mm"
  const minutesToTime = (mins: number) => {
    const h = Math.floor(mins / 60);
    const m = mins % 60;
    return `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}`;
  };

  const startMins = timeToMinutes(startTime);
  const [localStartMins, setLocalStartMins] = useState(startMins);
  const [isDragging, setIsDragging] = useState(false);

  useEffect(() => {
    if (!isDragging) {
      setLocalStartMins(startMins);
    }
  }, [startMins, isDragging]);

  const isToday = new Date(selectedDate).toDateString() === new Date().toDateString();
  const now = new Date();
  const pastMins = isToday ? now.getHours() * 60 + now.getMinutes() : 0;

  const checkOverlap = (stMins: number, durHours: number) => {
    const endMins = stMins + durHours * 60;
    return existingBookings.some(b => {
      const bStart = timeToMinutes(b.startTime);
      const bEnd = timeToMinutes(b.endTime);
      return (stMins < bEnd && endMins > bStart);
    });
  };

  useEffect(() => {
    const handleMouseMove = (e: MouseEvent) => {
      if (!isDragging || !containerRef.current) return;
      const rect = containerRef.current.getBoundingClientRect();
      const clickX = e.clientX - rect.left;
      let percentage = clickX / rect.width;
      percentage = Math.max(0, Math.min(1, percentage));
      const clickedMins = percentage * 24 * 60;
      
      let snappedMins = Math.round(clickedMins / 30) * 30;
      
      if (isToday && snappedMins < pastMins) {
        snappedMins = Math.ceil(pastMins / 30) * 30;
      }
      
      if (snappedMins + durationHours * 60 > 24 * 60) {
        snappedMins = 24 * 60 - durationHours * 60;
      }

      if (!checkOverlap(snappedMins, durationHours)) {
        setLocalStartMins(snappedMins);
      }
    };

    const handleMouseUp = () => {
      if (isDragging) {
        setIsDragging(false);
        onTimeChange(minutesToTime(localStartMins), durationHours);
      }
    };

    if (isDragging) {
      document.addEventListener('mousemove', handleMouseMove);
      document.addEventListener('mouseup', handleMouseUp);
    }

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
    // Helpers are deterministic for the values listed below.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isDragging, durationHours, pastMins, isToday, localStartMins, onTimeChange, existingBookings]);

  const handleTimelineClick = (e: React.MouseEvent<HTMLDivElement>) => {
    if (isDragging) return;
    if (!containerRef.current) return;
    // Only process click if clicking on the container or grid, not on the draggable block itself
    if ((e.target as HTMLElement).closest('.ttp-block-selected')) return;

    const rect = containerRef.current.getBoundingClientRect();
    const clickX = e.clientX - rect.left;
    const percentage = clickX / rect.width;
    const clickedMins = percentage * 24 * 60;
    
    let snappedMins = Math.round(clickedMins / 30) * 30;
    
    if (isToday && snappedMins < pastMins) {
      snappedMins = Math.ceil(pastMins / 30) * 30;
    }
    
    if (snappedMins + durationHours * 60 > 24 * 60) {
      snappedMins = 24 * 60 - durationHours * 60;
    }

    if (!checkOverlap(snappedMins, durationHours)) {
      onTimeChange(minutesToTime(snappedMins), durationHours);
    }
  };

  const handleBlockMouseDown = (e: React.MouseEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(true);
  };

  const endMins = localStartMins + durationHours * 60;
  const isOverlap = checkOverlap(localStartMins, durationHours);

  return (
    <div className="ttp-wrapper">
      <div className="ttp-header">
        <span>00:00</span>
        <span>12:00</span>
        <span>23:59</span>
      </div>
      
      <div className="ttp-container" ref={containerRef} onClick={handleTimelineClick}>
        {/* Grid lines */}
        {Array.from({ length: 24 }, (_, i) => i).map(h => (
          <div key={h} className="ttp-grid-line" style={{ left: `${(h / 24) * 100}%` }}>
            <span className="ttp-hour-label">{h}</span>
          </div>
        ))}

        {/* Past Time Block (Gray) */}
        {isToday && pastMins > 0 && (
          <div 
            className="ttp-block-past" 
            style={{ left: 0, width: `${(Math.min(pastMins, 24*60) / (24 * 60)) * 100}%` }}
            title="Thời gian trong quá khứ"
          />
        )}

        {/* Existing Bookings (Red Blocks) */}
        {existingBookings.map((b, idx) => {
          const bStart = timeToMinutes(b.startTime);
          const bEnd = timeToMinutes(b.endTime);
          const left = (bStart / (24 * 60)) * 100;
          const width = ((bEnd - bStart) / (24 * 60)) * 100;
          return (
            <div 
              key={idx} 
              className="ttp-block-booked" 
              style={{ left: `${left}%`, width: `${width}%` }}
              title={`Đã đặt: ${b.startTime} - ${b.endTime}`}
            />
          );
        })}

        {/* User Selection Block (Blue Block) - Draggable */}
        <div 
          className={`ttp-block-selected ${isDragging ? 'dragging' : ''} ${isOverlap ? 'overlap' : ''}`}
          style={{ 
            left: `${(localStartMins / (24 * 60)) * 100}%`, 
            width: `${(durationHours * 60 / (24 * 60)) * 100}%` 
          }}
          onMouseDown={handleBlockMouseDown}
        >
          <span className="ttp-selected-text">{minutesToTime(localStartMins)} - {minutesToTime(endMins)}</span>
        </div>
      </div>
      
      {isOverlap && (
        <div className="ttp-error">Khoảng thời gian này đã có người đặt. Vui lòng kéo thanh chọn sang giờ khác!</div>
      )}
    </div>
  );
}
