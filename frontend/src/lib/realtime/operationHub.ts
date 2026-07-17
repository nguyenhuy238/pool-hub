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

  let disposed = false;
  const setStatus = (status: OperationRealtimeStatus) => handlers.onStatusChange?.(status);
  const sessionUpdated = (payload: OperationEventPayload) => handlers.onSessionUpdated?.(payload);
  const orderUpdated = (payload: OperationEventPayload) => handlers.onOrderUpdated?.(payload);
  const bookingUpdated = (payload: OperationEventPayload) => handlers.onBookingUpdated?.(payload);
  const tableStatusChanged = (payload: OperationEventPayload) => handlers.onTableStatusChanged?.(payload);

  connection.on("SessionStarted", sessionUpdated);
  connection.on("SessionUpdated", sessionUpdated);
  connection.on("SessionTransferred", sessionUpdated);
  connection.on("SessionClosed", sessionUpdated);
  connection.on("OrderUpdated", orderUpdated);
  connection.on("BookingUpdated", bookingUpdated);
  connection.on("TableStatusChanged", tableStatusChanged);

  connection.onreconnecting(() => {
    if (!disposed) setStatus("reconnecting");
  });
  connection.onreconnected(() => {
    if (!disposed) setStatus("connected");
  });
  connection.onclose(() => {
    if (!disposed) setStatus("fallback");
  });

  setStatus("connecting");
  const startPromise = connection
    .start()
    .then(async () => {
      if (disposed) {
        await connection.stop();
        return;
      }
      setStatus("connected");
    })
    .catch(() => {
      if (!disposed) setStatus("fallback");
    });

  return () => {
    disposed = true;
    connection.off("SessionUpdated", sessionUpdated);
    connection.off("SessionStarted", sessionUpdated);
    connection.off("SessionTransferred", sessionUpdated);
    connection.off("SessionClosed", sessionUpdated);
    connection.off("OrderUpdated", orderUpdated);
    connection.off("BookingUpdated", bookingUpdated);
    connection.off("TableStatusChanged", tableStatusChanged);
    void startPromise.finally(() => {
      if (connection.state !== signalR.HubConnectionState.Disconnected) {
        void connection.stop().catch(() => undefined);
      }
    });
  };
}
