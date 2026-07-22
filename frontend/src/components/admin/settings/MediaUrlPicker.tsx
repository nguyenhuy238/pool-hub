"use client";
/* eslint-disable @next/next/no-img-element */

import { useMemo, useState } from "react";
import { mediaLibrary } from "@/lib/mock/mediaLibrary";

const imageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".ico"];
const videoExtensions = [".mp4", ".webm"];
const fallbackImage = "/images/poolhub/hero.png";

export function validateMediaUrl(value: string, type: "image" | "video", required = false) {
  if (!value.trim()) return required ? "Trường media này không được để trống." : "";
  if (!value.startsWith("/") && !/^https?:\/\//i.test(value)) return "URL phải là đường dẫn local hoặc bắt đầu bằng http:// / https://.";
  const path = value.split(/[?#]/)[0].toLowerCase();
  const extensions = type === "image" ? imageExtensions : videoExtensions;
  return extensions.some((extension) => path.endsWith(extension)) ? "" : `Định dạng ${type === "image" ? "ảnh" : "video"} không được hỗ trợ.`;
}

export function MediaUrlPicker({
  label,
  value,
  type = "image",
  required = false,
  onChange
}: {
  label: string;
  value: string;
  type?: "image" | "video";
  required?: boolean;
  onChange: (value: string) => void;
}) {
  const [libraryOpen, setLibraryOpen] = useState(false);
  const [previewOpen, setPreviewOpen] = useState(true);
  const [mediaFailed, setMediaFailed] = useState(false);
  const error = useMemo(() => validateMediaUrl(value, type, required), [required, type, value]);
  const external = /^https?:\/\//i.test(value);
  const samples = mediaLibrary.filter((item) => item.type === type);

  return (
    <div className="media-picker full-field">
      <label>
        <span>{label}</span>
        <div className="media-input-row">
          <input value={value} onChange={(event) => { setMediaFailed(false); onChange(event.target.value); }} placeholder={type === "image" ? "/images/poolhub/hero.png" : "/videos/poolhub/hero.mp4"} />
          <button type="button" className="ghost-btn" onClick={() => setLibraryOpen((open) => !open)}>Chọn mẫu</button>
          <button type="button" className="ghost-btn" onClick={() => setPreviewOpen((open) => !open)}>Preview</button>
          <button type="button" className="danger-btn" onClick={() => onChange("")}>Clear</button>
        </div>
      </label>
      {error ? <p className="field-error">{error}</p> : null}
      {external ? <p className="field-warning">Nên dùng ảnh đã upload lên hệ thống để tránh lỗi tải ảnh.</p> : null}
      {libraryOpen ? (
        <div className="media-library">
          {samples.length ? samples.map((item) => (
            <button type="button" key={`${item.name}-${item.url}`} onClick={() => { onChange(item.url); setMediaFailed(false); setLibraryOpen(false); }}>
              {item.type === "image" ? <img src={item.url} alt={item.name} /> : <span>Video</span>}
              <strong>{item.name}</strong>
            </button>
          )) : <div className="state-card">Chưa có video mẫu local.</div>}
        </div>
      ) : null}
      {previewOpen && value && !error ? (
        <div className="media-preview">
          {type === "video" ? (
            mediaFailed ? <div className="media-fallback">Video không tải được.</div> : <video src={value} muted loop playsInline controls onError={() => setMediaFailed(true)} />
          ) : (
            <img src={mediaFailed ? fallbackImage : value} alt={`Preview ${label}`} onError={() => setMediaFailed(true)} />
          )}
        </div>
      ) : null}
    </div>
  );
}
