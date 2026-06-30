"use client";

import { useEffect, useState } from "react";
import { Badge, DataTable, Modal, PageHeader, SearchFilterBar, StateBlock } from "@/components/ui";
import { useToast } from "@/components/toast";
import { customerReviewsApi } from "@/lib/api/customerReviewsApi";
import type { CustomerReview, PagedResult } from "@/types";

const statuses = [
  { value: "", label: "Tất cả" },
  { value: "1", label: "Pending" },
  { value: "2", label: "Approved" },
  { value: "3", label: "Rejected" },
  { value: "4", label: "Hidden" }
];

function statusLabel(status: number) {
  if (status === 1) return "Pending";
  if (status === 2) return "Approved";
  if (status === 3) return "Rejected";
  if (status === 4) return "Hidden";
  return String(status);
}

function statusTone(status: number) {
  if (status === 2) return "green";
  if (status === 1) return "yellow";
  if (status === 3) return "red";
  return "neutral";
}

export default function CustomerReviewsPage() {
  const toast = useToast();
  const [data, setData] = useState<PagedResult<CustomerReview> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [params, setParams] = useState({ search: "", status: "", rating: "", pageNumber: 1, pageSize: 20 });
  const [rejecting, setRejecting] = useState<CustomerReview | null>(null);
  const [configuring, setConfiguring] = useState<CustomerReview | null>(null);
  const [displayConfig, setDisplayConfig] = useState({ showOnHome: true, isFeatured: true, displayOrder: 0 });
  const [reason, setReason] = useState("");

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setData(await customerReviewsApi.list(params));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được đánh giá.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [params]);

  const rows = data?.items || data?.data || [];

  async function run(action: () => Promise<unknown>, message: string) {
    try {
      await action();
      toast(message, "success");
      await load();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Thao tác thất bại.", "error");
    }
  }

  return (
    <>
      <PageHeader title="Quản lý đánh giá" description="Duyệt, ẩn và chọn review khách hàng thật hiển thị trên trang chủ." />
      <SearchFilterBar>
        <label><span>Tìm kiếm</span><input value={params.search} onChange={(event) => setParams({ ...params, search: event.target.value, pageNumber: 1 })} /></label>
        <label><span>Trạng thái</span><select value={params.status} onChange={(event) => setParams({ ...params, status: event.target.value, pageNumber: 1 })}>{statuses.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}</select></label>
        <label><span>Số sao</span><select value={params.rating} onChange={(event) => setParams({ ...params, rating: event.target.value, pageNumber: 1 })}><option value="">Tất cả</option><option value="5">5</option><option value="4">4</option><option value="3">3</option><option value="2">2</option><option value="1">1</option></select></label>
      </SearchFilterBar>
      <StateBlock loading={loading} error={error} empty={!loading && !rows.length} />
      <DataTable
        rows={rows as unknown as Record<string, unknown>[]}
        columns={[
          { key: "displayName", label: "Khách hàng", render: (row) => <><strong>{String(row.displayName || row.customerName || "-")}</strong><br /><span className="muted-text">{String(row.phoneNumber || "")}</span></> },
          { key: "rating", label: "Sao", render: (row) => "*".repeat(Number(row.rating || 0)) },
          { key: "content", label: "Nội dung" },
          { key: "status", label: "Trạng thái", render: (row) => <Badge tone={statusTone(Number(row.status)) as never}>{statusLabel(Number(row.status))}</Badge> },
          { key: "source", label: "Nguồn" },
          { key: "isFeatured", label: "Nổi bật", render: (row) => row.isFeatured ? "Có" : "Không" },
          { key: "displayOrder", label: "Thứ tự", render: (row) => String(row.displayOrder ?? 0) },
          { key: "createdAtUtc", label: "Ngày tạo", render: (row) => row.createdAtUtc ? new Date(String(row.createdAtUtc)).toLocaleString("vi-VN") : "-" }
        ]}
        actions={(row) => {
          const review = row as unknown as CustomerReview;
          return (
            <div className="actions">
              <button className="secondary-btn" onClick={() => run(() => customerReviewsApi.approve(review.publicId, { isFeatured: true, displayOrder: review.displayOrder || 0 }), "Đã duyệt review.")}>Duyệt</button>
              <button className="ghost-btn" onClick={() => {
                setConfiguring(review);
                setDisplayConfig({
                  showOnHome: Number(review.status) === 2,
                  isFeatured: Boolean(review.isFeatured),
                  displayOrder: Number(review.displayOrder || 0)
                });
              }}>Cấu hình hiển thị</button>
              <button className="ghost-btn" onClick={() => run(() => customerReviewsApi.hide(review.publicId), "Đã ẩn review.")}>Ẩn</button>
              <button className="danger-btn" onClick={() => { setRejecting(review); setReason(review.rejectedReason || ""); }}>Từ chối</button>
            </div>
          );
        }}
      />
      {rejecting ? (
        <Modal title="Từ chối đánh giá" onClose={() => setRejecting(null)} size="small">
          <label><span>Lý do</span><textarea rows={3} value={reason} onChange={(event) => setReason(event.target.value)} /></label>
          <div className="modal-actions">
            <button className="ghost-btn" onClick={() => setRejecting(null)}>Hủy</button>
            <button className="danger-btn" onClick={() => run(async () => { await customerReviewsApi.reject(rejecting.publicId, reason); setRejecting(null); }, "Đã từ chối review.")}>Từ chối</button>
          </div>
        </Modal>
      ) : null}
      {configuring ? (
        <Modal title="Cấu hình review trên trang chủ" onClose={() => setConfiguring(null)} size="small">
          <div className="form-stack">
            <label className="check-row"><input type="checkbox" checked={displayConfig.showOnHome} onChange={(event) => setDisplayConfig({ ...displayConfig, showOnHome: event.target.checked })} /><span>Hiển thị review này trên trang chủ</span></label>
            <label className="check-row"><input type="checkbox" checked={displayConfig.isFeatured} onChange={(event) => setDisplayConfig({ ...displayConfig, isFeatured: event.target.checked })} disabled={!displayConfig.showOnHome} /><span>Đánh dấu nổi bật</span></label>
            <label><span>Thứ tự hiển thị</span><input type="number" value={displayConfig.displayOrder} onChange={(event) => setDisplayConfig({ ...displayConfig, displayOrder: Number(event.target.value) })} /></label>
            <div className="modal-actions">
              <button className="ghost-btn" onClick={() => setConfiguring(null)}>Hủy</button>
              <button className="primary-btn" onClick={() => run(async () => {
                if (displayConfig.showOnHome) {
                  await customerReviewsApi.visibility(configuring.publicId, {
                    status: 2,
                    isFeatured: displayConfig.isFeatured,
                    displayOrder: displayConfig.displayOrder
                  });
                } else {
                  await customerReviewsApi.hide(configuring.publicId);
                }
                setConfiguring(null);
              }, "Đã cập nhật cấu hình hiển thị.")}>Lưu cấu hình</button>
            </div>
          </div>
        </Modal>
      ) : null}
    </>
  );
}
