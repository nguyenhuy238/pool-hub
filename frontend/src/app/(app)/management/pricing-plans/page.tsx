"use client";
import { useState } from "react";
import { pricingApi, venueApi } from "@/lib/api/endpoints";
import { money } from "@/lib/status";
import { DataTable, PageHeader, SmartForm, StateBlock, useList, useLoad, ListControls, Pagination } from "@/components/ui";
import type { PricingPlan, PricingPlanRule, TableType } from "@/types";

const DAYS_OF_WEEK = [
  { value: 0, label: "Chủ nhật" },
  { value: 1, label: "Thứ hai" },
  { value: 2, label: "Thứ ba" },
  { value: 3, label: "Thứ tư" },
  { value: 4, label: "Thứ năm" },
  { value: 5, label: "Thứ sáu" },
  { value: 6, label: "Thứ bảy" },
];

export default function PricingPlansPage() {
  const [ruleParams, setRuleParams] = useState({ search: "", pageNumber: 1, pageSize: 10 });
  const { data, loading, error, reload } = useLoad(
    async () => ({
      plans: await pricingApi.plans(),
      rules: await pricingApi.rules(ruleParams),
      tableTypes: await venueApi.tableTypes(),
    }),
    [ruleParams]
  );

  const plans = useList<PricingPlan>(data?.plans);
  const rules = useList<PricingPlanRule>(data?.rules);
  const tableTypes = useList<TableType>(data?.tableTypes);

  // Map to select options
  const planOptions = plans.map(p => ({ value: String(p.pricingPlanId), label: p.name }));
  const tableTypeOptions = tableTypes.map(t => ({ value: String(t.tableTypeId), label: t.name }));

  return (
    <>
      <PageHeader title="Quản lý bảng giá" description="Quản lý các bảng giá và quy tắc tính tiền theo khung giờ." />

      <SmartForm<PricingPlan>
        title="Tạo bảng giá"
        initial={{}}
        fields={[
          { name: "name", label: "Tên", required: true }
        ]}
        onSubmit={async (value) => {
          await pricingApi.createPlan(value);
          reload();
        }}
      />

      <SmartForm<PricingPlanRule>
        title="Tạo quy tắc tính giá"
        initial={{ minimumMinutes: 30, billingBlockMinutes: 15 } as any}
        fields={[
          { name: "pricingPlanId", label: "Plan", options: planOptions, required: true },
          { name: "tableTypeId", label: "Loại bàn", options: tableTypeOptions, required: true },
          { name: "dayOfWeek", label: "Thứ", options: DAYS_OF_WEEK.map(d => ({ value: d.value.toString(), label: d.label })), required: true },
          { name: "startTime", label: "Giờ bắt đầu (HH:mm:ss)", type: "time", required: true },
          { name: "endTime", label: "Giờ kết thúc (HH:mm:ss)", type: "time", required: true },
          { name: "hourlyRate", label: "Giá/giờ", type: "number", required: true },
          { name: "minimumMinutes", label: "Phút tối thiểu", type: "number", required: true },
          { name: "billingBlockMinutes", label: "Block tính tiền (phút)", type: "number", required: true }
        ]}
        onSubmit={async (value) => {
          if (Number(value.hourlyRate) < 0) throw new Error("Giá không được âm.");
          if (Number(value.minimumMinutes) < 0) throw new Error("Phút tối thiểu không hợp lệ.");
          if (Number(value.billingBlockMinutes) <= 0) throw new Error("Block tính tiền phải lớn hơn 0.");
          if (value.startTime && value.endTime && value.startTime >= value.endTime) throw new Error("Giờ kết thúc phải lớn hơn giờ bắt đầu.");

          await pricingApi.createRule(Number(value.pricingPlanId), value);
          reload();
        }}
      />

      <StateBlock loading={loading} error={error} empty={!loading && !plans.length} />

      <DataTable
        rows={plans.map(p => ({ ...p, id: p.pricingPlanId })) as unknown as Record<string, unknown>[]}
        columns={[
          { key: "pricingPlanId", label: "ID" },
          { key: "name", label: "Tên" }
        ]}
      />

      <br />
      <h2>Pricing Rules</h2>
      <DataTable
        rows={rules.map(r => ({ ...r, id: r.pricingPlanRuleId || Math.random() })) as unknown as Record<string, unknown>[]}
        columns={[
          { key: "pricingPlanId", label: "Plan", render: (row) => plans.find(p => p.pricingPlanId === Number(row.pricingPlanId))?.name || String(row.pricingPlanId) },
          { key: "tableTypeId", label: "Loại bàn", render: (row) => tableTypes.find(t => t.tableTypeId === Number(row.tableTypeId))?.name || String(row.tableTypeId) },
          { key: "dayOfWeek", label: "Thứ", render: (row) => DAYS_OF_WEEK.find(d => d.value === Number(row.dayOfWeek))?.label || String(row.dayOfWeek) },
          { key: "startTime", label: "Giờ bắt đầu" },
          { key: "endTime", label: "Giờ kết thúc" },
          { key: "hourlyRate", label: "Giá", render: (row) => money(Number(row.hourlyRate)) }
        ]}
      />
      <Pagination 
        pageNumber={ruleParams.pageNumber} 
        totalPages={(data?.rules as any)?.totalCount ? Math.ceil((data?.rules as any).totalCount / ruleParams.pageSize) : 102}
        onChange={(page) => setRuleParams(prev => ({ ...prev, pageNumber: page }))} 
      />
    </>
  );
}
