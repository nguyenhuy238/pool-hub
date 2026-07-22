import { addDaysToVietnamDateInput, vietnamDateTimeToUtcIso } from "@/lib/dateTime";

export type BookingTimeSlot = {
  time: string;
  label: string;
  displayLabel: string;
  dayOffset: 0 | 1;
  minutesFromStartDate: number;
  localDate: string;
  localDateTime: string;
};

export function generateFullDaySlots(slotMinutes = 30) {
  return Array.from({ length: Math.floor(24 * 60 / slotMinutes) }, (_, index) => {
    const minutes = index * slotMinutes;
    const time = `${Math.floor(minutes / 60).toString().padStart(2, "0")}:${(minutes % 60).toString().padStart(2, "0")}`;
    return createSlot("", time, 0, minutes);
  });
}

export function generateBookingSlots({ startDate, overnightEnabled, slotMinutes = 30, overnightEndHour = 8 }: {
  startDate: string;
  overnightEnabled: boolean;
  slotMinutes?: number;
  overnightEndHour?: number;
}): BookingTimeSlot[] {
  const nextDate = addDaysToVietnamDateInput(startDate, 1);
  const daySlots = generateFullDaySlots(slotMinutes).map((slot) => createSlot(startDate, slot.time, 0, slot.minutesFromStartDate));
  if (!overnightEnabled) return daySlots;

  const nextDayPointCount = Math.floor(overnightEndHour * 60 / slotMinutes) + 1;
  const nextDaySlots = Array.from({ length: nextDayPointCount }, (_, index) => {
    const minutes = index * slotMinutes;
    const time = `${Math.floor(minutes / 60).toString().padStart(2, "0")}:${(minutes % 60).toString().padStart(2, "0")}`;
    return createSlot(nextDate, time, 1, 24 * 60 + minutes);
  });
  return [...daySlots, ...nextDaySlots];
}

function createSlot(localDate: string, time: string, dayOffset: 0 | 1, minutesFromStartDate: number): BookingTimeSlot {
  return {
    time,
    label: time,
    displayLabel: dayOffset === 1 ? `${time} (+1)` : time,
    dayOffset,
    minutesFromStartDate,
    localDate,
    localDateTime: `${localDate}T${time}`
  };
}

export function slotToUtcIso(slot: BookingTimeSlot) {
  return vietnamDateTimeToUtcIso(slot.localDate, slot.time);
}

export function calculateDurationMinutes(startSlot?: BookingTimeSlot, endSlot?: BookingTimeSlot) {
  if (!startSlot || !endSlot) return 0;
  return Math.max(0, endSlot.minutesFromStartDate - startSlot.minutesFromStartDate);
}

export function formatSlotLabel(slot: BookingTimeSlot) {
  return slot.displayLabel;
}

export function formatSlotDateTime(slot?: BookingTimeSlot) {
  if (!slot) return "-";
  const [year, month, day] = slot.localDate.split("-");
  return `${slot.time} ${day}/${month}/${year}`;
}

export function validateSlotRange(startSlot: BookingTimeSlot | undefined, endSlot: BookingTimeSlot | undefined, overnightEnabled: boolean) {
  if (!startSlot || !endSlot) return { valid: false, message: "Vui lòng chọn đầy đủ giờ bắt đầu và kết thúc." };
  if (!overnightEnabled && endSlot.minutesFromStartDate <= startSlot.minutesFromStartDate) {
    return { valid: false, message: "Giờ kết thúc phải sau giờ bắt đầu. Nếu muốn đặt qua đêm, hãy bật Đặt qua đêm." };
  }
  if (endSlot.minutesFromStartDate <= startSlot.minutesFromStartDate) {
    return { valid: false, message: "Giờ kết thúc phải sau giờ bắt đầu." };
  }
  return { valid: true, message: "" };
}
