import { apiFetch, toQuery } from "@/lib/api/client";

export type MediaAsset = {
  mediaAssetId: number;
  publicId: string;
  url: string;
  fileName: string;
  originalFileName: string;
  folder: string;
  contentType: string;
  mediaType: "Image" | "Video" | string;
  sizeBytes: number;
  altText?: string;
  createdAtUtc: string;
};

export type MediaPage = {
  items: MediaAsset[];
  pageNumber: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};

export const mediaApi = {
  upload(file: File, folder: string, altText?: string) {
    const form = new FormData();
    form.append("file", file);
    form.append("folder", folder);
    if (altText) form.append("altText", altText);
    return apiFetch<MediaAsset>("/api/admin/media/upload", { method: "POST", body: form });
  },
  list(params: { keyword?: string; mediaType?: string; folder?: string; pageNumber?: number; pageSize?: number } = {}) {
    return apiFetch<MediaPage>(`/api/admin/media-assets${toQuery(params)}`);
  },
  delete(id: number) {
    return apiFetch(`/api/admin/media-assets/${id}`, { method: "DELETE" });
  }
};
