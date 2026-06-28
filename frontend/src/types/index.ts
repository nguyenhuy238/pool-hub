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
  totalItems?: number;
  pageNumber?: number;
  pageSize?: number;
  totalPages?: number;
};

export type AuthUser = {
  userId: number;
  publicId?: string;
  email: string;
  fullName: string;
  roles: RoleName[];
  permissions?: string[];
};

export type AuthResponse = AuthUser & {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  user?: AuthUser;
};

export type LoginRequest = { email: string; password: string };
export type RegisterRequest = {
  fullName: string;
  email: string;
  phoneNumber?: string;
  password: string;
  confirmPassword: string;
};
export type ForgotPasswordRequest = { email: string };
export type ResetPasswordRequest = {
  email: string;
  token: string;
  newPassword: string;
  confirmPassword: string;
};
export type ChangePasswordRequest = {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
};

export type User = AuthUser & {
  publicId?: string;
  phoneNumber?: string;
  avatarUrl?: string;
  emailConfirmed?: boolean;
  status?: "Active" | "Locked" | "Deleted" | string;
  lastLoginAtUtc?: string;
  createdAtUtc?: string;
  updatedAtUtc?: string;
};

export type Customer = {
  customerId: number;
  publicId?: string;
  fullName: string;
  phoneNumber: string;
  email?: string;
  note?: string;
  status: boolean;
};

export type Role = {
  roleId?: number;
  name?: string;
  description?: string;
  isSystem?: boolean;
  isActive?: boolean;
  userCount?: number;
  createdAtUtc?: string;
  updatedAtUtc?: string;
  permissionCodes?: string[];
};

export type Booking = {
  bookingId: number;
  bookingCode?: string;
  customerId?: number;
  customerName?: string;
  phoneNumber?: string;
  email?: string;
  tableId?: number;
  tableTypeId?: number;
  startTimeUtc: string;
  endTimeUtc: string;
  numberOfGuests?: number;
  note?: string;
  status: number;
};

export type BookingCalendarItem = {
  bookingId: number;
  bookingCode: string;
  customerId?: number;
  customerName?: string;
  customerPhone?: string;
  tableId?: number;
  tableCode?: string;
  tableName?: string;
  tableTypeId?: number;
  tableTypeName?: string;
  startTimeUtc: string;
  endTimeUtc: string;
  numberOfGuests?: number;
  status: number;
  note?: string;
  confirmedAtUtc?: string;
  cancelledAtUtc?: string;
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
  durationMinutes?: number;
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
  orderId: number;
  productId: number;
  productNameSnapshot?: string;
  unitPriceSnapshot?: number;
  quantity: number;
  lineTotalAmount?: number;
  note?: string;
};

export type InvoiceLine = {
  invoiceLineId: number;
  invoiceId: number;
  lineType: string;
  referenceId?: number;
  description: string;
  quantity: number;
  unitPrice: number;
  lineTotalAmount: number;
};

export type InvoiceDiscount = {
  invoiceDiscountId: number;
  invoiceId: number;
  discountId: number;
  appliedByUserId?: number;
  amountApplied: number;
  descriptionSnapshot?: string;
};

export type InvoicePayment = {
  paymentId: number;
  invoiceId: number;
  paymentMethodId: number;
  amount: number;
  paymentStatus: number;
  transactionCode?: string;
  paidAtUtc?: string;
  note?: string;
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
  lines?: InvoiceLine[];
  discounts?: InvoiceDiscount[];
  payments?: InvoicePayment[];
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
  activeTables?: number;
  pendingBookings?: number;
  confirmedBookings?: number;
  unpaidInvoices?: number;
  todayAuditLogs?: number;
  ordersToday?: number;
  totalCustomers?: number;
  invoicesToday?: number;
};

export type RevenuePoint = { date: string; amount: number };
export type ActiveSessionDashboard = { sessionId: number; sessionCode?: string; startedAtUtc: string; durationMinutes: number };
export type LowStockProduct = { productId: number; name: string; sku: string; stockQuantity: number; lowStockThreshold?: number };
export type RecentAuditLog = { auditLogId: number; actorUserId?: number; action: string; entityName: string; entityId?: number; createdAtUtc: string };

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

