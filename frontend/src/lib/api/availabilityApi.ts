import { apiFetch, unwrapList } from "@/lib/api/client";
import { landingSettingsApi, type PricingPlanSummary, type PricingRuleSummary } from "@/lib/api/landingSettingsApi";
import type { PricingPlan, PricingPlanRule, TableType, VenueLayoutResponse } from "@/types";

export type LandingAvailability = {
  layout: VenueLayoutResponse;
  tableTypes: TableType[];
  usingMock: boolean;
};

export type LandingPricing = {
  plans: Array<PricingPlan | PricingPlanSummary>;
  rules: Array<PricingPlanRule | PricingRuleSummary>;
  usingMock: boolean;
};

export const availabilityApi = {
  async getAvailability() {
    const [layout, tableTypes] = await Promise.all([
      apiFetch<VenueLayoutResponse>("/api/public/venue-layout", { skipAuth: true }),
      apiFetch<TableType[] | { items?: TableType[] }>("/api/table-types", { skipAuth: true })
    ]);
    return { layout, tableTypes: unwrapList(tableTypes), usingMock: false } satisfies LandingAvailability;
  },

  async getPricing() {
    const summary = await landingSettingsApi.pricingSummary();
    return { plans: summary.plans, rules: summary.rules, usingMock: false } satisfies LandingPricing;
  }
};
