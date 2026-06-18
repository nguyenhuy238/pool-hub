"use client";
/* eslint-disable @next/next/no-img-element */

import { useCallback, useEffect, useState } from "react";
import { mediaApi, type MediaAsset } from "@/lib/api/mediaApi";
import { useToast } from "@/components/toast";

const folders = ["all", "landing", "logo", "hero", "gallery", "services", "reviews", "seo"];

export function MediaLibraryModal({
  open,
  mediaType,
  initialFolder,
  onClose,
  onSelect
}: {
  open: boolean;
  mediaType: "image" | "video" | "all";
  initialFolder?: string;
  onClose: () => void;
  onSelect: (asset: MediaAsset) => void;
}) {
  const toast = useToast();
  const [items, setItems] = useState<MediaAsset[]>([]);
  const [loading, setLoading] = useState(false);
  const [keyword, setKeyword] = useState("");
  const [typeFilter, setTypeFilter] = useState(mediaType);
  const [folder, setFolder] = useState(initialFolder || "all");

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const result = await mediaApi.list({
        keyword,
        mediaType: typeFilter === "all" ? undefined : typeFilter === "image" ? "Image" : "Video",
        folder: folder === "all" ? undefined : folder,
        pageNumber: 1,
        pageSize: 100
      });
      setItems(result.items || []);
    } catch (error) {
      toast(error instanceof Error ? error.message : "Không tải được thư viện media.", "error");
    } finally {
      setLoading(false);
    }
  }, [folder, keyword, toast, typeFilter]);

  useEffect(() => {
    if (open) load();
  }, [load, open]);

  if (!open) return null;

  async function remove(asset: MediaAsset) {
    if (!window.confirm(`Xóa media "${asset.originalFileName}"?`)) return;
    try {
      await mediaApi.delete(asset.mediaAssetId);
      toast("Đã xóa media.", "success");
      await load();
    } catch (error) {
      toast(error instanceof Error ? error.message : "Không thể xóa media.", "error");
    }
  }

  async function copyUrl(url: string) {
    await navigator.clipboard.writeText(url);
    toast("Đã copy URL.", "success");
  }

  return (
    <div className="media-modal-backdrop" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) onClose(); }}>
      <div className="media-modal" role="dialog" aria-modal="true" aria-label="Media Library">
        <div className="media-modal-head">
          <div><h2>Media Library</h2><p>Chọn media đã upload hoặc quản lý file hiện có.</p></div>
          <button type="button" className="ghost-btn" onClick={onClose}>Đóng</button>
        </div>
        <div className="media-library-controls">
          <input value={keyword} onChange={(event) => setKeyword(event.target.value)} placeholder="Tìm theo tên file" />
          <select value={typeFilter} onChange={(event) => setTypeFilter(event.target.value as typeof typeFilter)}>
            <option value="all">All</option><option value="image">Image</option><option value="video">Video</option>
          </select>
          <select value={folder} onChange={(event) => setFolder(event.target.value)}>
            {folders.map((item) => <option key={item} value={item}>{item === "all" ? "All folders" : item}</option>)}
          </select>
          <button type="button" className="primary-btn" onClick={load}>Tìm kiếm</button>
        </div>
        {loading ? <div className="state-card">Đang tải media...</div> : null}
        {!loading && !items.length ? <div className="state-card">Chưa có media phù hợp.</div> : null}
        <div className="media-manager-grid">
          {items.map((asset) => (
            <article className="media-manager-item" key={asset.mediaAssetId}>
              <button type="button" className="media-select" onClick={() => { onSelect(asset); onClose(); }}>
                {asset.mediaType.toLowerCase() === "video"
                  ? <video src={asset.url} muted preload="metadata" />
                  : <img src={asset.url} alt={asset.altText || asset.originalFileName} />}
              </button>
              <div className="media-manager-meta">
                <strong title={asset.originalFileName}>{asset.originalFileName}</strong>
                <span>{asset.mediaType} · {formatBytes(asset.sizeBytes)}</span>
                <span>{new Date(asset.createdAtUtc).toLocaleDateString("vi-VN")}</span>
              </div>
              <div className="media-manager-actions">
                <button type="button" className="ghost-btn" onClick={() => copyUrl(asset.url)}>Copy URL</button>
                <button type="button" className="danger-btn" onClick={() => remove(asset)}>Xóa</button>
              </div>
            </article>
          ))}
        </div>
      </div>
    </div>
  );
}

function formatBytes(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
