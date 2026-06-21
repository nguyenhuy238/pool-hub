"use client";

import { useState, useEffect, useRef } from "react";
import Link from "next/link";
import { miscApi } from "@/lib/api/endpoints";
import { useAuth } from "@/components/auth-provider";

export function NotificationDropdown() {
  const { user } = useAuth();
  const [open, setOpen] = useState(false);
  const [count, setCount] = useState(0);
  const [notifications, setNotifications] = useState<any[]>([]);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!user) return;
    miscApi.notificationUnreadCount()
      .then(res => setCount(res.count))
      .catch(() => {});
  }, [user]);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (ref.current && !ref.current.contains(event.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const toggleDropdown = async () => {
    if (!open) {
      try {
        const data = await miscApi.notifications();
        setNotifications((data as any).items || data || []);
      } catch (err) {}
    }
    setOpen(!open);
  };

  const markAsRead = async (id: number) => {
    await miscApi.notificationRead(id);
    setNotifications(prev => prev.map(n => n.notificationId === id ? { ...n, isRead: true } : n));
    setCount(c => Math.max(0, c - 1));
  };

  const markAllAsRead = async () => {
    await miscApi.notificationReadAll();
    setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
    setCount(0);
  };

  return (
    <div ref={ref} style={{ position: "relative" }}>
      <button className="icon-btn ghost-btn" onClick={toggleDropdown} style={{ position: "relative" }}>
        🔔
        {count > 0 && (
          <span style={{ position: "absolute", top: 0, right: 0, background: "red", color: "white", borderRadius: "50%", padding: "2px 6px", fontSize: "10px", fontWeight: "bold" }}>
            {count}
          </span>
        )}
      </button>

      {open && (
        <div style={{ position: "absolute", top: "100%", right: 0, width: "320px", background: "white", border: "1px solid #ddd", borderRadius: "8px", boxShadow: "0 4px 12px rgba(0,0,0,0.1)", zIndex: 100, overflow: "hidden" }}>
          <div style={{ padding: "12px 16px", borderBottom: "1px solid #eee", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
            <strong>Thông báo</strong>
            {count > 0 && <button className="ghost-btn" style={{ fontSize: "12px", padding: "4px 8px" }} onClick={markAllAsRead}>Đánh dấu đã đọc tất cả</button>}
          </div>
          <div style={{ maxHeight: "300px", overflowY: "auto" }}>
            {notifications.length === 0 ? (
              <div style={{ padding: "20px", textAlign: "center", color: "#888" }}>Không có thông báo</div>
            ) : (
              notifications.map((n: any) => (
                <div key={n.notificationId} onClick={() => { if (!n.isRead) markAsRead(n.notificationId); }} style={{ padding: "12px 16px", borderBottom: "1px solid #eee", cursor: "pointer", background: n.isRead ? "white" : "#f0f8ff" }}>
                  <div style={{ fontWeight: n.isRead ? "normal" : "bold", fontSize: "14px", marginBottom: "4px" }}>{n.title}</div>
                  <div style={{ fontSize: "12px", color: "#555" }}>{n.message}</div>
                </div>
              ))
            )}
          </div>
          <div style={{ padding: "8px", textAlign: "center", borderTop: "1px solid #eee", background: "#f9f9f9" }}>
            <Link href="/operation/notifications" onClick={() => setOpen(false)} style={{ fontSize: "13px", color: "var(--primary)", textDecoration: "none" }}>Xem tất cả thông báo</Link>
          </div>
        </div>
      )}
    </div>
  );
}
