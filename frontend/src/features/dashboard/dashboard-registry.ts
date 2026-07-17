import type React from "react";
import { money } from "@/lib/status";
import { ROLES } from "@/lib/auth/constants";
import type { DashboardConfig, DashboardContext, DashboardMetric } from "./dashboard-types";
import { canViewAudit, canViewInventory, canViewPayments, canViewReports, hasRole } from "./dashboard-permissions";

function metric(key: string, label: string, value: React.ReactNode, hint?: string, tone?: DashboardMetric["tone"]): DashboardMetric {
  return { key, label, value: value ?? "-", hint, tone };
}

function commonOperations(summary: DashboardContext["summary"]) {
  return [
    metric("availableTables", "Bàn trống", summary?.availableTables ?? 0, "Theo thời gian thực", "success"),
    metric("inUseTables", "Bàn đang sử dụng", summary?.inUseTables ?? 0, "Theo thời gian thực", "info"),
    metric("maintenanceTables", "Bàn bảo trì", summary?.maintenanceTables ?? 0, "Theo thời gian thực", "warning"),
    metric("activeSessions", "Phiên chơi đang chạy", summary?.activeSessions ?? 0, "Theo thời gian thực", "info")
  ];
}

export function resolveDashboardConfig(context: DashboardContext): DashboardConfig {
  const { roles, permissions, summary } = context;
  const paymentAllowed = canViewPayments(roles, permissions);
  const inventoryAllowed = canViewInventory(roles, permissions);
  const reportsAllowed = canViewReports(roles, permissions);
  const auditAllowed = canViewAudit(roles, permissions);

  if (hasRole(roles, ROLES.ADMIN)) {
    return {
      title: "Tổng quan",
      description: "Tổng quan điều hành dành cho quản trị viên. Biểu đồ phân tích chuyên sâu nằm trong mục Phân tích.",
      metrics: [
        reportsAllowed ? metric("todayRevenue", "Doanh thu hôm nay", money(summary?.todayRevenue ?? 0), "Theo thanh toán hoàn tất", "success") : null,
        metric("inUseTables", "Bàn đang sử dụng", summary?.inUseTables ?? 0, "Theo thời gian thực", "info"),
        metric("pendingBookings", "Lượt đặt bàn chờ xử lý", summary?.pendingBookings ?? 0, undefined, "warning"),
        paymentAllowed ? metric("unpaidInvoices", "Hóa đơn chưa thanh toán", summary?.unpaidInvoices ?? 0, undefined, "danger") : null,
        inventoryAllowed ? metric("lowStockProducts", "Sản phẩm sắp hết", summary?.lowStockProducts ?? 0, undefined, "warning") : null,
        metric("unreadNotifications", "Thông báo chưa đọc", summary?.unreadNotifications ?? 0),
        auditAllowed ? metric("todayAuditLogs", "Sự kiện hệ thống hôm nay", summary?.todayAuditLogs ?? 0) : null
      ].filter(Boolean) as DashboardMetric[],
      actions: [
        { label: "Quản lý người dùng", href: "/admin/users" },
        { label: "Bảng giá", href: "/management/pricing-plans" },
        { label: "Phân tích", href: "/admin/analytics", primary: true },
        { label: "Nhật ký hệ thống", href: "/admin/audit-logs" },
        { label: "Cấu hình", href: "/admin/settings" }
      ]
    };
  }

  if (hasRole(roles, ROLES.MANAGER)) {
    return {
      title: "Tổng quan",
      description: "Tổng quan vận hành dành cho quản lý, tập trung vào bàn, đặt bàn, tồn kho và doanh thu hôm nay.",
      metrics: [
        ...commonOperations(summary),
        metric("longRunningSessions", "Phiên chơi kéo dài", summary?.longRunningSessions ?? 0, "Trên 3 giờ", "warning"),
        metric("todayBookings", "Lượt đặt bàn hôm nay", summary?.todayBookings ?? 0),
        metric("pendingBookings", "Lượt đặt bàn chờ xử lý", summary?.pendingBookings ?? 0, undefined, "warning"),
        paymentAllowed ? metric("unpaidInvoices", "Hóa đơn chưa thanh toán", summary?.unpaidInvoices ?? 0, undefined, "danger") : null,
        inventoryAllowed ? metric("lowStockProducts", "Tồn kho thấp", summary?.lowStockProducts ?? 0, undefined, "warning") : null,
        reportsAllowed ? metric("todayRevenue", "Doanh thu hôm nay", money(summary?.todayRevenue ?? 0), undefined, "success") : null
      ].filter(Boolean) as DashboardMetric[],
      actions: [
        { label: "Sơ đồ bàn", href: "/operation/floor-map", primary: true },
        { label: "Đặt bàn", href: "/operation/bookings" },
        { label: "Tồn kho", href: "/admin/inventory" },
        { label: "Phân tích", href: "/admin/analytics" }
      ]
    };
  }

  if (hasRole(roles, ROLES.STAFF)) {
    return {
      title: "Tổng quan",
      description: "Bảng thao tác vận hành dành cho nhân viên.",
      metrics: [
        ...commonOperations(summary),
        metric("upcomingBookings", "Lượt đặt bàn sắp đến", summary?.upcomingBookings ?? 0, "Trong 2 giờ tới", "warning"),
        metric("pendingBookings", "Lượt đặt bàn cần xử lý", summary?.pendingBookings ?? 0),
        metric("ordersToday", "Đơn hàng hôm nay", summary?.ordersToday ?? 0),
        metric("longRunningSessions", "Phiên chơi cần kiểm tra", summary?.longRunningSessions ?? 0, "Trên 3 giờ", "warning")
      ],
      actions: [
        { label: "Mở bàn", href: "/operation/floor-map", primary: true },
        { label: "Sơ đồ bàn", href: "/operation/floor-map" },
        { label: "Xử lý đặt bàn", href: "/operation/bookings" },
        { label: "Tạo đơn hàng", href: "/operation/orders" }
      ]
    };
  }

  if (hasRole(roles, ROLES.CASHIER)) {
    return {
      title: "Tổng quan",
      description: "Tổng quan thu ngân và xử lý hóa đơn.",
      metrics: [
        paymentAllowed ? metric("unpaidInvoices", "Hóa đơn chưa thanh toán", summary?.unpaidInvoices ?? 0, undefined, "danger") : null,
        paymentAllowed ? metric("invoicesToday", "Hóa đơn vừa tạo hôm nay", summary?.invoicesToday ?? 0) : null,
        paymentAllowed ? metric("successfulPaymentsToday", "Thanh toán thành công hôm nay", summary?.successfulPaymentsToday ?? 0, undefined, "success") : null,
        paymentAllowed ? metric("pendingPayments", "Giao dịch đang chờ", summary?.pendingPayments ?? 0, undefined, "warning") : null,
        reportsAllowed || paymentAllowed ? metric("todayRevenue", "Doanh thu hôm nay", money(summary?.todayRevenue ?? 0), undefined, "success") : null,
        metric("unreadNotifications", "Thông báo chưa đọc", summary?.unreadNotifications ?? 0)
      ].filter(Boolean) as DashboardMetric[],
      actions: [
        { label: "Tìm hóa đơn", href: "/operation/invoices", primary: true },
        { label: "Thanh toán", href: "/operation/invoices" },
        { label: "Lịch sử giao dịch", href: "/admin/payments/history" },
        { label: "Mã giảm giá", href: "/admin/discounts" }
      ]
    };
  }

  return {
    title: "Tổng quan",
    description: "Khu vực cá nhân dành cho khách hàng.",
    metrics: [
      metric("unreadNotifications", "Thông báo chưa đọc", summary?.unreadNotifications ?? 0)
    ],
    actions: [
      { label: "Đặt bàn", href: "/booking", primary: true },
      { label: "Hồ sơ cá nhân", href: "/profile" },
      { label: "Thông báo", href: "/notifications" }
    ]
  };
}
