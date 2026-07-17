import { apiFetch, toQuery } from "@/lib/api/client";
import { vietnamDateTimeToUtcIso } from "@/lib/dateTime";
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
  const start = new Date(vietnamDateTimeToUtcIso(request.bookingDate, request.startTime));
  const end = new Date(start.getTime() + request.durationHours * 60 * 60 * 1000);

  return {
    customerName: request.customerName,
    phoneNumber: request.phoneNumber,
    email: request.email,
    tableId: request.tableId,
    tableTypeId: request.tableTypeId,
    startTimeUtc: start.toISOString(),
    endTimeUtc: end.toISOString(),
    numberOfGuests: request.numberOfGuests
  };
}

export interface PublicBookingSlot {
  startTimeUtc: string;
  endTimeUtc: string;
  status?: number;
}

export interface BookingAvailabilityItem {
  tableId: number;
  estimatedAmount?: number;
  depositRequiredAmount?: number;
  depositPercent?: number;
  requiresApproval?: boolean;
  conflictDetails?: string[];
}

export const publicBookingApi = {
  availability: (tableId: number, startTimeUtc: string, endTimeUtc: string) =>
    apiFetch<BookingAvailabilityItem[]>(`/api/bookings/availability${toQuery({ tableId, startTimeUtc, endTimeUtc })}`, { skipAuth: true }),
  create: (request: PublicBookingRequest) => apiFetch<Booking>("/api/bookings/public", {
    method: "POST",
    body: JSON.stringify(toBookingPayload(request)),
    skipAuth: true
  }),
  getBooking: (bookingId: number) => apiFetch<Booking>(`/api/bookings/public/${bookingId}`, { skipAuth: true }),
  submitDepositTransfer: (bookingId: number) => apiFetch<Booking>(`/api/bookings/${bookingId}/deposit/submit-transfer`, {
    method: "POST",
    skipAuth: true
  }),
  getPublicCalendar: (tableId: number, date: string) => apiFetch<PublicBookingSlot[]>(`/api/bookings/public/calendar?tableId=${tableId}&date=${date}`, { skipAuth: true })
};
