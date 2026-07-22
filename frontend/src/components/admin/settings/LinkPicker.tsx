"use client";

import { useEffect } from "react";
import type { LinkType } from "@/lib/api/landingSettingsApi";

const sectionOptions = ["#hero", "#about", "#services", "#pricing", "#booking", "#reviews", "#gallery", "#contact"];
const routeOptions = ["/", "/booking", "/login"];

export function validateLink(type: LinkType, value: string) {
  if (!value) return "Link không được để trống.";
  if (type === "external" && !/^https?:\/\//i.test(value)) return "External URL phải bắt đầu bằng http:// hoặc https://.";
  if (type === "section" && !sectionOptions.includes(value)) return "Section này không tồn tại trên trang chủ.";
  if (type === "internal" && !value.startsWith("/")) return "Route nội bộ phải bắt đầu bằng /.";
  if (type === "phone" && !value.startsWith("tel:")) return "Link gọi điện phải bắt đầu bằng tel:.";
  if (type === "email" && !value.startsWith("mailto:")) return "Email link phải bắt đầu bằng mailto:.";
  return "";
}

export function LinkPicker({
  label,
  type,
  value,
  hotline,
  email,
  mapUrl,
  onChange
}: {
  label: string;
  type: LinkType;
  value: string;
  hotline: string;
  email: string;
  mapUrl?: string;
  onChange: (type: LinkType, value: string) => void;
}) {
  const error = validateLink(type, value);
  const automaticValue = type === "phone"
    ? `tel:${hotline.replace(/\s/g, "")}`
    : type === "email"
      ? `mailto:${email}`
      : type === "map"
        ? mapUrl || "https://www.google.com/maps"
        : value;

  useEffect(() => {
    if (["phone", "email", "map"].includes(type) && value !== automaticValue) onChange(type, automaticValue);
  }, [automaticValue, onChange, type, value]);

  function changeType(nextType: LinkType) {
    const defaults: Record<LinkType, string> = {
      section: "#booking",
      internal: "/",
      external: "https://",
      phone: `tel:${hotline.replace(/\s/g, "")}`,
      map: mapUrl || "https://www.google.com/maps",
      email: `mailto:${email}`
    };
    onChange(nextType, defaults[nextType]);
  }

  return (
    <div className="link-picker full-field">
      <label>
        <span>{label}</span>
        <select value={type} onChange={(event) => changeType(event.target.value as LinkType)}>
          <option value="section">Cuộn tới section</option>
          <option value="internal">Route nội bộ</option>
          <option value="external">URL bên ngoài</option>
          <option value="phone">Gọi điện</option>
          <option value="map">Chỉ đường Google Maps</option>
          <option value="email">Email</option>
        </select>
      </label>
      {type === "section" ? <label><span>Section</span><select value={value} onChange={(event) => onChange(type, event.target.value)}>{sectionOptions.map((option) => <option key={option}>{option}</option>)}</select></label> : null}
      {type === "internal" ? <label><span>Route</span><select value={value} onChange={(event) => onChange(type, event.target.value)}>{routeOptions.map((option) => <option key={option}>{option}</option>)}</select></label> : null}
      {type === "external" ? <label><span>URL</span><input value={value} onChange={(event) => onChange(type, event.target.value)} /></label> : null}
      {type === "phone" ? <div className="picker-readonly">{`tel:${hotline.replace(/\s/g, "")}`}</div> : null}
      {type === "map" ? <div className="picker-readonly">{mapUrl || "Chưa cấu hình Google Maps direction URL"}</div> : null}
      {type === "email" ? <div className="picker-readonly">{`mailto:${email}`}</div> : null}
      {error ? <p className="field-error">{error}</p> : null}
    </div>
  );
}
