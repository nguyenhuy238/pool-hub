"use client";

import Link from "next/link";
import { pricingApi } from "@/lib/api/endpoints";
import { money } from "@/lib/status";
import { useList, useLoad } from "@/components/ui";
import type { PricingPlan, PricingPlanRule } from "@/types";

export default function HomePage() {
  const { data } = useLoad(async () => {
    const [plans, rules] = await Promise.all([pricingApi.plans(), pricingApi.rules()]);
    return { plans, rules };
  }, []);
  const plans = useList<PricingPlan>(data?.plans);
  const rules = useList<PricingPlanRule>(data?.rules);

  return (
    <>
      <section className="hero">
        <div className="hero-content">
          <h1>PoolHub – Quản lý & đặt bàn bi-a thông minh</h1>
          <p>Trải nghiệm đặt bàn nhanh, vận hành phiên chơi, order đồ uống và thanh toán thống nhất cho billiard, bar và game center.</p>
          <div className="hero-actions">
            <Link className="primary-btn" href="/booking">Đặt bàn ngay</Link>
            <Link className="secondary-btn" href="/pricing">Xem bảng giá</Link>
          </div>
        </div>
      </section>
      <section className="section">
        <h2>Dịch vụ</h2>
        <div className="section-grid">
          {["Billiard", "Đồ uống", "Game Center"].map((item) => <div className="card" key={item}><h2>{item}</h2><p>Quản lý liền mạch từ đặt lịch đến hóa đơn.</p></div>)}
        </div>
      </section>
      <section className="section">
        <h2>Quy trình</h2>
        <div className="section-grid">
          {["Chọn bàn", "Đặt lịch", "Đến chơi", "Thanh toán"].map((item, index) => <div className="card" key={item}><h2>{index + 1}. {item}</h2><p>Thông tin được đồng bộ với hệ thống vận hành.</p></div>)}
        </div>
      </section>
      <section className="section">
        <h2>Bảng giá cơ bản</h2>
        <div className="section-grid">
          {plans.slice(0, 3).map((plan) => {
            const rule = rules.find((item) => item.pricingPlanId === plan.pricingPlanId);
            return <div className="card" key={plan.pricingPlanId}><h2>{plan.name}</h2><p>{rule ? `${money(rule.hourlyRate)} / giờ` : "Liên hệ quầy để biết chi tiết"}</p></div>;
          })}
          {!plans.length ? <div className="state-card">Chưa có bảng giá công khai hoặc API đang tắt.</div> : null}
        </div>
      </section>
    </>
  );
}