export type VenueTableLayoutItem = {
  tableId: number;
  tableCode: string;
  tableName: string;
  tableTypeId: number;
  tableTypeName: string;
  capacity: number;
  operationalStatus: number;
  isActive: boolean;
  activeSessionId?: number;
  nextBookingId?: number;
  nextBookingCode?: string;
  nextBookingStartTimeUtc?: string;
};

export type VenueZoneLayoutItem = {
  zoneId: number;
  zoneName: string;
  description?: string;
  displayOrder: number;
  tables: VenueTableLayoutItem[];
};

export type VenueFloorLayoutItem = {
  floorId: number;
  floorName: string;
  description?: string;
  displayOrder: number;
  zones: VenueZoneLayoutItem[];
};

export type VenueLayoutResponse = {
  floors: VenueFloorLayoutItem[];
  totalTables: number;
  availableTables: number;
  occupiedTables: number;
  reservedTables?: number;
  maintenanceTables?: number;
  inactiveTables?: number;
  fetchedAtUtc: string;
};

export type PricingPlan = { pricingPlanId: number; name: string; isDefault?: boolean; isActive?: boolean };
export type PricingPlanRule = { pricingPlanRuleId: number; pricingPlanId: number; tableTypeId: number; dayOfWeek: number; hourlyRate: number; startTime?: string; endTime?: string; minimumMinutes?: number; billingBlockMinutes?: number; isActive?: boolean };
export type Notification = { notificationId: number; title?: string; message?: string; isRead?: boolean; createdAtUtc?: string };
export type Discount = {
  discountId: number; discountCode: string; name: string; discountType: string; value: number;
  maxAmount?: number; minTimeSubtotal?: number; appliesTo: "TIME"; startsAtUtc: string; endsAtUtc?: string; isActive: boolean;
};
export type InventoryTransaction = {
  inventoryTransactionId: number; productId: number; productName: string; transactionType: number;
  quantity: number; unitCost?: number; note?: string; createdAtUtc: string;
};
export type Payment = {
  paymentId: number; invoiceId: number; paymentMethodId: number; amount: number;
  paymentStatus: number; transactionCode?: string; paidAtUtc?: string;
};
export type RevenueReport = { date: string; revenue: number; invoiceCount: number };
export type TableUsageReport = { tableId: number; tableName: string; sessionCount: number; totalMinutes: number };
export type ProductSalesReport = { productId: number; productName: string; quantity: number; revenue: number };
export type BookingReport = { status: number; count: number };
export type CustomerReport = { customerId: number; customerName: string; bookingCount: number; sessionCount: number; revenue: number };
export type PaymentMethodReport = { paymentMethodId: number; paymentMethodName: string; paymentCount: number; amount: number };
export type InventoryReport = { productId: number; productName: string; currentStock: number; netMovement: number; inventoryValue: number };
export type AuditLog = {
  auditLogId: number;
  actorUserId?: number;
  actorName?: string;
  action: string;
  entityName: string;
  entityId?: number;
  entityPublicId?: string;
  oldValues?: string;
  newValues?: string;
  ipAddress?: string;
  userAgent?: string;
  description?: string;
  createdAtUtc: string;
};

export type CustomerDto = {
  customerId: number;
  fullName: string;
  phoneNumber?: string;
  email?: string;
  note?: string;
  status: boolean;
  createdAtUtc: string;
  totalBookings: number;
};

export type CustomerBookingHistory = {
  bookingId: number;
  bookingCode: string;
  tableId?: number;
  tableName?: string;
  startTimeUtc: string;
  endTimeUtc: string;
  status: number;
};

export type CustomerSessionHistory = {
  sessionId: number;
  sessionCode: string;
  startedAtUtc: string;
  endedAtUtc?: string;
  status: number;
};

export type CustomerInvoiceHistory = {
  invoiceId: number;
  invoiceCode: string;
  grandTotalAmount: number;
  paidAmount: number;
  paymentStatus: number;
  status: number;
  issuedAtUtc?: string;
};

export type SelectOption = { value: string; label: string };
