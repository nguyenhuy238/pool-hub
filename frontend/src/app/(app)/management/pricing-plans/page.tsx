"use client";
import { FormEvent, useState } from "react";
import { pricingApi, venueApi } from "@/lib/api/endpoints";
import { getTotalPages } from "@/lib/api/client";
import { money } from "@/lib/status";
import { ConfirmDialog, DataTable, Modal, PageHeader, SmartForm, StateBlock, useList, useLoad, ListControls, Pagination } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { PricingPlan, PricingPlanRule, TableType } from "@/types";

const DAY_TYPES = [
  { value: 1, label: "Ngày thường (T2 - T6)" },
  { value: 2, label: "Cuối tuần (T7 - CN)" },
  { value: 3, label: "Ngày lễ" },
  { value: 4, label: "Ngày đặc biệt" }
];

export default function PricingPlansPage() {
  const toast = useToast();
  const [planParams, setPlanParams] = useState<Record<string, string | number | boolean>>({ search: "", pageNumber: 1, pageSize: 10, isActive: true });
  const [ruleParams, setRuleParams] = useState<Record<string, string | number | boolean>>({ search: "", pageNumber: 1, pageSize: 10, pricingPlanId: "", tableTypeId: "", dayType: "", sortBy: "", sortDir: "" });
  const [editingPlan, setEditingPlan] = useState<PricingPlan | null>(null);
  const [deletingPlan, setDeletingPlan] = useState<PricingPlan | null>(null);
  const [detailPlan, setDetailPlan] = useState<PricingPlan | null>(null);
  const [editingRule, setEditingRule] = useState<PricingPlanRule | null>(null);
  const [deletingRule, setDeletingRule] = useState<PricingPlanRule | null>(null);
  const { data, loading, error, reload } = useLoad(
    async () => ({
      plans: await pricingApi.plans(planParams),
      rules: await pricingApi.rules(ruleParams),
      tableTypes: await venueApi.tableTypes(),
    }),
    [ruleParams, planParams]
  );

  const plans = useList<PricingPlan>(data?.plans);
  const rules = useList<PricingPlanRule>(data?.rules);
  const tableTypes = useList<TableType>(data?.tableTypes);

  // Map to select options
  const planOptions = plans.map(p => ({ value: String(p.pricingPlanId), label: p.name }));
  const tableTypeOptions = tableTypes.map(t => ({ value: String(t.tableTypeId), label: t.name }));

  async function deletePlan() {
    if (!deletingPlan) return;
    try {
      await pricingApi.deletePlan(deletingPlan.pricingPlanId);
      toast("Đã xóa bảng giá.", "success");
      setDeletingPlan(null);
      await reload();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể xóa bảng giá.", "error");
    }
  }

  async function deleteRule() {
    if (!deletingRule) return;
    try {
      await pricingApi.deleteRule(deletingRule.pricingPlanId, deletingRule.pricingPlanRuleId);
      toast("Đã xóa quy tắc tính giá.", "success");
      setDeletingRule(null);
      await reload();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể xóa quy tắc.", "error");
    }
  }

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


      <StateBlock loading={loading} error={error} empty={!loading && !plans.length} />

      <ListControls 
        search={String(planParams.search)} pageNumber={Number(planParams.pageNumber)} pageSize={Number(planParams.pageSize)} 
        onChange={p => setPlanParams({ ...planParams, ...p })}
        extra={
          <label>
            <span>Trạng thái</span>
            <select value={planParams.isActive === true ? "true" : planParams.isActive === false ? "false" : ""} onChange={e => {
              const val = e.target.value === "true" ? true : e.target.value === "false" ? false : "";
              setPlanParams({ ...planParams, isActive: val, pageNumber: 1 });
            }}>
              <option value="">Tất cả</option>
              <option value="true">Đang hoạt động</option>
              <option value="false">Ngừng hoạt động</option>
            </select>
          </label>
        }
      />

      <DataTable
        rows={plans.map(p => ({ ...p, id: p.pricingPlanId })) as unknown as Record<string, unknown>[]}
        columns={[
          { key: "pricingPlanId", label: "ID" },
          { key: "name", label: "Tên" },
          { key: "isDefault", label: "Mặc định", render: (row) => row.isDefault ? "Có" : "Không" },
          { key: "isActive", label: "Trạng thái", render: (row) => row.isActive === false ? "Ngừng hoạt động" : "Đang hoạt động" }
        ]}
        actions={(row) => {
          const plan = row as unknown as PricingPlan;
          return <div className="action-group">
            <button className="ghost-btn compact" onClick={() => setDetailPlan(plan)}>Chi tiết</button>
            <button className="ghost-btn compact" onClick={() => setEditingPlan(plan)}>Sửa</button>
            <button className="danger-btn compact" disabled={plan.isDefault} onClick={() => setDeletingPlan(plan)}>Xóa</button>
          </div>;
        }}
      />

      <Pagination 
        pageNumber={Number(planParams.pageNumber)} 
        totalPages={getTotalPages(data?.plans, Number(planParams.pageSize))}
        onChange={(page) => setPlanParams(prev => ({ ...prev, pageNumber: page }))} 
      />

      <br />
      <h2>Quy tắc tính giá</h2>

      <SmartForm<PricingPlanRule>
        title="Tạo quy tắc tính giá"
        initial={{ minimumMinutes: 0, billingBlockMinutes: 1 } as any}
        fields={[
          { name: "pricingPlanId", label: "Bảng giá", options: planOptions, required: true },
          { name: "tableTypeId", label: "Loại bàn", options: tableTypeOptions, required: true },
          { name: "dayType", label: "Loại ngày", options: DAY_TYPES.map(d => ({ value: d.value.toString(), label: d.label })), required: true },
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

      <ListControls 
        search={String(ruleParams.search)} pageNumber={Number(ruleParams.pageNumber)} pageSize={Number(ruleParams.pageSize)} 
        onChange={p => setRuleParams({ ...ruleParams, ...p })}
        extra={
          <>
            <label><span>Bảng giá</span><select value={String(ruleParams.pricingPlanId || "")} onChange={e => setRuleParams({ ...ruleParams, pricingPlanId: e.target.value, pageNumber: 1 })}><option value="">Tất cả</option>{planOptions.map(p => <option key={p.value} value={p.value}>{p.label}</option>)}</select></label>
            <label><span>Loại bàn</span><select value={String(ruleParams.tableTypeId || "")} onChange={e => setRuleParams({ ...ruleParams, tableTypeId: e.target.value, pageNumber: 1 })}><option value="">Tất cả</option>{tableTypeOptions.map(p => <option key={p.value} value={p.value}>{p.label}</option>)}</select></label>
            <label><span>Loại ngày</span><select value={String(ruleParams.dayType || "")} onChange={e => setRuleParams({ ...ruleParams, dayType: e.target.value, pageNumber: 1 })}><option value="">Tất cả</option>{DAY_TYPES.map(p => <option key={p.value} value={p.value}>{p.label}</option>)}</select></label>
            <label>
              <span>Sắp xếp</span>
              <select value={`${ruleParams.sortBy || ""}_${ruleParams.sortDir || ""}`} onChange={e => {
                const [sb, sd] = e.target.value.split("_");
                setRuleParams({ ...ruleParams, sortBy: sb || "", sortDir: sd || "", pageNumber: 1 });
              }}>
                <option value="_">Mặc định</option>
                <option value="hourlyrate_asc">Giá (Thấp đến cao)</option>
                <option value="hourlyrate_desc">Giá (Cao đến thấp)</option>
                <option value="starttime_asc">Giờ bắt đầu (Sớm đến muộn)</option>
                <option value="starttime_desc">Giờ bắt đầu (Muộn đến sớm)</option>
              </select>
            </label>
          </>
        }
      />

      <DataTable
        rows={rules.map(r => ({ ...r, id: r.pricingPlanRuleId || Math.random() })) as unknown as Record<string, unknown>[]}
        columns={[
          { key: "pricingPlanId", label: "Bảng giá", render: (row) => plans.find(p => p.pricingPlanId === Number(row.pricingPlanId))?.name || String(row.pricingPlanId) },
          { key: "tableTypeId", label: "Loại bàn", render: (row) => tableTypes.find(t => t.tableTypeId === Number(row.tableTypeId))?.name || String(row.tableTypeId) },
          { key: "dayType", label: "Loại ngày", render: (row) => DAY_TYPES.find(d => d.value === Number(row.dayType))?.label || String(row.dayType) },
          { key: "startTime", label: "Giờ bắt đầu" },
          { key: "endTime", label: "Giờ kết thúc" },
          { key: "hourlyRate", label: "Giá", render: (row) => money(Number(row.hourlyRate)) }
        ]}
        actions={(row) => {
          const rule = row as unknown as PricingPlanRule;
          return <div className="action-group">
            <button className="ghost-btn compact" onClick={() => setEditingRule(rule)}>Sửa</button>
            <button className="danger-btn compact" onClick={() => setDeletingRule(rule)}>Xóa</button>
          </div>;
        }}
      />
      <Pagination 
        pageNumber={Number(ruleParams.pageNumber)} 
        totalPages={getTotalPages(data?.rules, Number(ruleParams.pageSize))}
        onChange={(page) => setRuleParams(prev => ({ ...prev, pageNumber: page }))} 
      />
      {editingPlan ? <PlanFormModal plan={editingPlan} onClose={() => setEditingPlan(null)} onSaved={async () => { setEditingPlan(null); await reload(); }} /> : null}
      {detailPlan ? <PlanDetailModal plan={detailPlan} onClose={() => setDetailPlan(null)} /> : null}
      {deletingPlan ? <ConfirmDialog title="Xóa bảng giá" message={`Xóa bảng giá “${deletingPlan.name}”? Backend sẽ từ chối nếu còn quy tắc đang hoạt động hoặc đây là bảng giá mặc định.`} confirmLabel="Xóa" danger onCancel={() => setDeletingPlan(null)} onConfirm={deletePlan} /> : null}
      {editingRule ? <RuleFormModal rule={editingRule} plans={plans} tableTypes={tableTypes} onClose={() => setEditingRule(null)} onSaved={async () => { setEditingRule(null); await reload(); }} /> : null}
      {deletingRule ? <ConfirmDialog title="Xóa quy tắc tính giá" message="Xóa quy tắc tính giá này?" confirmLabel="Xóa" danger onCancel={() => setDeletingRule(null)} onConfirm={deleteRule} /> : null}
    </>
  );
}

function PlanDetailModal({ plan, onClose }: { plan: PricingPlan; onClose: () => void }) {
  return <Modal title={`Chi tiết bảng giá — ${plan.name}`} onClose={onClose}>
    <div className="audit-detail-grid">
      <div><span>ID</span><strong>{plan.pricingPlanId}</strong></div>
      <div><span>Mặc định</span><strong>{plan.isDefault ? "Có" : "Không"}</strong></div>
      <div><span>Trạng thái</span><strong>{plan.isActive === false ? "Ngừng hoạt động" : "Đang hoạt động"}</strong></div>
    </div>
    <div className="modal-actions"><button className="ghost-btn" onClick={onClose}>Đóng</button></div>
  </Modal>;
}

function PlanFormModal({ plan, onClose, onSaved }: { plan: PricingPlan; onClose: () => void; onSaved: () => Promise<void> }) {
  const toast = useToast();
  const [form, setForm] = useState({ name: plan.name, isDefault: Boolean(plan.isDefault), isActive: plan.isActive !== false });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!form.name.trim()) return setError("Vui lòng nhập tên bảng giá.");
    setSaving(true);
    try {
      await pricingApi.updatePlan(plan.pricingPlanId, { ...form, name: form.name.trim() });
      toast("Đã cập nhật bảng giá.", "success");
      await onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể lưu bảng giá.");
    } finally {
      setSaving(false);
    }
  }
  return <Modal title="Sửa bảng giá" onClose={onClose}>
    <form className="form-stack" onSubmit={submit}>
      {error ? <div className="inline-alert error">{error}</div> : null}
      <label><span>Tên bảng giá</span><input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} /></label>
      <label className="check-option"><input type="checkbox" checked={form.isDefault} onChange={(event) => setForm({ ...form, isDefault: event.target.checked })} />Đặt làm mặc định</label>
      <label className="check-option"><input type="checkbox" checked={form.isActive} onChange={(event) => setForm({ ...form, isActive: event.target.checked })} />Đang hoạt động</label>
      <div className="modal-actions"><button type="button" className="ghost-btn" onClick={onClose}>Hủy</button><button className="primary-btn" disabled={saving}>{saving ? "Đang lưu..." : "Lưu"}</button></div>
    </form>
  </Modal>;
}

