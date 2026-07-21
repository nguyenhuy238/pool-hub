import { expect, test } from "@playwright/test";
import { normalizeActiveSessions } from "../src/lib/activeSessions";
import type { Session } from "../src/types";

test.describe("active session normalization", () => {
  test("merges duplicate session rows without losing table assignments", () => {
    const rows: Session[] = [
      {
        sessionId: 10,
        sessionCode: "SS20260718035525",
        status: 1,
        startedAtUtc: "2026-07-18T03:55:25.000Z",
        activeAssignments: [{ assignmentId: 101, tableId: 4, tableName: "Table 04" }]
      },
      {
        sessionId: 10,
        sessionCode: "SS20260718035525",
        status: 1,
        startedAtUtc: "2026-07-18T03:55:25.000Z",
        activeAssignments: [{ assignmentId: 102, tableId: 5, tableName: "Table 05" }]
      }
    ];

    const result = normalizeActiveSessions(rows);

    expect(result).toHaveLength(1);
    expect(result[0].activeTableCount).toBe(2);
    expect(result[0].activeAssignments?.map((assignment) => assignment.tableName)).toEqual(["Table 04", "Table 05"]);
    expect(result[0].releasedTableCount).toBe(0);
  });

  test("keeps one row after partial release and excludes released tables from active list", () => {
    const rows: Session[] = [
      {
        sessionId: 10,
        sessionCode: "SS20260718035525",
        status: 1,
        startedAtUtc: "2026-07-18T03:55:25.000Z",
        activeAssignments: [{ assignmentId: 102, tableId: 5, tableName: "Table 05" }]
      },
      {
        sessionId: 10,
        sessionCode: "SS20260718035525",
        status: 1,
        startedAtUtc: "2026-07-18T03:55:25.000Z",
        releasedAssignments: [{
          assignmentId: 101,
          tableId: 4,
          tableName: "Table 04",
          startedAtUtc: "2026-07-18T03:55:25.000Z",
          endedAtUtc: "2026-07-18T04:15:25.000Z",
          durationMinutes: 20,
          hourlyRateSnapshot: 90000,
          amount: 45000
        }]
      }
    ];

    const result = normalizeActiveSessions(rows);

    expect(result).toHaveLength(1);
    expect(result[0].activeAssignments?.map((assignment) => assignment.tableName)).toEqual(["Table 05"]);
    expect(result[0].releasedTableCount).toBe(1);
    expect(result[0].releasedAssignments?.[0].assignmentId).toBe(101);
  });
});
