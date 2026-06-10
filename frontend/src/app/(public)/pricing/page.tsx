"use client";

import { pricingApi } from "@/lib/api/endpoints";
import { money } from "@/lib/status";
import { PageHeader, StateBlock, useList, useLoad } from "@/components/ui";
import type { PricingPlan, PricingPlanRule } from "@/types";

export default function PricingPage() {
  const { data, loading, error } = useLoad(async () => {
    const [plans, rules] = await Promise.all([pricingApi.plans(), pricingApi.rules()]);
    return { plans, rules };
  }, []);
  const plans = useList<PricingPlan>(data?.plans);
  const rules = useList<PricingPlanRule>(data?.rules);
  return (
    <section className="section">
      <PageHeader title="Bảng giá" description="Giá tham khảo lấy trực tiếp từ pricing plans và pricing rules." />
      <StateBlock loading={loading} error={error} empty={!loading && !plans.length} />
      <div className="section-grid">
        {plans.map((plan) => <div className="card" key={plan.pricingPlanId}><h2>{plan.name}</h2>{rules.filter((rule) => rule.pricingPlanId === plan.pricingPlanId).map((rule) => <p key={rule.pricingPlanRuleId}>Thứ {rule.dayOfWeek}: {money(rule.hourlyRate)} / giờ</p>)}</div>)}
      </div>
    </section>
  );
}
