import { apiFetch, toQuery } from "@/lib/api/client";
import type {
  ActiveSessionDashboard,
  Booking,
  Customer,
  DashboardSummary,
  Floor,
  Invoice,
  LowStockProduct,
  Notification,
  Order,
  PaymentMethod,
  PricingPlan,
  PricingPlanRule,
  PricingSpecialDate,
  Product,
  ProductCategory,
  RecentAuditLog,
  RevenuePoint,
  ReleaseSessionTablesResponse,
  Session,
  TableType,
  VenueLayoutResponse,
  VenueTable,
  Zone,
  BookingCalendarItem,
  CustomerDto,
  CustomerBookingHistory,
  CustomerSessionHistory,
  CustomerInvoiceHistory,
  CustomerPointHistory,
  CustomerPortalProfile,
  PagedResult,
  Discount, InventoryTransaction, Payment, RevenueReport, TableUsageReport, ProductSalesReport, BookingReport, CustomerReport, PaymentMethodReport, InventoryReport
} from "@/types";

type UpdateCustomerPayload = Pick<CustomerDto, "fullName" | "status"> & {
  email?: string;
  note?: string;
};

export const venueApi = {
  layout: () => apiFetch<VenueLayoutResponse>("/api/venue-tables/layout"),
  floors: (params: Record<string, string | number | undefined> = {}) => apiFetch<Floor[] | { items?: Floor[] }>(`/api/floors${toQuery(params)}`),
  zones: (params: Record<string, string | number | undefined> = {}) => apiFetch<Zone[] | { items?: Zone[] }>(`/api/zones${toQuery(params)}`),
  tableTypes: (params: Record<string, string | number | undefined> = {}) => apiFetch<TableType[] | { items?: TableType[] }>(`/api/table-types${toQuery(params)}`),
  tables: (params: Record<string, string | number | undefined> = {}) => apiFetch<VenueTable[] | { items?: VenueTable[] }>(`/api/venue-tables${toQuery(params)}`),
  floorDetail: (id: number) => apiFetch<Floor>(`/api/floors/${id}`),
  zoneDetail: (id: number) => apiFetch<Zone>(`/api/zones/${id}`),
  tableTypeDetail: (id: number) => apiFetch<TableType>(`/api/table-types/${id}`),
  tableDetail: (id: number) => apiFetch<VenueTable>(`/api/venue-tables/${id}`),
  createFloor: (body: Partial<Floor>) => apiFetch<Floor>("/api/floors", { method: "POST", body: JSON.stringify(body) }),
  updateFloor: (id: number, body: Partial<Floor>) => apiFetch<Floor>(`/api/floors/${id}`, { method: "PUT", body: JSON.stringify(body) }),
  deleteFloor: (id: number) => apiFetch(`/api/floors/${id}`, { method: "DELETE" }),
  createZone: (body: Partial<Zone>) => apiFetch<Zone>("/api/zones", { method: "POST", body: JSON.stringify(body) }),
  updateZone: (id: number, body: Partial<Zone>) => apiFetch<Zone>(`/api/zones/${id}`, { method: "PUT", body: JSON.stringify(body) }),
  deleteZone: (id: number) => apiFetch(`/api/zones/${id}`, { method: "DELETE" }),
  createTableType: (body: Partial<TableType>) => apiFetch<TableType>("/api/table-types", { method: "POST", body: JSON.stringify(body) }),
  updateTableType: (id: number, body: Partial<TableType>) => apiFetch<TableType>(`/api/table-types/${id}`, { method: "PUT", body: JSON.stringify(body) }),
  deleteTableType: (id: number) => apiFetch(`/api/table-types/${id}`, { method: "DELETE" }),
  createTable: (body: Partial<VenueTable>) => apiFetch<VenueTable>("/api/venue-tables", { method: "POST", body: JSON.stringify(body) }),
  updateTable: (id: number, body: Partial<VenueTable>) => apiFetch<VenueTable>(`/api/venue-tables/${id}`, { method: "PUT", body: JSON.stringify(body) }),
  deleteTable: (id: number) => apiFetch(`/api/venue-tables/${id}`, { method: "DELETE" })
};

