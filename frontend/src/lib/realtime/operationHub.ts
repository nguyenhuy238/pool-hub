"use client";

import * as signalR from "@microsoft/signalr";
import { API_BASE_URL } from "@/lib/api/client";

export type OperationRealtimeStatus = "connecting" | "connected" | "reconnecting" | "disconnected" | "fallback";

export type OperationEventPayload = {
  eventType?: string;
  sessionId?: number;
  bookingId?: number;
  tableId?: number;
  updatedAtUtc?: string;
};

export type OperationHubHandlers = {
  onSessionUpdated?: (payload: OperationEventPayload) => void;
  onOrderUpdated?: (payload: OperationEventPayload) => void;
  onBookingUpdated?: (payload: OperationEventPayload) => void;
  onTableStatusChanged?: (payload: OperationEventPayload) => void;
  onStatusChange?: (status: OperationRealtimeStatus) => void;
};

export function connectOperationHub(handlers: OperationHubHandlers) {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${API_BASE_URL}/hubs/operation`, { withCredentials: true })
    .withAutomaticReconnect()
    .build();

  const setStatus = (status: OperationRealtimeStatus) => handlers.onStatusChange?.(status);

  connection.on("SessionStarted", (payload: OperationEventPayload) => handlers.onSessionUpdated?.(payload));
  connection.on("SessionUpdated", (payload: OperationEventPayload) => handlers.onSessionUpdated?.(payload));
  connection.on("SessionTransferred", (payload: OperationEventPayload) => handlers.onSessionUpdated?.(payload));
  connection.on("SessionClosed", (payload: OperationEventPayload) => handlers.onSessionUpdated?.(payload));
  connection.on("OrderUpdated", (payload: OperationEventPayload) => handlers.onOrderUpdated?.(payload));
  connection.on("BookingUpdated", (payload: OperationEventPayload) => handlers.onBookingUpdated?.(payload));
  connection.on("TableStatusChanged", (payload: OperationEventPayload) => handlers.onTableStatusChanged?.(payload));

  connection.onreconnecting(() => setStatus("reconnecting"));
  connection.onreconnected(() => setStatus("connected"));
  connection.onclose(() => setStatus("fallback"));

  setStatus("connecting");
  connection
    .start()
    .then(() => setStatus("connected"))
    .catch(() => setStatus("fallback"));

  return () => {
    connection.off("SessionUpdated");
    connection.off("SessionStarted");
    connection.off("SessionTransferred");
    connection.off("SessionClosed");
    connection.off("OrderUpdated");
    connection.off("BookingUpdated");
    connection.off("TableStatusChanged");
    void connection.stop();
  };
}
