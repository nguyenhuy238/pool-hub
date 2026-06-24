# PoolHub Technical Specification

## 1. Tech stack hiện tại
- Backend: ASP.NET Core Web API trên .NET 10.
- ORM: Entity Framework Core.
- Database target: SQL Server.
- Auth: JWT Bearer + refresh token.
- Password hashing: BCrypt.
- Frontend: Next.js 14, React 18, TypeScript.

## 2. Solution structure
- `PoolHub.API`: entrypoint, controllers, middleware, auth, swagger.
- `PoolHub.Services`: use-case/business services theo domain.
- `PoolHub.Infrastructure`: DbContext, migrations, repositories, seed.
- `PoolHub.Core`: entities, DTOs, interfaces, enums.
- `PoolHub.Shared`: response envelope, exceptions, constants, helpers.
- `frontend`: Next.js app router, service layer, UI pages.

## 3. Data model summary
Seed script và DbContext cho thấy 27 bảng cốt lõi, gom theo nhóm:
- IAM: `users`, `roles`, `user_roles`, `permissions`, `role_permissions`, `refresh_tokens`, `password_reset_tokens`.
- CRM: `customers`.
- Venue: `floors`, `zones`, `table_types`, `venue_tables`.
- Pricing/Booking: `pricing_plans`, `pricing_plan_rules`, `bookings`, `sessions`, `session_table_assignments`.
- Product/Inventory: `product_categories`, `products`, `inventory_transactions`.
- Sales: `orders`, `order_items`, `discounts`, `invoices`, `invoice_lines`, `invoice_discounts`, `payment_methods`, `payments`.
- Ops: `notifications`, `audit_logs`, `site_settings`, `media_assets`.

## 4. Đặc tả contract kỹ thuật
- Response envelope dùng `ApiResponse<T>` với `success`, `message`, `data`, `errors`.
- Nhiều list API có paging via `PagedResult<T>`.
- Authentication flow có access token và refresh token rotation.
- Nhiều nghiệp vụ multi-step phải đi qua transaction trong service layer.
- DbContext tự sinh audit entry cho mutation không nằm trong nhóm audit thủ công.

## 5. Những quyết định kỹ thuật đã chọn
- Dùng layered architecture để giảm coupling giữa controller và business logic.
- Dùng role-based authorization thay vì logic quyền rải trong controller.
- Dùng rate limit riêng cho password recovery để giảm abuse.
- Dùng static case mapping sang snake_case cho cột DB.
- Dùng seed data để phục vụ demo/test nhanh.

## 6. Các lệch cần lưu ý
- Một số file tài liệu cũ ghi backend net8.0; code hiện tại là net10.0.
- Swagger legacy và frontend service layer vẫn còn một vài chỗ có thể cần đồng bộ lại với controller mới.