export const bookingApi = {
  list: (params: Record<string, string | number | undefined> = {}) => apiFetch<Booking[] | { items?: Booking[] }>(`/api/bookings${toQuery(params)}`),
  availability: (tableId: number, startTimeUtc: string, endTimeUtc: string) =>
    apiFetch<VenueTable[]>(`/api/bookings/availability${toQuery({ tableId, startTimeUtc, endTimeUtc })}`),
  availabilityMany: (tableIds: number[], startTimeUtc: string, endTimeUtc: string) =>
    apiFetch<VenueTable[]>(`/api/bookings/availability${toQuery({ tableIds, startTimeUtc, endTimeUtc })}`),
  calendar: (from: string, to: string, params: Record<string, string | number | undefined> = {}) => 
    apiFetch<BookingCalendarItem[] | { items?: BookingCalendarItem[] }>(`/api/bookings/calendar${toQuery({ from, to, ...params })}`),
  create: (body: Partial<Booking>) => apiFetch<Booking>("/api/bookings", { method: "POST", body: JSON.stringify(body), skipAuth: true }),
  update: (id: number, body: Partial<Booking>) => apiFetch<Booking>(`/api/bookings/${id}`, { method: "PUT", body: JSON.stringify(body) }),
  confirm: (id: number) => apiFetch<Booking>(`/api/bookings/${id}/confirm`, { method: "PUT" }),
  approve: (id: number) => apiFetch<Booking>(`/api/bookings/${id}/approve`, { method: "POST" }),
  submitDepositTransfer: (id: number) => apiFetch<Booking>(`/api/bookings/${id}/deposit/submit-transfer`, { method: "POST", skipAuth: true }),
  confirmDeposit: (id: number, paidAmount: number, transactionCode?: string) =>
    apiFetch<Booking>(`/api/bookings/${id}/deposit/confirm`, { method: "POST", body: JSON.stringify({ paidAmount, transactionCode }) }),
  rejectDepositTransfer: (id: number, reason?: string) =>
    apiFetch<Booking>(`/api/bookings/${id}/deposit/reject`, { method: "POST", body: JSON.stringify({ reason }) }),
  mockPayDeposit: (id: number) => apiFetch<Booking>(`/api/bookings/${id}/deposit/mock-pay`, { method: "POST" }),
  cancel: (id: number, reason?: string, cancelledByVenue?: boolean) => apiFetch<Booking>(`/api/bookings/${id}/cancel`, { method: "PUT", body: JSON.stringify({ reason, cancelledByVenue }) }),
  noShow: (id: number, reason?: string) => apiFetch<Booking>(`/api/bookings/${id}/no-show`, { method: "PUT", body: JSON.stringify({ reason }) }),
  startSession: (id: number, tableId?: number) => apiFetch<Session>(`/api/bookings/${id}/start-session`, { method: "POST", body: JSON.stringify(tableId ? { tableId } : {}) }),
  delete: (id: number) => apiFetch(`/api/bookings/${id}`, { method: "DELETE" })
};

export const customerApi = {
  list: (params: Record<string, string | number | boolean | null | undefined> = {}) => apiFetch<CustomerDto[] | { items?: CustomerDto[], totalCount?: number }>(`/api/customers${toQuery(params as Record<string, string | number | null | undefined>)}`),
  create: (body: Partial<Customer>) => apiFetch<Customer>("/api/customers", { method: "POST", body: JSON.stringify(body) }),
  detail: (id: number) => apiFetch<CustomerDto>(`/api/customers/${id}`),
  update: (id: number, body: UpdateCustomerPayload) => apiFetch<CustomerDto>(`/api/customers/${id}`, { method: "PUT", body: JSON.stringify(body) }),
  updateStatus: (id: number, status: boolean) => apiFetch(`/api/customers/${id}/status`, { method: "PATCH", body: JSON.stringify({ status }) }),
  bookingHistory: (id: number) => apiFetch<{ items?: CustomerBookingHistory[] }>(`/api/customers/${id}/bookings`),
  sessionHistory: (id: number) => apiFetch<{ items?: CustomerSessionHistory[] }>(`/api/customers/${id}/sessions`),
  invoiceHistory: (id: number) => apiFetch<{ items?: CustomerInvoiceHistory[] }>(`/api/customers/${id}/invoices`),
  exchangeVoucher: (id: number, templateId: number) => apiFetch(`/api/customers/${id}/exchange-voucher/${templateId}`, { method: "POST" }),
  pointHistory: (id: number) => apiFetch<{ items?: any[] }>(`/api/customers/${id}/point-history`)
};

