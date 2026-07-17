import { expect, test } from "@playwright/test";
import { ApiError, apiFetch } from "../src/lib/api/client";
import { venueApi } from "../src/lib/api/endpoints";

test.describe("api client", () => {
  test.afterEach(() => {
    delete (globalThis as { fetch?: typeof fetch }).fetch;
  });

  test("venueApi.layout calls the authenticated floor-map endpoint and unwraps ApiResponse", async () => {
    let requestedUrl = "";
    let requestedInit: RequestInit | undefined;
    globalThis.fetch = async (input: RequestInfo | URL, init?: RequestInit) => {
      requestedUrl = String(input);
      requestedInit = init;
      return new Response(JSON.stringify({
        success: true,
        data: {
          floors: [],
          totalTables: 0,
          availableTables: 0,
          occupiedTables: 0,
          reservedTables: 0,
          maintenanceTables: 0,
          inactiveTables: 0,
          fetchedAtUtc: "2026-07-17T00:00:00.000Z"
        }
      }), { status: 200, headers: { "Content-Type": "application/json" } });
    };

    const result = await venueApi.layout();

    expect(requestedUrl).toBe("http://localhost:5056/api/venue-tables/layout");
    expect(requestedInit?.credentials).toBe("include");
    expect(result.totalTables).toBe(0);
  });

  test("apiFetch preserves status-specific failures for floor-map handling", async () => {
    globalThis.fetch = async () => new Response(JSON.stringify({ success: false, message: "Forbidden" }), { status: 403 });

    const error = await apiFetch("/api/venue-tables/layout").catch((err) => err);

    expect(error).toBeInstanceOf(ApiError);
    expect(error.status).toBe(403);
    expect(error.message).toBe("Forbidden");
  });

  test("apiFetch does not show endpoint placeholder when layout endpoint succeeds", async () => {
    globalThis.fetch = async () => new Response(JSON.stringify({ success: true, data: { floors: [], totalTables: 0 } }), { status: 200 });

    const result = await apiFetch<{ totalTables: number }>("/api/venue-tables/layout");

    expect(result.totalTables).toBe(0);
  });
});
