export function DashboardSkeleton() {
  return (
    <div className="kpi-grid" aria-label="Đang tải bảng tổng quan">
      {Array.from({ length: 8 }).map((_, index) => (
        <div className="card metric loading-state" key={index}>
          <span className="skeleton-line" />
          <strong className="skeleton-line wide" />
        </div>
      ))}
    </div>
  );
}