export const customerPortalApi = {
  profile: () => apiFetch<CustomerPortalProfile>("/api/customer-portal/me"),
  updateProfile: (body: { fullName: string; phoneNumber: string }) =>
    apiFetch<CustomerPortalProfile>("/api/customer-portal/me", { method: "PUT", body: JSON.stringify(body) }),
  bookings: (params: { pageNumber?: number; pageSize?: number } = {}) =>
    apiFetch<PagedResult<CustomerBookingHistory>>(`/api/customer-portal/me/bookings${toQuery(params)}`),
  sessions: (params: { pageNumber?: number; pageSize?: number } = {}) =>
    apiFetch<PagedResult<CustomerSessionHistory>>(`/api/customer-portal/me/sessions${toQuery(params)}`),
  invoices: (params: { pageNumber?: number; pageSize?: number } = {}) =>
    apiFetch<PagedResult<CustomerInvoiceHistory>>(`/api/customer-portal/me/invoices${toQuery(params)}`),
  invoiceDetail: (id: number) => apiFetch<Invoice>(`/api/customer-portal/me/invoices/${id}`),
  vouchers: (params: { pageNumber?: number; pageSize?: number } = {}) =>
    apiFetch<PagedResult<Discount>>(`/api/customer-portal/me/vouchers${toQuery(params)}`),
  voucherTemplates: () => apiFetch<Discount[]>("/api/customer-portal/me/voucher-templates"),
  exchangeVoucher: (templateId: number) =>
    apiFetch<Discount>(`/api/customer-portal/me/vouchers/${templateId}/exchange`, { method: "POST" }),
  points: (params: { pageNumber?: number; pageSize?: number } = {}) =>
    apiFetch<PagedResult<CustomerPointHistory>>(`/api/customer-portal/me/point-history${toQuery(params)}`)
};

export const sessionApi = {
  list: (params: Record<string, string | number | undefined> = {}) => apiFetch<Session[] | { items?: Session[] }>(`/api/sessions${toQuery(params)}`),
  active: (params: Record<string, string | number | undefined> = {}) => apiFetch<Session[] | { items?: Session[] }>(`/api/sessions/active${toQuery(params)}`),
  detail: (id: number) => apiFetch<Session>(`/api/sessions/${id}`),
  summary: (id: number) => apiFetch<any>(`/api/sessions/${id}/summary`),
  start: (body: { tableId: number; bookingId?: number; customerId?: number }) => apiFetch<Session>("/api/sessions/start", { method: "POST", body: JSON.stringify(body) }),
  end: (id: number) => apiFetch<any>(`/api/sessions/${id}/close`, { method: "POST", body: JSON.stringify({ endedAtUtc: null, generateInvoice: true }) }),
  switchTable: (id: number, sourceAssignmentId: number, newTableId: number) => apiFetch(`/api/sessions/${id}/transfer`, { method: "POST", body: JSON.stringify({ sourceAssignmentId, newTableId }) }),
  transfer: (id: number, body: { sourceAssignmentId: number; toTableId: number; reason?: string; note?: string; markOldTableMaintenance?: boolean; transferAtUtc?: string }) =>
    apiFetch(`/api/sessions/${id}/transfer`, { method: "POST", body: JSON.stringify(body) }),
  releaseTables: (id: number, body: { assignmentIds: number[]; endedAtUtc?: string | null; note?: string }) =>
    apiFetch<ReleaseSessionTablesResponse>(`/api/sessions/${id}/release-tables`, { method: "POST", body: JSON.stringify(body) }),
  reopen: (id: number, body: { reason: string; reopenLastTable?: boolean }) =>
    apiFetch<Session>(`/api/sessions/${id}/reopen`, { method: "POST", body: JSON.stringify(body) })
};

