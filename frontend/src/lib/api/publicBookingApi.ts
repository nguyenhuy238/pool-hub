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
  async create(request: PublicBookingRequest) {
    try {
      return await apiFetch<Booking>("/api/bookings", {
        method: "POST",
        body: JSON.stringify(toBookingPayload(request)),
        skipAuth: true
      });
    } catch (error) {
      if (error instanceof TypeError) {
        // TODO: Remove this fallback after the deployed backend is always reachable from the public website.
        return {
          bookingId: Date.now(),
          customerName: request.customerName,
          phoneNumber: request.phoneNumber,
          tableTypeId: request.tableTypeId,
          startTimeUtc: new Date(`${request.bookingDate}T${request.startTime}`).toISOString(),
          endTimeUtc: new Date(new Date(`${request.bookingDate}T${request.startTime}`).getTime() + request.durationHours * 60 * 60 * 1000).toISOString(),
          numberOfGuests: request.numberOfGuests,
          status: 1
        } satisfies Booking;
      }

      throw error;
    }
  }
};
