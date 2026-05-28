# SYSTEM_CONTEXT - PoolHub

## 1) Product Overview
PoolHub là hệ thống quản lý trung tâm giải trí bi-a, bao gồm các nhóm chức năng chính:
- Quản trị người dùng và phân quyền.
- Quản lý bàn/khu vực/tầng và giá theo khung giờ.
- Booking và phiên chơi thực tế (session).
- Gọi món, quản lý tồn kho cơ bản.
- Lập hóa đơn, thanh toán, báo cáo doanh thu/vận hành.

## 2) Current Implementation Status (as of 2026-05-28)
- Source code backend hiện tại đang là skeleton ASP.NET Core Web API (`net8.0`).
- Code nghiệp vụ thực tế CHƯA được triển khai trong project `pool-hub/`.
- Tài liệu nghiệp vụ đầy đủ đang nằm ở các file phân tích thiết kế và `swagger_poolhub.yaml`.

## 3) Main Business Modules (theo OpenAPI)
Các endpoint hiện có trong đặc tả API:
- Auth & Settings: `/auth/*`, `/settings/pricing*`
- Promotions: `/promotions`
- Tables & Floor Map: `/tables*`
- Sessions: `/sessions/*`
- Bookings: `/bookings*`
- Products/Orders/Inventory: `/products*`, `/orders*`, `/inventory*`
- Invoices/Payments: `/invoices*`
- Reports: `/reports/*`
- System Alerts & Extensions: `/system/alerts`, `/extend/game-center`

## 4) Data Model Context
File seed SQL đang phản ánh hệ thống 27 bảng, gồm các nhóm:
- IAM: `users`, `roles`, `user_roles`, `refresh_tokens`
- CRM: `customers`
- Venue: `floors`, `zones`, `table_types`, `venue_tables`
- Pricing & Booking: `pricing_plans`, `pricing_plan_rules`, `bookings`, `sessions`, `session_table_assignments`
- Product & Inventory: `product_categories`, `products`, `inventory_transactions`
- Sales: `orders`, `order_items`, `discounts`, `invoices`, `invoice_lines`, `invoice_discounts`, `payment_methods`, `payments`
- Ops: `notifications`, `audit_logs`

## 5) Technology Context
- Runtime: .NET 8 Web API
- API documentation/runtime swagger: Swashbuckle.AspNetCore
- Target DB (theo tài liệu): SQL Server
- Kiến trúc mục tiêu (theo tài liệu tuần): backend nhiều lớp, JWT auth, RBAC

## 6) Gaps to Bridge
- Code chưa match OpenAPI/ERD.
- Chưa có domain models, DbContext, repository/service layers.
- Chưa có authentication/authorization thật.
- Chưa có migration + seed tự động theo môi trường.

## 7) Development Direction
Ưu tiên triển khai theo thứ tự:
1. Foundation: solution structure, shared contracts, error envelope.
2. Data layer: schema migration + EF Core mapping theo ERD.
3. Auth/JWT/RBAC: login, refresh token, policy-based authorization.
4. Core operations: tables, sessions, bookings.
5. Sales flow: orders -> invoices -> payments.
6. Reporting & alerts.