export const orderApi = {
  bySession: (sessionId: number) => apiFetch<Order[]>(`/api/orders?sessionId=${sessionId}`),
  create: (sessionId: number) => apiFetch<Order>("/api/orders", { method: "POST", body: JSON.stringify({ sessionId }) }),
  addItem: (orderId: number, body: { productId: number; quantity: number }) => apiFetch(`/api/orders/${orderId}/items`, { method: "POST", body: JSON.stringify(body) }),
  updateItem: (orderId: number, itemId: number, quantity: number) => apiFetch(`/api/orders/${orderId}/items/${itemId}`, { method: "PUT", body: JSON.stringify({ quantity }) }),
  deleteItem: (orderId: number, itemId: number) => apiFetch(`/api/orders/${orderId}/items/${itemId}`, { method: "DELETE" }),
  cancel: (orderId: number) => apiFetch(`/api/orders/${orderId}/cancel`, { method: "PUT" })
};

export const invoiceApi = {
  list: (params: Record<string, string | number | undefined> = {}) => apiFetch<Invoice[] | { items?: Invoice[] }>(`/api/invoices${toQuery(params)}`),
  detail: (id: number) => apiFetch<Invoice>(`/api/invoices/${id}?_t=${Date.now()}`, { cache: "no-store", headers: { "Cache-Control": "no-cache, no-store, must-revalidate", "Pragma": "no-cache" } }),
  paymentMethods: () => apiFetch<PaymentMethod[]>("/api/invoices/payment-methods"),
  generate: (sessionId: number) => apiFetch<Invoice>(`/api/invoices/generate/${sessionId}`, { method: "POST" }),
  pay: (body: { invoiceId: number; paymentMethodId: number; amount: number; phoneNumber?: string; customerName?: string }) => apiFetch<Invoice>("/api/invoices/payments", { method: "POST", body: JSON.stringify(body) }),
  updateCustomer: (id: number, body: { phoneNumber?: string; fullName?: string; customerId?: number }) => apiFetch<Invoice>(`/api/invoices/${id}/customer`, { method: "PUT", body: JSON.stringify(body) }),
  discount: (id: number, discountCode: string) => apiFetch(`/api/invoices/${id}/discounts`, { method: "POST", body: JSON.stringify({ discountCode }) }),
  removeDiscount: (id: number) => apiFetch(`/api/invoices/${id}/discounts`, { method: "DELETE" }),
  cancel: (id: number, reason: string) => apiFetch(`/api/invoices/${id}/cancel`, { method: "POST", body: JSON.stringify({ reason }) }),
  exportPdf: (id: number) => apiFetch<{ url: string }>(`/api/invoices/${id}/export-pdf`),
  updateProducts: (id: number, products: { productId: number; quantity: number }[]) => 
    apiFetch(`/api/invoices/${id}/products`, { method: "PUT", body: JSON.stringify({ products }) })
};

