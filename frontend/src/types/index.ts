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
  averageRating?: number;
  ratingDistribution?: Record<number, number>;
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
  loyaltyPoints?: number;
  totalPointsEarned?: number;
};

export type PublicReview = {
  publicId: string;
  rating: number;
  content: string;
  displayName: string;
  isVerified?: boolean;
  avatarUrl?: string;
  checkInImageUrl?: string;
  isFeatured?: boolean;
  displayOrder?: number;
  createdAtUtc?: string;
};

export type CustomerReview = PublicReview & {
  customerId?: number;
  customerPublicId?: string;
  customerName?: string;
  phoneNumber?: string;
  isAnonymous?: boolean;
  bookingCode?: string;
  sessionCode?: string;
  invoiceCode?: string;
  status: number;
  source?: string;
  approvedAtUtc?: string;
  rejectedReason?: string;
  note?: string;
};

export type ReviewInvitation = {
  publicId: string;
  customerDisplayName: string;
  invoiceCode: string;
  sessionCode: string;
  playedAt?: string;
  tableName?: string;
  canSubmit: boolean;
  reason?: string;
};

export type ReviewInvitationLink = {
  publicId: string;
  reviewUrl: string;
  expiresAtUtc: string;
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
  tableId?: number | null;
  tableIds?: number[];
  tables?: BookingTableInfo[];
  tableCode?: string;
  tableName?: string;
  tableTypeName?: string;
  tableTypeId?: number;
  startTimeUtc: string;
  endTimeUtc: string;
  numberOfGuests?: number;
  note?: string;
  hasSession?: boolean;
  status: number;
  estimatedAmount?: number;
  requiresApproval?: boolean;
  approvedAtUtc?: string;
  holdExpiresAtUtc?: string;
  cancellationReason?: string;
  noShowAtUtc?: string;
  source?: string;
  deposit?: BookingDeposit;
  depositPaymentInstruction?: DepositPaymentInstruction;
  statusText?: string;
  depositStatusText?: string;
};

export type BookingTableInfo = {
  tableId: number;
  tableCode?: string;
  tableName?: string;
  tableTypeId?: number;
  tableTypeName?: string;
  zoneId?: number;
  zoneName?: string;
  floorId?: number;
  floorName?: string;
  capacity?: number;
};

export type BookingDeposit = {
  bookingDepositId: number;
  requiredAmount: number;
  paidAmount: number;
  appliedAmount: number;
  refundedAmount: number;
  forfeitedAmount: number;
  status: number;
  dueAtUtc: string;
  paidAtUtc?: string;
};

export type DepositPaymentInstruction = {
  paymentMethodCode?: string;
  paymentMethodName?: string;
  bankName: string;
  bankCode?: string;
  bankAccountNumber: string;
  bankAccountName: string;
  qrImageUrl?: string;
  vietQrUrl?: string;
  amount: number;
  transferContent: string;
  expiresAtUtc?: string;
};

export type BookingCalendarItem = {
  bookingId: number;
  bookingCode: string;
  customerId?: number;
  customerName?: string;
  customerPhone?: string;
  tableId?: number;
  tableIds?: number[];
  tables?: BookingTableInfo[];
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
  hasSession?: boolean;
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
  customerName?: string;
  currentTable?: {
    assignmentId?: number;
    tableId: number;
    tableCode?: string;
    tableName?: string;
    zoneId?: number;
    floorId?: number;
    startedAtUtc?: string;
    hourlyRateSnapshot?: number;
    estimatedAmount?: number;
  };
  activeAssignments?: SessionActiveTable[];
  releasedAssignments?: ReleasedSessionTable[];
  activeTableCount?: number;
  releasedTableCount?: number;
  estimatedTimeSubtotal?: number;
  orderSubtotal?: number;
  estimatedGrandTotal?: number;
  durationMinutes?: number;
  note?: string;
  assignments?: SessionTableAssignment[];
};

export type SessionActiveTable = {
  assignmentId: number;
  tableId: number;
  tableCode?: string;
  tableName?: string;
  tableTypeName?: string;
  zoneId?: number;
  floorId?: number;
  startedAtUtc?: string;
  hourlyRateSnapshot?: number;
  estimatedAmount?: number;
};

export type SessionTableAssignment = {
  sessionTableAssignmentId?: number;
  assignmentId?: number;
  tableId: number;
  tableName?: string;
  tableCode?: string;
  startedAtUtc?: string;
  endedAtUtc?: string;
  durationMinutes?: number;
  actualDurationMinutes?: number;
  billableDurationMinutes?: number;
  hourlyRate?: number;
  hourlyRateSnapshot?: number;
  minimumMinutes?: number;
  billingBlockMinutes?: number;
  pricingPlanName?: string;
  amount?: number;
};

export type ReleasedSessionTable = {
  assignmentId: number;
  tableId: number;
  tableCode?: string;
  tableName?: string;
  startedAtUtc: string;
  endedAtUtc: string;
  durationMinutes: number;
  hourlyRateSnapshot: number;
  amount: number;
};

export type ReleaseSessionTablesResponse = {
  sessionId: number;
  sessionCode?: string;
  sessionStatus: number;
  releasedAssignments: ReleasedSessionTable[];
  remainingActiveAssignments: Array<Session["currentTable"]>;
  remainingActiveTableCount: number;
  wasSessionAutoClosed: boolean;
  sessionEndedAtUtc?: string;
  timeSubtotalAmount: number;
  invoiceId?: number;
  invoiceCode?: string;
  message?: string;
};

export type Product = {
  productId: number;
  productCategoryId: number;
  name: string;
  sku: string;
  unitPrice: number;
  stockQuantity: number;
  isStockTracked?: boolean;
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
  productId?: number;
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
  invoiceCode?: string;
  paymentMethodId: number;
  paymentMethodName?: string;
  amount: number;
  paymentStatus: number;
  transactionCode?: string;
  paidAtUtc?: string;
  note?: string;
  refundReason?: string;
};

