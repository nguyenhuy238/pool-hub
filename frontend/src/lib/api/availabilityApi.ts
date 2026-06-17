import { apiFetch, unwrapList } from "@/lib/api/client";
import { mockPricingPlans, mockPricingRules, mockTableTypes, mockVenueLayout } from "@/lib/mock/landingData";
import type { PricingPlan, PricingPlanRule, TableType, VenueLayoutResponse } from "@/types";

export type LandingAvailability = {
  layout: VenueLayoutResponse;
  tableTypes: TableType[];
  usingMock: boolean;
};

export type LandingPricing = {
  plans: PricingPlan[];
  rules: PricingPlanRule[];
  usingMock: boolean;
};

export const availabilityApi = {
  async getAvailability() {
    try {
      const [layout, tableTypes] = await Promise.all([
        apiFetch<VenueLayoutResponse>("/api/venue-tables/layout", { skipAuth: true }),
        apiFetch<TableType[] | { items?: TableType[] }>("/api/table-types", { skipAuth: true })
      ]);

      return { layout, tableTypes: unwrapList(tableTypes), usingMock: false } satisfies LandingAvailability;
    } catch {
      // TODO: Replace with GET /api/bookings/availability when backend exposes time-slot availability.
      return { layout: mockVenueLayout, tableTypes: mockTableTypes, usingMock: true } satisfies LandingAvailability;
    }
  },

  async getPricing() {
    try {
      const [plans, rules] = await Promise.all([
        apiFetch<PricingPlan[] | { items?: PricingPlan[] }>("/api/pricing-plans", { skipAuth: true }),
        apiFetch<PricingPlanRule[] | { items?: PricingPlanRule[] }>("/api/pricing-plans/rules", { skipAuth: true })
      ]);

      return { plans: unwrapList(plans), rules: unwrapList(rules), usingMock: false } satisfies LandingPricing;
    } catch {
      return { plans: mockPricingPlans, rules: mockPricingRules, usingMock: true } satisfies LandingPricing;
    }
  }
};
