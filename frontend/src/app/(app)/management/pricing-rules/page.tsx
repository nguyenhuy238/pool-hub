"use client";

import { pricingApi } from "@/lib/api/endpoints";
import { money } from "@/lib/status";
import { DataTable, PageHeader, SmartForm, StateBlock, useList, useLoad } from "@/components/ui";
import type { PricingPlanRule } from "@/types";

export default function PricingRulesPage() {
  const { data, loading, error, reload } = useLoad(() => pricingApi.rules(), []);
  const rows = useList<PricingPlanRule>(data);

  return (
    <>
      <PageHeader title="Quy tắc tính giá" description="Quản lý quy tắc tính giá theo từng bảng giá." />
      <SmartForm<PricingPlanRule>
        title="Tạo quy tắc tính giá"
        initial={{ minimumMinutes: 30, billingBlockMinutes: 15 }}
        fields={[
          { name: "pricingPlanId", label: "Plan ID", type: "number", required: true },
          { name: "tableTypeId", label: "Table Type ID", type: "number", required: true },
          { name: "dayOfWeek", label: "Day of week", type: "number", required: true },
          { name: "hourlyRate", label: "Giá/giờ", type: "number", required: true }
        ]}
        onSubmit={async (value) => { await pricingApi.createRule(Number(value.pricingPlanId), value); reload(); }}
      />
      <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
      <DataTable rows={rows as unknown as Record<string, unknown>[]} columns={[
        { key: "pricingPlanId", label: "Plan" },
        { key: "tableTypeId", label: "Loại bàn" },
        { key: "dayOfWeek", label: "Thứ" },
        { key: "hourlyRate", label: "Giá", render: (row) => money(Number(row.hourlyRate)) }
      ]} actions={(row) => <button className="danger-btn" onClick={() => pricingApi.deleteRule(Number(row.pricingPlanId), Number(row.pricingPlanRuleId)).then(() => reload())}>Delete</button>} />
    </>
  );
}
