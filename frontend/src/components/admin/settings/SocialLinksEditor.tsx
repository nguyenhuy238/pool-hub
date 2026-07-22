"use client";

import type { SocialLinkSettings } from "@/lib/api/landingSettingsApi";

const platforms = ["Facebook", "TikTok", "Instagram", "Zalo", "YouTube", "Messenger", "Website"];

export function SocialLinksEditor({ items, onChange }: { items: SocialLinkSettings[]; onChange: (items: SocialLinkSettings[]) => void }) {
  function update(index: number, patch: Partial<SocialLinkSettings>) {
    onChange(items.map((item, itemIndex) => itemIndex === index ? { ...item, ...patch } : item));
  }

  return (
    <div className="settings-repeater full-field">
      <div className="repeater-head"><h3>Social links</h3><button type="button" className="primary-btn" onClick={() => onChange([...items, { platform: "Facebook", url: "", icon: "f", isActive: true, displayOrder: items.length + 1 }])}>Thêm link</button></div>
      {items.map((item, index) => {
        const invalid = item.url && !/^https?:\/\//i.test(item.url);
        return (
          <div className="repeater-item" key={index}>
            <div className="settings-form-grid">
              <label><span>Platform</span><select value={item.platform} onChange={(event) => update(index, { platform: event.target.value })}>{platforms.map((platform) => <option key={platform}>{platform}</option>)}</select></label>
              <label><span>URL</span><input value={item.url} onChange={(event) => update(index, { url: event.target.value })} /></label>
              <label><span>Icon</span><input value={item.icon} onChange={(event) => update(index, { icon: event.target.value })} /></label>
              <label><span>Display order</span><input type="number" value={item.displayOrder} onChange={(event) => update(index, { displayOrder: Number(event.target.value) })} /></label>
              <label className="check-row"><input type="checkbox" checked={item.isActive} onChange={(event) => update(index, { isActive: event.target.checked })} /><span>Active</span></label>
              <button type="button" className="danger-btn" onClick={() => onChange(items.filter((_, itemIndex) => itemIndex !== index))}>Xóa</button>
            </div>
            {invalid ? <p className="field-error">URL phải bắt đầu bằng http:// hoặc https://.</p> : null}
          </div>
        );
      })}
    </div>
  );
}
