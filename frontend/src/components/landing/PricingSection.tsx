"use client";

import { useEffect, useState } from "react";
import { availabilityApi, type LandingPricing } from "@/lib/api/availabilityApi";
import { money } from "@/lib/status";

export function PricingSection() {
  const [pricing, setPricing] = useState<LandingPricing | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    availabilityApi.getPricing().then(setPricing).finally(() => setLoading(false));
  }, []);

  return (
    <section className="landing-section" id="pricing">
      <div className="section-heading split-heading">
        <div>
          <p className="eyebrow">Bảng giá ưu đãi</p>
          <h2>Giá theo khung giờ, để nhóm bạn chủ động ngân sách</h2>
        </div>
        <div className="golden-hour">Thứ 2 - Thứ 6 trước 17h: ưu đãi tốt hơn</div>
      </div>
      {loading ? <div className="state-card">Đang tải bảng giá...</div> : null}
      <div className="pricing-grid">
        {(pricing?.rules || []).slice(0, 6).map((rule) => {
          const plan = pricing?.plans.find((item) => item.pricingPlanId === rule.pricingPlanId);
          return (
            <article className="price-card" key={rule.pricingPlanRuleId}>
              <span>{plan?.name || "Khung giá"}</span>
              <h3>{money(rule.hourlyRate)} / giờ</h3>
              <p>{rule.startTime?.slice(0, 5) || "09:00"} - {rule.endTime?.slice(0, 5) || "24:00"}</p>
            </article>
          );
        })}
      </div>
      {pricing?.usingMock ? <p className="data-note">Đang hiển thị bảng giá mẫu do API public chưa phản hồi.</p> : null}
    </section>
  );
}
