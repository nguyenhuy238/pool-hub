"use client";
/* eslint-disable @next/next/no-img-element */

import { useState } from "react";

export function MediaPreview({ url, mediaType, label }: { url: string; mediaType: "image" | "video"; label: string }) {
  const [failed, setFailed] = useState(false);

  if (!url) return <div className="media-fallback">Chưa chọn media.</div>;
  if (failed) return <div className="media-fallback">Không tải được media. Vui lòng chọn file khác.</div>;

  return (
    <div className="media-preview">
      {mediaType === "video"
        ? <video src={url} muted loop playsInline controls onError={() => setFailed(true)} />
        : <img src={url} alt={`Preview ${label}`} onError={() => setFailed(true)} />}
    </div>
  );
}