function RuleFormModal({ rule, plans, tableTypes, onClose, onSaved }: { rule: PricingPlanRule; plans: PricingPlan[]; tableTypes: TableType[]; onClose: () => void; onSaved: () => Promise<void> }) {
  const toast = useToast();
  const [form, setForm] = useState({ ...rule });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!form.pricingPlanId || !form.tableTypeId) return setError("Vui lòng chọn bảng giá và loại bàn.");
    if (!form.startTime || !form.endTime || String(form.startTime) >= String(form.endTime)) return setError("Giờ kết thúc phải lớn hơn giờ bắt đầu.");
    if (Number(form.hourlyRate) <= 0 || Number(form.minimumMinutes) < 0 || Number(form.billingBlockMinutes) <= 0) return setError("Giá phải lớn hơn 0, phút tối thiểu không được âm và block tính tiền phải lớn hơn 0.");
    setSaving(true);
    try {
      await pricingApi.updateRule(rule.pricingPlanId, rule.pricingPlanRuleId, form);
      toast("Đã cập nhật quy tắc tính giá.", "success");
      await onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể lưu quy tắc.");
    } finally {
      setSaving(false);
    }
  }
  return <Modal title="Sửa quy tắc tính giá" onClose={onClose} size="large">
    <form className="form-grid modal-form" onSubmit={submit}>
      {error ? <div className="inline-alert error full-field">{error}</div> : null}
      <label><span>Bảng giá</span><select value={form.pricingPlanId} disabled>{plans.map((plan) => <option key={plan.pricingPlanId} value={plan.pricingPlanId}>{plan.name}</option>)}</select></label>
      <label><span>Loại bàn</span><select value={form.tableTypeId} onChange={(event) => setForm({ ...form, tableTypeId: Number(event.target.value) })}>{tableTypes.map((type) => <option key={type.tableTypeId} value={type.tableTypeId}>{type.name}</option>)}</select></label>
      <label><span>Loại ngày</span><select value={form.dayType} onChange={(event) => setForm({ ...form, dayType: Number(event.target.value) })}>{DAY_TYPES.map((day) => <option key={day.value} value={day.value}>{day.label}</option>)}</select></label>
      <label><span>Giờ bắt đầu</span><input type="time" value={String(form.startTime ?? "").slice(0, 5)} onChange={(event) => setForm({ ...form, startTime: event.target.value })} /></label>
      <label><span>Giờ kết thúc</span><input type="time" value={String(form.endTime ?? "").slice(0, 5)} onChange={(event) => setForm({ ...form, endTime: event.target.value })} /></label>
      <label><span>Giá/giờ</span><input type="number" min={1} value={form.hourlyRate} onChange={(event) => setForm({ ...form, hourlyRate: Number(event.target.value) })} /></label>
      <label><span>Phút tối thiểu</span><input type="number" min={0} value={form.minimumMinutes} onChange={(event) => setForm({ ...form, minimumMinutes: Number(event.target.value) })} /></label>
      <label><span>Block tính tiền</span><input type="number" min={1} value={form.billingBlockMinutes} onChange={(event) => setForm({ ...form, billingBlockMinutes: Number(event.target.value) })} /></label>
      <div className="modal-actions full-field"><button type="button" className="ghost-btn" onClick={onClose}>Hủy</button><button className="primary-btn" disabled={saving}>{saving ? "Đang lưu..." : "Lưu"}</button></div>
    </form>
  </Modal>;
}