export type Invoice = {
  invoiceId: number;
  invoiceCode?: string;
  sessionId: number;
  customerId?: number;
  customerName?: string;
  customerPhone?: string;
  timeSubtotalAmount?: number;
  productSubtotalAmount?: number;
  subtotalAmount?: number;
  discountAmount?: number;
  taxAmount?: number;
  grandTotalAmount?: number;
  paidAmount?: number;
  depositAppliedAmount?: number;
  depositRefundAmount?: number;
  remainingAmount?: number;
  paymentStatus?: number;
  status?: number;
  note?: string;
  lines?: InvoiceLine[];
  discounts?: InvoiceDiscount[];
  payments?: InvoicePayment[];
  reviewInvitation?: ReviewInvitationLink;
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
  successfulPaymentsToday?: number;
  pendingPayments?: number;
  longRunningSessions?: number;
  upcomingBookings?: number;
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
  activeSessionStartedAtUtc?: string;
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
export type PricingPlanRule = { pricingPlanRuleId: number; pricingPlanId: number; tableTypeId: number; dayType?: number; dayOfWeek?: number; hourlyRate: number; startTime?: string; endTime?: string; minimumMinutes?: number; billingBlockMinutes?: number; isActive?: boolean };
export type PricingSpecialDate = { pricingSpecialDateId: number; date: string; dayType: number; description: string };
export type Notification = { notificationId: number; title?: string; message?: string; isRead?: boolean; createdAtUtc?: string };
export type Discount = {
  discountId: number; discountCode: string; name: string; discountType: string; value: number;
  maxAmount?: number; minTimeSubtotal?: number; appliesTo: "TIME"; startsAtUtc: string; endsAtUtc?: string; isActive: boolean;
  isVoucher?: boolean; pointsRequired?: number; customerId?: number; maxUsage?: number; usageCount?: number;
};
export type InventoryTransaction = {
  inventoryTransactionId: number; productId: number; productName: string; transactionType: number;
  quantity: number; unitCost?: number; referenceType?: string; referenceId?: number;
  orderId?: number; invoiceId?: number; invoiceCode?: string;
  note?: string; createdAtUtc: string;
};
export type Payment = {
  paymentId: number; invoiceId: number; invoiceCode?: string; paymentMethodId: number; paymentMethodName?: string; amount: number;
  paymentStatus: number; transactionCode?: string; paidAtUtc?: string; note?: string; refundReason?: string;
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
  loyaltyPoints?: number;
  totalPointsEarned?: number;
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

export type CustomerPointHistory = {
  customerPointHistoryId: number;
  customerId: number;
  points: number;
  transactionType: string;
  description: string;
  referenceId?: number;
  createdAtUtc: string;
};

export type SelectOption = { value: string; label: string };

export type DepositRefundStatus = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9;
export type DepositRefundMethod = 1 | 2;
export type DepositRefundReason =
  | "CustomerCancelledInTime"
  | "CustomerCancelledLate"
  | "VenueFault"
  | "BookingRejected"
  | "DuplicateDeposit"
  | "DepositExcess"
  | "ManualAdjustment"
  | "Other";

export type PublicDepositRefund = {
  refundCode: string;
  bookingCode?: string;
  amount: number;
  reason: DepositRefundReason | string;
  status: DepositRefundStatus;
  tokenExpiresAtUtc?: string;
  customerEmailMasked?: string;
  customerPhoneMasked?: string;
  refundMethod?: DepositRefundMethod | null;
  bankCode?: string;
  bankName?: string;
  bankAccountLast4?: string;
  isVerified: boolean;
  nextStep?: string;
};

export type VerifyRefundRequest = {
  verificationCode: string;
  phoneLast4: string;
};

export type SubmitRefundMethodRequest = {
  refundMethod: "BankTransfer" | "CashAtVenue";
  bankCode?: string;
  bankName?: string;
  accountNumber?: string;
  confirmAccountNumber?: string;
  accountHolderName?: string;
};

export type DepositRefundListItem = {
  bookingDepositRefundId: number;
  publicId?: string;
  bookingDepositId: number;
  bookingId: number;
  invoiceId?: number | null;
  customerId?: number | null;
  refundCode: string;
  amount: number;
  status: DepositRefundStatus;
  reason: DepositRefundReason | string;
  refundMethod?: DepositRefundMethod | null;
  reasonDetail?: string | null;
  idempotencyKey?: string | null;
  createdAtUtc: string;
  bookingCode?: string;
  customerName?: string;
  customerEmailMasked?: string;
  customerPhoneMasked?: string;
  bankCode?: string;
  bankName?: string;
  bankAccountLast4?: string;
  manualTransferCode?: string;
  failureReason?: string;
  rejectReason?: string;
  note?: string;
  approvedAtUtc?: string;
  processingAtUtc?: string;
  succeededAtUtc?: string;
};

export type DepositRefundDetail = DepositRefundListItem;

export type DepositRefundBankInfo = {
  bookingDepositRefundId: number;
  refundCode: string;
  bankCode?: string;
  bankName?: string;
  accountNumber?: string;
  accountHolderName?: string;
  accountLast4?: string;
};

export type RejectRefundRequest = { reason: string };
export type RequestCustomerRefundUpdateRequest = { reason?: string };
export type CompleteBankTransferRequest = { manualTransferCode: string; proofMediaAssetId?: number; note?: string };
export type MarkRefundFailedRequest = { reason: string };
export type CompleteCashPickupRequest = { cashPickupCode: string; bookingCode: string; phoneLast4: string; note?: string };
