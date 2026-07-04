import { expect, test } from "@playwright/test";
import { localDateRangeToUtcRange } from "../src/lib/dateTime";

test("localDateRangeToUtcRange returns half-open Vietnam day in UTC", () => {
  const range = localDateRangeToUtcRange("2026-07-10");

  expect(range.fromUtc).toBe("2026-07-09T17:00:00.000Z");
  expect(range.toUtc).toBe("2026-07-10T17:00:00.000Z");
  expect(range.toUtc).not.toContain("23:59:59");
});

