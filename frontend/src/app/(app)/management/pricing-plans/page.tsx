"use client";
import { pricingApi } from "@/lib/api/endpoints";
import { money } from "@/lib/status";
import { DataTable, PageHeader, SmartForm, StateBlock, useList, useLoad } from "@/components/ui";
import type { PricingPlan, PricingPlanRule } from "@/types";
export default function PricingPlansPage() {
  const { data, loading, error, reload } = useLoad(async () => ({ plans: await pricingApi.plans(), rules: await pricingApi.rules() }), []);
  const plans = useList<PricingPlan>(data?.plans);
  const rules = useList<PricingPlanRule>(data?.rules);
  return <><PageHeader title="Pricing Plans" description="Quản lý plan và rule tính tiền." /><SmartForm<PricingPlan> title="Tạo pricing plan" initial={{}} fields={[{ name: "name", label: "Tên", required: true }]} onSubmit={async (value) => { await pricingApi.createPlan(value); reload(); }} /><SmartForm<PricingPlanRule> title="Tạo pricing rule" initial={{}} fields={[{ name: "pricingPlanId", label: "Plan ID", type: "number", required: true }, { name: "tableTypeId", label: "Table Type ID", type: "number", required: true }, { name: "dayOfWeek", label: "Day of week", type: "number", required: true }, { name: "hourlyRate", label: "Giá/giờ", type: "number", required: true }]} onSubmit={async (value) => { await pricingApi.createRule(Number(value.pricingPlanId), value); reload(); }} /><StateBlock loading={loading} error={error} empty={!loading && !plans.length} /><DataTable rows={plans as unknown as Record<string, unknown>[]} columns={[{ key: "pricingPlanId", label: "ID" }, { key: "name", label: "Tên" }]} /><h2>Rules</h2><DataTable rows={rules as unknown as Record<string, unknown>[]} columns={[{ key: "pricingPlanId", label: "Plan" }, { key: "tableTypeId", label: "Loại bàn" }, { key: "dayOfWeek", label: "Thứ" }, { key: "hourlyRate", label: "Giá", render: (row) => money(Number(row.hourlyRate)) }]} /></>;
}
