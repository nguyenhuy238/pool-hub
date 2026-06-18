"use client";

import { useMemo, useState } from "react";
import { FileUploadButton } from "@/components/admin/settings/FileUploadButton";
import { MediaLibraryModal } from "@/components/admin/settings/MediaLibraryModal";
import { MediaPreview } from "@/components/admin/settings/MediaPreview";

const imageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".ico"];
const videoExtensions = [".mp4", ".webm"];

export function validateMediaValue(value: string, mediaType: "image" | "video" | "all", required = false) {
  if (!value.trim()) return required ? "Trường media này không được để trống." : "";
  if (!value.startsWith("/") && !/^https?:\/\//i.test(value)) return "URL phải là đường dẫn local hoặc bắt đầu bằng http:// / https://.";
  const path = value.split(/[?#]/)[0].toLowerCase();
  const validImage = imageExtensions.some((extension) => path.endsWith(extension));
  const validVideo = videoExtensions.some((extension) => path.endsWith(extension));
  if (mediaType === "image" && !validImage) return "Ảnh phải có định dạng jpg, jpeg, png, webp, gif hoặc ico.";
  if (mediaType === "video" && !validVideo) return "Video phải có định dạng mp4 hoặc webm.";
  if (mediaType === "all" && !validImage && !validVideo) return "Định dạng media không được hỗ trợ.";
  return "";
}

export function MediaPicker({
  label,
  value,
  onChange,
  mediaType = "image",
  folder = "landing",
  required = false,
  helperText,
  altText
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  mediaType?: "image" | "video" | "all";
  folder?: string;
  required?: boolean;
  helperText?: string;
  altText?: string;
}) {
  const [libraryOpen, setLibraryOpen] = useState(false);
  const [previewOpen, setPreviewOpen] = useState(true);
  const error = useMemo(() => validateMediaValue(value, mediaType, required), [mediaType, required, value]);
  const previewType = mediaType === "all" ? (videoExtensions.some((extension) => value.split(/[?#]/)[0].toLowerCase().endsWith(extension)) ? "video" : "image") : mediaType;
  const external = /^https?:\/\//i.test(value);

  return (
    <div className="media-picker full-field">
      <label>
        <span>{label}</span>
        <div className="media-input-row">
          <input value={value} onChange={(event) => onChange(event.target.value)} placeholder="/uploads/landing/example.jpg" />
          <FileUploadButton mediaType={mediaType} folder={folder} altText={altText} onUploaded={(asset) => onChange(asset.url)} />
          <button type="button" className="ghost-btn" onClick={() => setLibraryOpen(true)}>Chọn media</button>
          <button type="button" className="ghost-btn" onClick={() => setPreviewOpen((open) => !open)}>Preview</button>
          <button type="button" className="danger-btn" onClick={() => onChange("")}>Clear</button>
        </div>
      </label>
      {helperText ? <p className="field-help">{helperText}</p> : null}
      {error ? <p className="field-error">{error}</p> : null}
      {external ? <p className="field-warning">Nên dùng media đã upload lên hệ thống để tránh lỗi tải file ngoài.</p> : null}
      {previewOpen && !error ? <MediaPreview url={value} mediaType={previewType} label={label} /> : null}
      <MediaLibraryModal open={libraryOpen} mediaType={mediaType} initialFolder={folder} onClose={() => setLibraryOpen(false)} onSelect={(asset) => onChange(asset.url)} />
    </div>
  );
}
