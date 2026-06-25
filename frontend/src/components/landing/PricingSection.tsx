"use client";

import React, { useEffect, useState } from "react";
import { venueApi, pricingApi } from "@/lib/api/endpoints";
import type { TableType, PricingPlanRule } from "@/types";

function formatDays(days: number[]) {
  if (!days || days.length === 0) return "Mọi ngày";
  if (days.length === 7) return "Cả Tuần";
  
  const isAllWeekdays = [1, 2, 3, 4, 5].every(d => days.includes(d)) && days.length === 5;
  const isAllWeekend = days.includes(0) && days.includes(6) && days.length === 2;
  
  if (isAllWeekdays) return "Thứ 2 - Thứ 6";
  if (isAllWeekend) return "T7 - CN";
  
  // Sort days: 1 (Mon) to 7 (Sun=0)
  const sorted = [...days].sort((a, b) => (a === 0 ? 7 : a) - (b === 0 ? 7 : b));
  return sorted.map(d => d === 0 ? "CN" : `T${d + 1}`).join(", ");
}

function groupRules(rules: PricingPlanRule[]) {
  const groups = new Map<string, { days: number[], rate: number, start: string, end: string }>();
  
  for (const r of rules) {
    // If dayOfWeek is completely missing, we treat it as everyday (though usually it's set 0-6)
    const d = r.dayOfWeek !== undefined ? r.dayOfWeek : -1;
    const start = r.startTime ? r.startTime.slice(0, 5) : "";
    const end = r.endTime ? r.endTime.slice(0, 5) : "";
    const rate = r.hourlyRate || 0;
    
    const key = `${rate}-${start}-${end}`;
    if (!groups.has(key)) {
      groups.set(key, { days: d !== -1 ? [d] : [0,1,2,3,4,5,6], rate, start, end });
    } else {
      const existing = groups.get(key)!;
      if (d !== -1 && !existing.days.includes(d)) {
        existing.days.push(d);
      }
    }
  }

  return Array.from(groups.values()).map(g => ({
    ...g,
    dayText: formatDays(g.days)
  })).sort((a, b) => {
    // Sort by day grouping then by start time
    if (a.dayText !== b.dayText) return a.dayText.localeCompare(b.dayText);
    return a.start.localeCompare(b.start);
  });
}

function getTableTypeColor(name: string) {
  const n = name.toLowerCase();
  if (n.includes('vip')) return '#8b5cf6'; // Purple
  if (n.includes('standard')) return '#10b981'; // Green
  if (n.includes('carom')) return '#f59e0b'; // Orange
  if (n.includes('snooker')) return '#ef4444'; // Red
  return '#111827'; // Default
}

export function PricingSection() {
  const [tableTypes, setTableTypes] = useState<TableType[]>([]);
  const [rules, setRules] = useState<PricingPlanRule[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        const [typesRes, rulesRes] = await Promise.all([
          venueApi.tableTypes(),
          pricingApi.rules({ pageSize: 500 })
        ]);
        
        const types = Array.isArray(typesRes) ? typesRes : (typesRes as any).items || [];
        const rls = Array.isArray(rulesRes) ? rulesRes : (rulesRes as any).items || [];
        
        setTableTypes(types);
        setRules(rls);
      } catch (error) {
        console.error("Failed to load pricing data", error);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  return (
    <section className="landing-section" id="pricing">
      <div className="section-heading split-heading">
        <div>
          <p className="eyebrow">Bảng giá thực tế</p>
          <h2>Giá chi tiết theo từng loại bàn & khung giờ</h2>
        </div>
        <div className="golden-hour">Giá được đồng bộ trực tiếp từ hệ thống quản lý</div>
      </div>
      
      {loading ? (
        <div style={{ textAlign: 'center', padding: '40px' }}>Đang tải bảng giá...</div>
      ) : tableTypes.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '40px' }}>Vui lòng liên hệ để biết chi tiết.</div>
      ) : (
        <div className="pricing-grid">
          {tableTypes.filter(t => t.isActive !== false).map((tableType) => {
            const typeRules = rules.filter(r => r.tableTypeId === tableType.tableTypeId && r.isActive !== false);
            const groupedRules = groupRules(typeRules);
            
            return (
              <article className="price-card" key={tableType.tableTypeId} style={{ display: 'flex', flexDirection: 'column', gap: '12px', borderTop: `4px solid ${getTableTypeColor(tableType.name)}` }}>
                <span style={{ display: 'inline-block', marginBottom: '8px', fontWeight: 'bold', fontSize: '1.2rem', color: getTableTypeColor(tableType.name) }}>
                  {tableType.name}
                </span>
                
                {groupedRules.length === 0 ? (
                  <p style={{ color: '#6b7280', fontSize: '0.9rem' }}>Chưa cập nhật giá</p>
                ) : (
                  <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'flex', flexDirection: 'column', gap: '12px' }}>
                    {groupedRules.map((gr, idx) => (
                      <li key={idx} style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid #e5e7eb', paddingBottom: '8px' }}>
                        <div>
                          <div style={{ fontSize: '0.95rem', fontWeight: 600, color: '#374151' }}>
                            {gr.dayText}
                          </div>
                          <div style={{ fontSize: '0.85rem', color: '#6b7280', marginTop: '2px' }}>
                            {gr.start && gr.end ? `${gr.start} - ${gr.end}` : 'Cả ngày'}
                          </div>
                        </div>
                        <div style={{ fontWeight: 800, color: '#3b82f6', fontSize: '1.1rem', display: 'flex', alignItems: 'center' }}>
                          {(gr.rate).toLocaleString()}đ<span style={{ fontSize: '0.8rem', color: '#6b7280', fontWeight: 'normal', marginLeft: '2px' }}>/h</span>
                        </div>
                      </li>
                    ))}
                  </ul>
                )}
                
                {tableType.description && (
                  <p style={{ fontSize: '0.85rem', color: '#6b7280', marginTop: 'auto', paddingTop: '12px' }}>{tableType.description}</p>
                )}
              </article>
            );
          })}
        </div>
      )}
      <p className="data-note">Bảng giá trên được cập nhật trực tiếp từ hệ thống. Quý khách vui lòng đặt bàn trước để đảm bảo có vị trí tốt nhất.</p>
    </section>
  );
}
