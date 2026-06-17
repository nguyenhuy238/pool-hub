import { apiFetch, toQuery } from "@/lib/api/client";
import type {
  ActiveSessionDashboard,
  AuditLog,
  AuthResponse,
  AuthUser,
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
  Product,
  ProductCategory,
  RecentAuditLog,
  RevenuePoint,
  Role,
  Session,
  TableType,
  User,
  VenueLayoutResponse,
  VenueTable,
  Zone,
  BookingCalendarItem,
  CustomerDto
} from "@/types";

export const authApi = {
  login: (body: { email: string; password: string }) => apiFetch<AuthResponse>("/api/auth/login", { method: "POST", body: JSON.stringify(body), skipAuth: true }),
  register: (body: { email: string; password: string; fullName: string; role: string }) => apiFetch<AuthResponse>("/api/auth/register", { method: "POST", body: JSON.stringify(body) }),
  me: () => apiFetch<AuthUser>("/api/auth/me"),
  logout: (refreshToken: string) => apiFetch("/api/auth/logout", { method: "POST", body: JSON.stringify({ refreshToken }) })
};

export const venueApi = {
  layout: () => apiFetch<VenueLayoutResponse>("/api/venue-tables/layout", { skipAuth: true }),
  floors: (params: Record<string, string | number | undefined> = {}) => apiFetch<Floor[] | { items?: Floor[] }>(`/api/floors${toQuery(params)}`),
  zones: (params: Record<string, string | number | undefined> = {}) => apiFetch<Zone[] | { items?: Zone[] }>(`/api/zones${toQuery(params)}`),
  tableTypes: (params: Record<string, string | number | undefined> = {}) => apiFetch<TableType[] | { items?: TableType[] }>(`/api/table-types${toQuery(params)}`),
  tables: (params: Record<string, string | number | undefined> = {}) => apiFetch<VenueTable[] | { items?: VenueTable[] }>(`/api/venue-tables${toQuery(params)}`),
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
  calendar: (from: string, to: string, params: Record<string, string | number | undefined> = {}) => 
    apiFetch<BookingCalendarItem[] | { items?: BookingCalendarItem[] }>(`/api/bookings/calendar${toQuery({ from, to, ...params })}`),
  create: (body: Partial<Booking>) => apiFetch<Booking>("/api/bookings", { method: "POST", body: JSON.stringify(body), skipAuth: true }),
  confirm: (id: number) => apiFetch<Booking>(`/api/bookings/${id}/confirm`, { method: "PUT" }),
  cancel: (id: number) => apiFetch<Booking>(`/api/bookings/${id}/cancel`, { method: "PUT" }),
  delete: (id: number) => apiFetch(`/api/bookings/${id}`, { method: "DELETE" })
};

export const customerApi = {
  list: (params: Record<string, string | number | boolean | null | undefined> = {}) => apiFetch<CustomerDto[] | { items?: CustomerDto[], totalCount?: number }>(`/api/customers${toQuery(params as Record<string, string | number | null | undefined>)}`),
  detail: (id: number) => apiFetch<CustomerDto>(`/api/customers/${id}`),
  update: (id: number, body: Partial<CustomerDto>) => apiFetch<CustomerDto>(`/api/customers/${id}`, { method: "PUT", body: JSON.stringify(body) })
};

export const sessionApi = {
  list: (params: Record<string, string | number | undefined> = {}) => apiFetch<Session[] | { items?: Session[] }>(`/api/sessions${toQuery(params)}`),
  detail: (id: number) => apiFetch<Session>(`/api/sessions/${id}`),
  start: (body: { tableId: number; bookingId?: number; customerId?: number }) => apiFetch<Session>("/api/sessions/start", { method: "POST", body: JSON.stringify(body) }),
  end: (id: number) => apiFetch<Session>(`/api/sessions/${id}/end`, { method: "POST" }),
  switchTable: (id: number, newTableId: number) => apiFetch(`/api/sessions/${id}/switch`, { method: "POST", body: JSON.stringify({ newTableId }) })
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
  detail: (id: number) => apiFetch<Invoice>(`/api/invoices/${id}`),
  paymentMethods: () => apiFetch<PaymentMethod[]>("/api/invoices/payment-methods"),
  generate: (sessionId: number) => apiFetch<Invoice>(`/api/invoices/generate/${sessionId}`, { method: "POST" }),
  pay: (body: { invoiceId: number; paymentMethodId: number; amount: number }) => apiFetch("/api/invoices/payments", { method: "POST", body: JSON.stringify(body) }),
  discount: (id: number, discountCode: string) => apiFetch(`/api/invoices/${id}/discounts`, { method: "POST", body: JSON.stringify({ discountCode }) })
};

