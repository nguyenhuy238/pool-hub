"use client";

import { useRef, useState } from "react";
import { mediaApi, type MediaAsset } from "@/lib/api/mediaApi";
import { useToast } from "@/components/toast";

const imageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".ico"];
const videoExtensions = [".mp4", ".webm"];

export function FileUploadButton({
  mediaType,
  folder,
  altText,
  onUploaded
}: {
  mediaType: "image" | "video" | "all";
  folder: string;
  altText?: string;
  onUploaded: (asset: MediaAsset) => void;
}) {
  const toast = useToast();
  const inputRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);

  const accept = mediaType === "video"
    ? ".mp4,.webm,video/mp4,video/webm"
    : mediaType === "image"
      ? ".jpg,.jpeg,.png,.webp,.gif,.ico,image/jpeg,image/png,image/webp,image/gif,image/x-icon"
      : ".jpg,.jpeg,.png,.webp,.gif,.ico,.mp4,.webm";

  async function upload(file?: File) {
    if (!file) return;
    const extension = `.${file.name.split(".").pop()?.toLowerCase() || ""}`;
    const isVideo = videoExtensions.includes(extension);
    const validExtension = imageExtensions.includes(extension) || isVideo;
    if (!validExtension || (mediaType === "image" && isVideo) || (mediaType === "video" && !isVideo)) {
      toast("Định dạng file không hợp lệ.", "error");
      return;
    }
    const maxSize = isVideo ? 30 * 1024 * 1024 : 5 * 1024 * 1024;
    if (file.size > maxSize) {
      toast(isVideo ? "Video tối đa 30MB." : "Ảnh tối đa 5MB.", "error");
      return;
    }

    setUploading(true);
    try {
      const asset = await mediaApi.upload(file, folder, altText);
      onUploaded(asset);
      toast("Upload media thành công.", "success");
    } catch (error) {
      toast(error instanceof Error ? error.message : "Upload media thất bại.", "error");
    } finally {
      setUploading(false);
      if (inputRef.current) inputRef.current.value = "";
    }
  }

  return (
    <>
      <input ref={inputRef} className="sr-only" type="file" accept={accept} onChange={(event) => upload(event.target.files?.[0])} />
      <button type="button" className="ghost-btn" disabled={uploading} onClick={() => inputRef.current?.click()}>
        {uploading ? "Đang upload..." : "Upload từ máy"}
      </button>
    </>
  );
}
