export type RoleName = "Admin" | "Owner" | "Manager" | "Staff" | "Cashier" | "Customer" | "Guest" | string;

export type ApiResponse<T> = {
  success?: boolean;
  message?: string;
  data?: T;
  errors?: string[];
  traceId?: string;
};

export type PagedResult<T> = {
  items?: T[];
  data?: T[];
  totalCount?: number;
  pageNumber?: number;
  pageSize?: number;
  totalPages?: number;
};

export type AuthUser = {
  userId: number;
  email: string;
  fullName: string;
  roles: RoleName[];
};

export type AuthResponse = AuthUser & {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
};

export type User = AuthUser & {
  publicId?: string;
  phoneNumber?: string;
  status?: boolean;
};

export type Role = { roleId?: number; name?: string; roleName?: string; normalizedName?: string };

export type Booking = {
  bookingId: number;
  bookingCode?: string;
  customerId?: number;
  customerName?: string;
  phoneNumber?: string;
  tableId?: number;
  tableTypeId?: number;
  startTimeUtc: string;
  endTimeUtc: string;
  numberOfGuests?: number;
  status: number;
};

export type Session = {
  sessionId: number;
  sessionCode?: string;
  customerId?: number;
  bookingId?: number;
  status: number;
  startedAtUtc: string;
  endedAtUtc?: string;
  tableId?: number;
  tableName?: string;
  note?: string;
  assignments?: SessionTableAssignment[];
};

export type SessionTableAssignment = {
  sessionTableAssignmentId?: number;
  tableId: number;
  tableName?: string;
  tableCode?: string;
  startedAtUtc?: string;
  endedAtUtc?: string;
  durationMinutes?: number;
  amount?: number;
};

export type Product = {
  productId: number;
  productCategoryId: number;
  name: string;
  sku: string;
  unitPrice: number;
  stockQuantity: number;
};

export type ProductCategory = { productCategoryId: number; name: string; description?: string; isActive?: boolean };

export type Order = {
  orderId: number;
  orderCode?: string;
  sessionId: number;
  status?: number;
  subtotalAmount?: number;
  items?: OrderItem[];
};

export type OrderItem = {
  orderItemId: number;
  productId: number;
  productName?: string;
  quantity: number;
  unitPrice?: number;
  lineTotal?: number;
};

export type Invoice = {
  invoiceId: number;
  invoiceCode?: string;
  sessionId: number;
  timeSubtotalAmount?: number;
  productSubtotalAmount?: number;
  subtotalAmount?: number;
  discountAmount?: number;
  taxAmount?: number;
  grandTotalAmount?: number;
  paidAmount?: number;
  paymentStatus?: number;
  status?: number;
  lines?: unknown[];
  discounts?: unknown[];
  payments?: unknown[];
};

export type PaymentMethod = {
  paymentMethodId: number;
  name: string;
  code: string;
  description?: string;
  isActive?: boolean;
};

export type DashboardSummary = {
  totalTables: number;
  availableTables: number;
  inUseTables: number;
  reservedTables: number;
  maintenanceTables: number;
  todayBookings: number;
  activeSessions: number;
  todayRevenue: number;
  lowStockProducts: number;
  unreadNotifications: number;
};

export type Floor = { floorId: number; name: string; description?: string; displayOrder?: number; isActive?: boolean };
export type Zone = { zoneId: number; floorId: number; name: string; description?: string; displayOrder?: number; isActive?: boolean };
export type TableType = { tableTypeId: number; name: string; code?: string; description?: string; defaultCapacity?: number; isActive?: boolean };
export type VenueTable = {
  tableId: number;
  zoneId: number;
  tableTypeId: number;
  tableCode: string;
  tableName: string;
  capacity: number;
  operationalStatus: number;
  isActive?: boolean;
};

export type PricingPlan = { pricingPlanId: number; name: string; isDefault?: boolean; isActive?: boolean };
export type PricingPlanRule = { pricingPlanRuleId: number; pricingPlanId: number; tableTypeId: number; dayOfWeek: number; hourlyRate: number; startTime?: string; endTime?: string; minimumMinutes?: number; billingBlockMinutes?: number; isActive?: boolean };
export type Notification = { notificationId: number; title?: string; message?: string; isRead?: boolean; createdAtUtc?: string };
export type AuditLog = { auditLogId: number; actor?: string; action?: string; entity?: string; oldValues?: string; newValues?: string; ipAddress?: string; createdAtUtc?: string };

export type SelectOption = { value: string; label: string };