export const productApi = {
  list: (params: Record<string, string | number | undefined> = {}) => apiFetch<Product[] | { items?: Product[] }>(`/api/products${toQuery(params)}`),
  categories: (params: Record<string, string | number | undefined> = {}) => apiFetch<ProductCategory[] | { items?: ProductCategory[] }>(`/api/products/categories${toQuery(params)}`),
  create: (body: Partial<Product>) => apiFetch<Product>("/api/products", { method: "POST", body: JSON.stringify(body) }),
  update: (id: number, body: Partial<Product>) => apiFetch<Product>(`/api/products/${id}`, { method: "PUT", body: JSON.stringify(body) }),
  delete: (id: number) => apiFetch(`/api/products/${id}`, { method: "DELETE" }),
  createCategory: (body: Partial<ProductCategory>) => apiFetch<ProductCategory>("/api/products/categories", { method: "POST", body: JSON.stringify(body) })
};

export const pricingApi = {
  plans: (params: Record<string, string | number | undefined> = {}) => apiFetch<PricingPlan[] | { items?: PricingPlan[] }>(`/api/pricing-plans${toQuery(params)}`, { skipAuth: true }),
  rules: (params: Record<string, string | number | undefined> = {}) => apiFetch<PricingPlanRule[] | { items?: PricingPlanRule[] }>(`/api/pricing-plans/rules${toQuery(params)}`, { skipAuth: true }),
  createPlan: (body: Partial<PricingPlan>) => apiFetch<PricingPlan>("/api/pricing-plans", { method: "POST", body: JSON.stringify(body) }),
  updatePlan: (id: number, body: Partial<PricingPlan>) => apiFetch<PricingPlan>(`/api/pricing-plans/${id}`, { method: "PUT", body: JSON.stringify(body) }),
  deletePlan: (id: number) => apiFetch(`/api/pricing-plans/${id}`, { method: "DELETE" }),
  createRule: (planId: number, body: Partial<PricingPlanRule>) => apiFetch<PricingPlanRule>(`/api/pricing-plans/${planId}/rules`, { method: "POST", body: JSON.stringify(body) }),
  deleteRule: (planId: number, ruleId: number) => apiFetch(`/api/pricing-plans/${planId}/rules/${ruleId}`, { method: "DELETE" })
};

export const adminApi = {
  users: (params: Record<string, string | number | undefined> = {}) => apiFetch<User[] | { items?: User[] }>(`/api/users${toQuery(params)}`),
  createUser: (body: { fullName?: string; email?: string; password?: string; role?: string }) => apiFetch<User>("/api/users", { method: "POST", body: JSON.stringify(body) }),
  updateUser: (id: number, body: Partial<User>) => apiFetch<User>(`/api/users/${id}`, { method: "PUT", body: JSON.stringify(body) }),
  updateRoles: (id: number, roles: string[]) => apiFetch(`/api/users/${id}/roles`, { method: "PUT", body: JSON.stringify({ roles }) }),
  updateStatus: (id: number, status: boolean) => apiFetch(`/api/users/${id}/status`, { method: "PATCH", body: JSON.stringify({ status }) }),
  roles: () => apiFetch<Role[]>("/api/roles"),
  auditLogs: (params: Record<string, string | number | undefined> = {}) => apiFetch<AuditLog[] | { items?: AuditLog[] }>(`/api/audit-logs${toQuery(params)}`)
};

export const customerApi = {
  list: (params: Record<string, string | number | boolean | undefined> = {}) => apiFetch<Customer[] | { items?: Customer[] }>(`/api/customers${toQuery(params)}`),
  create: (body: Partial<Customer>) => apiFetch<Customer>("/api/customers", { method: "POST", body: JSON.stringify(body) }),
  update: (id: number, body: Partial<Customer>) => apiFetch<Customer>(`/api/customers/${id}`, { method: "PUT", body: JSON.stringify(body) }),
  updateStatus: (id: number, status: boolean) => apiFetch(`/api/customers/${id}/status`, { method: "PATCH", body: JSON.stringify({ status }) })
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
  dashboardSummary: () => apiFetch<DashboardSummary>("/api/dashboard/summary")
};
