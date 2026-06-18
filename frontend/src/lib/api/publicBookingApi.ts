import { apiFetch } from "@/lib/api/client";
import type { Booking } from "@/types";

export type PublicBookingRequest = {
  customerName: string;
  phoneNumber: string;
  email?: string;
  bookingDate: string;
  startTime: string;
  durationHours: number;
  tableTypeId: number;
  tableId?: number;
  numberOfGuests: number;
  note?: string;
};

function toBookingPayload(request: PublicBookingRequest) {
  const start = new Date(`${request.bookingDate}T${request.startTime}`);
  const end = new Date(start.getTime() + request.durationHours * 60 * 60 * 1000);

  return {
    customerName: request.customerName,
    phoneNumber: request.phoneNumber,
    tableId: request.tableId,
    tableTypeId: request.tableTypeId,
    startTimeUtc: start.toISOString(),
    endTimeUtc: end.toISOString(),
    numberOfGuests: request.numberOfGuests
  };
}

export const publicBookingApi = {
  create: (request: PublicBookingRequest) => apiFetch<Booking>("/api/bookings/public", {
    method: "POST",
    body: JSON.stringify(toBookingPayload(request)),
    skipAuth: true
  })
};
