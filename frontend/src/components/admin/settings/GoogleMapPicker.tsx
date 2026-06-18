"use client";

import { useState } from "react";
import type { GeneralInfoSettings } from "@/lib/api/landingSettingsApi";

export function validateMap(info: GeneralInfoSettings) {
  const errors: string[] = [];
  if (info.googleMapsEmbedUrl && !info.googleMapsEmbedUrl.startsWith("https://www.google.com/maps/embed")) errors.push("Embed URL phải bắt đầu bằng https://www.google.com/maps/embed.");
  for (const link of [info.googleMapsShareUrl, info.googleMapsDirectionUrl]) {
    if (link && !/^https:\/\/(maps\.google\.com|www\.google\.com\/maps)/i.test(link)) errors.push("Google Maps link phải dùng maps.google.com hoặc www.google.com/maps.");
  }
  if (info.latitude !== undefined && (info.latitude < -90 || info.latitude > 90)) errors.push("Latitude phải từ -90 đến 90.");
  if (info.longitude !== undefined && (info.longitude < -180 || info.longitude > 180)) errors.push("Longitude phải từ -180 đến 180.");
  return errors;
}

export function GoogleMapPicker({ value, onChange }: { value: GeneralInfoSettings; onChange: (value: GeneralInfoSettings) => void }) {
  const [previewOpen, setPreviewOpen] = useState(false);
  const errors = validateMap(value);
  const set = (patch: Partial<GeneralInfoSettings>) => onChange({ ...value, ...patch });

  return (
    <div className="map-picker full-field">
      <h3>Google Maps</h3>
      <div className="settings-form-grid">
        <label className="full-field"><span>Địa chỉ hiển thị</span><input value={value.address} onChange={(event) => set({ address: event.target.value })} /></label>
        <label className="full-field"><span>Google Maps embed URL</span><input value={value.googleMapsEmbedUrl || ""} onChange={(event) => set({ googleMapsEmbedUrl: event.target.value })} /></label>
        <label className="full-field"><span>Google Maps share URL</span><input value={value.googleMapsShareUrl || ""} onChange={(event) => set({ googleMapsShareUrl: event.target.value })} /></label>
        <label className="full-field"><span>Direction button URL</span><input value={value.googleMapsDirectionUrl || ""} onChange={(event) => set({ googleMapsDirectionUrl: event.target.value })} /></label>
        <label><span>Latitude</span><input type="number" step="any" value={value.latitude ?? ""} onChange={(event) => set({ latitude: event.target.value === "" ? undefined : Number(event.target.value) })} /></label>
        <label><span>Longitude</span><input type="number" step="any" value={value.longitude ?? ""} onChange={(event) => set({ longitude: event.target.value === "" ? undefined : Number(event.target.value) })} /></label>
        <label><span>Place ID</span><input value={value.placeId || ""} onChange={(event) => set({ placeId: event.target.value })} /></label>
        <label><span>Map display mode</span><select value={value.mapDisplayMode} onChange={(event) => set({ mapDisplayMode: event.target.value as GeneralInfoSettings["mapDisplayMode"] })}><option value="embed">Embed iframe</option><option value="placeholder">Static map placeholder</option><option value="external">External link only</option></select></label>
      </div>
      {errors.map((error) => <p className="field-error" key={error}>{error}</p>)}
      <div className="actions">
        <button type="button" className="ghost-btn" onClick={() => setPreviewOpen((open) => !open)}>Preview Map</button>
        <a className="ghost-btn" href={value.googleMapsShareUrl || value.googleMapsDirectionUrl || "https://www.google.com/maps"} target="_blank" rel="noreferrer">Mở Google Maps</a>
        <button type="button" className="ghost-btn" disabled={!value.googleMapsShareUrl} onClick={() => set({ googleMapsDirectionUrl: value.googleMapsShareUrl })}>Dùng link chỉ đường này</button>
      </div>
      {previewOpen && value.googleMapsEmbedUrl && !errors.some((error) => error.includes("Embed")) ? (
        <div className="map-admin-preview">
          <iframe title="Google Maps preview" src={value.googleMapsEmbedUrl} loading="lazy" referrerPolicy="no-referrer-when-downgrade" />
        </div>
      ) : previewOpen ? <div className="state-card">Nhập Google Maps embed URL hợp lệ để preview bản đồ.</div> : null}
    </div>
  );
}
