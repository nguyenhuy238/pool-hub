"use client";

import React, { useEffect, useState } from "react";
import { landingSettingsApi, type PricingRuleSummary, type TableTypePricingSummary } from "@/lib/api/landingSettingsApi";

function formatDays(days: number[]) {
  if (!days || days.length === 0) return "Mọi ngày";
  
  if (days.includes(1) && days.includes(2) && days.includes(3) && days.includes(4)) return "Mọi ngày";
  
  const parts = [];
  if (days.includes(1)) parts.push("Ngày thường");
  if (days.includes(2)) parts.push("Cuối tuần");
  if (days.includes(3)) parts.push("Ngày lễ");
  if (days.includes(4)) parts.push("Ngày đặc biệt");
  
  return parts.join(", ");
}

function groupRules(rules: PricingRuleSummary[]) {
  const groups = new Map<string, { days: number[], rate: number, start: string, end: string }>();
  
  for (const r of rules) {
    // If dayType is completely missing, we treat it as everyday (though usually it's set 1-4)
    const d = r.dayType !== undefined ? r.dayType : -1;
    const start = r.startTime ? r.startTime.slice(0, 5) : "";
    const end = r.endTime ? r.endTime.slice(0, 5) : "";
    const rate = r.hourlyRate || 0;
    
    const key = `${rate}-${start}-${end}`;
    if (!groups.has(key)) {
      groups.set(key, { days: d !== -1 ? [d] : [1,2,3,4], rate, start, end });
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
  const [tableTypes, setTableTypes] = useState<TableTypePricingSummary[]>([]);
  const [rules, setRules] = useState<PricingRuleSummary[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        const summary = await landingSettingsApi.pricingSummary();
        setTableTypes(summary.tableTypes);
        setRules(summary.rules);
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
