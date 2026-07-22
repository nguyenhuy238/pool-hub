import { expect, test } from "@playwright/test";
import { calculateDurationMinutes, generateBookingSlots, generateFullDaySlots, slotToUtcIso, validateSlotRange } from "../src/lib/timeSlots";

test("full-day and overnight slots preserve dates, UTC and duration", () => {
  const fullDay = generateFullDaySlots();
  expect(fullDay).toHaveLength(48);
  expect(fullDay[0].displayLabel).toBe("00:00");
  expect(fullDay[47].displayLabel).toBe("23:30");

  const slots = generateBookingSlots({ startDate: "2026-06-29", overnightEnabled: true });
  const start = slots.find((slot) => slot.time === "23:00" && slot.dayOffset === 0);
  const end = slots.find((slot) => slot.time === "02:00" && slot.dayOffset === 1);

  expect(end?.displayLabel).toBe("02:00 (+1)");
  expect(slotToUtcIso(start!)).toBe("2026-06-29T16:00:00.000Z");
  expect(slotToUtcIso(end!)).toBe("2026-06-29T19:00:00.000Z");
  expect(calculateDurationMinutes(start, end)).toBe(180);
  expect(validateSlotRange(start, end, true).valid).toBe(true);
});

test("same-day earlier end is rejected without overnight mode", () => {
  const slots = generateBookingSlots({ startDate: "2026-06-29", overnightEnabled: false });
  const start = slots.find((slot) => slot.time === "23:00");
  const end = slots.find((slot) => slot.time === "02:00");
  const result = validateSlotRange(start, end, false);

  expect(result.valid).toBe(false);
  expect(result.message).toContain("Đặt qua đêm");
});