export const productApi = {
  list: (params: Record<string, string | number | undefined> = {}) => apiFetch<Product[] | { items?: Product[] }>(`/api/products${toQuery(params)}`),
  categories: (params: Record<string, string | number | undefined> = {}) => apiFetch<ProductCategory[] | { items?: ProductCategory[] }>(`/api/products/categories${toQuery(params)}`),
  create: (body: Partial<Product>) => apiFetch<Product>("/api/products", { method: "POST", body: JSON.stringify(body) }),
  update: (id: number, body: Partial<Product>) => apiFetch<Product>(`/api/products/${id}`, { method: "PUT", body: JSON.stringify(body) }),
  delete: (id: number) => apiFetch(`/api/products/${id}`, { method: "DELETE" }),
  createCategory: (body: Partial<ProductCategory>) => apiFetch<ProductCategory>("/api/products/categories", { method: "POST", body: JSON.stringify(body) })
  ,
  updateCategory: (id: number, body: Partial<ProductCategory>) => apiFetch<ProductCategory>(`/api/products/categories/${id}`, { method: "PUT", body: JSON.stringify(body) }),
  deleteCategory: (id: number) => apiFetch(`/api/products/categories/${id}`, { method: "DELETE" })
};

export const pricingApi = {
  plans: (params: Record<string, string | number | boolean | undefined> = {}) => apiFetch<PricingPlan[] | { items?: PricingPlan[] }>(`/api/pricing-plans${toQuery(params)}`, { skipAuth: true }),
  rules: (params: Record<string, string | number | boolean | undefined> = {}) => apiFetch<PricingPlanRule[] | { items?: PricingPlanRule[] }>(`/api/pricing-plans/rules${toQuery(params)}`, { skipAuth: true }),
  createPlan: (body: Partial<PricingPlan>) => apiFetch<PricingPlan>("/api/pricing-plans", { method: "POST", body: JSON.stringify(body) }),
  updatePlan: (id: number, body: Partial<PricingPlan>) => apiFetch<PricingPlan>(`/api/pricing-plans/${id}`, { method: "PUT", body: JSON.stringify(body) }),
  deletePlan: (id: number) => apiFetch(`/api/pricing-plans/${id}`, { method: "DELETE" }),
  createRule: (planId: number, body: Partial<PricingPlanRule>) => apiFetch<PricingPlanRule>(`/api/pricing-plans/${planId}/rules`, { method: "POST", body: JSON.stringify(body) }),
  updateRule: (planId: number, ruleId: number, body: Partial<PricingPlanRule>) => apiFetch<PricingPlanRule>(`/api/pricing-plans/${planId}/rules/${ruleId}`, { method: "PUT", body: JSON.stringify(body) }),
  deleteRule: (planId: number, ruleId: number) => apiFetch(`/api/pricing-plans/${planId}/rules/${ruleId}`, { method: "DELETE" })
};

export const pricingSpecialDateApi = {
  list: (params: Record<string, string | number | undefined> = {}) => apiFetch<PricingSpecialDate[] | { items?: PricingSpecialDate[] }>(`/api/pricing-special-dates${toQuery(params)}`),
  detail: (id: number) => apiFetch<PricingSpecialDate>(`/api/pricing-special-dates/${id}`),
  create: (body: Partial<PricingSpecialDate>) => apiFetch<PricingSpecialDate>("/api/pricing-special-dates", { method: "POST", body: JSON.stringify(body) }),
  update: (id: number, body: Partial<PricingSpecialDate>) => apiFetch<PricingSpecialDate>(`/api/pricing-special-dates/${id}`, { method: "PUT", body: JSON.stringify(body) }),
  delete: (id: number) => apiFetch(`/api/pricing-special-dates/${id}`, { method: "DELETE" })
};

export const adminDashboardApi = {
  summary: () => apiFetch<DashboardSummary>("/api/admin/dashboard/summary"),
  revenue: (params: Record<string, string | number | boolean | undefined> = {}) => apiFetch<RevenuePoint[]>(`/api/admin/dashboard/revenue${toQuery(params)}`),
  activeSessions: () => apiFetch<ActiveSessionDashboard[]>("/api/admin/dashboard/active-sessions"),
  lowStockProducts: () => apiFetch<LowStockProduct[]>("/api/admin/dashboard/low-stock-products"),
  recentAuditLogs: () => apiFetch<RecentAuditLog[]>("/api/admin/dashboard/recent-audit-logs")
};

export const miscApi = {
  notifications: () => apiFetch<Notification[] | { items?: Notification[] }>("/api/notifications"),
  notificationUnreadCount: () => apiFetch<{ count: number }>("/api/notifications/unread-count"),
  notificationRead: (id: number) => apiFetch(`/api/notifications/${id}/read`, { method: "PATCH" }),
  notificationReadAll: () => apiFetch("/api/notifications/read-all", { method: "PATCH" }),
  notificationDelete: (id: number) => apiFetch(`/api/notifications/${id}`, { method: "DELETE" }),
  dashboardSummary: () => apiFetch<DashboardSummary>("/api/dashboard/summary")
};

export const discountApi = {
  list: (params: Record<string, string | number | boolean | undefined> = {}) => apiFetch<Discount[] | { items?: Discount[] }>(`/api/discounts${toQuery(params)}`),
  create: (body: Partial<Discount>) => apiFetch<Discount>("/api/discounts", { method: "POST", body: JSON.stringify(body) }),
  update: (id: number, body: Partial<Discount>) => apiFetch<Discount>(`/api/discounts/${id}`, { method: "PUT", body: JSON.stringify(body) }),
  status: (id: number, isActive: boolean) => apiFetch(`/api/discounts/${id}/status`, { method: "PATCH", body: JSON.stringify({ isActive }) })
};

export const inventoryApi = {
  list: (params: Record<string, string | number | undefined> = {}) => apiFetch<InventoryTransaction[] | { items?: InventoryTransaction[] }>(`/api/inventory-transactions${toQuery(params)}`),
  lowStock: () => apiFetch<LowStockProduct[]>("/api/inventory-transactions/low-stock"),
  adjust: (body: { productId: number; quantity: number; transactionType: number; unitCost?: number; note?: string }) =>
    apiFetch<InventoryTransaction>("/api/inventory-transactions/stock-adjust", { method: "POST", body: JSON.stringify(body) })
};

export const paymentsApi = {
  list: (params: Record<string, string | number | undefined> = {}) => apiFetch<Payment[] | { items?: Payment[] }>(`/api/payments${toQuery(params)}`),
  methods: () => apiFetch<PaymentMethod[]>("/api/payment-methods"),
  createMethod: (body: Partial<PaymentMethod>) => apiFetch<PaymentMethod>("/api/payment-methods", { method: "POST", body: JSON.stringify(body) }),
  updateMethod: (id: number, body: Partial<PaymentMethod>) => apiFetch<PaymentMethod>(`/api/payment-methods/${id}`, { method: "PUT", body: JSON.stringify(body) }),
  methodStatus: (id: number, isActive: boolean) => apiFetch(`/api/payment-methods/${id}/status`, { method: "PATCH", body: JSON.stringify({ isActive }) }),
  refund: (id: number, reason: string) => apiFetch(`/api/payments/${id}/refund`, { method: "POST", body: JSON.stringify({ reason }) })
};

export const reportsApi = {
  revenue: (params: Record<string, string | undefined> = {}) => apiFetch<RevenueReport[]>(`/api/reports/revenue${toQuery(params)}`),
  tableUsage: (params: Record<string, string | undefined> = {}) => apiFetch<TableUsageReport[]>(`/api/reports/table-usage${toQuery(params)}`),
  products: (params: Record<string, string | undefined> = {}) => apiFetch<ProductSalesReport[]>(`/api/reports/products${toQuery(params)}`),
  bookings: (params: Record<string, string | undefined> = {}) => apiFetch<BookingReport[]>(`/api/reports/bookings${toQuery(params)}`)
  ,
  customers: (params: Record<string, string | undefined> = {}) => apiFetch<CustomerReport[]>(`/api/reports/customers${toQuery(params)}`),
  paymentMethods: (params: Record<string, string | undefined> = {}) => apiFetch<PaymentMethodReport[]>(`/api/reports/payment-methods${toQuery(params)}`),
  inventory: (params: Record<string, string | undefined> = {}) => apiFetch<InventoryReport[]>(`/api/reports/inventory${toQuery(params)}`)
};
