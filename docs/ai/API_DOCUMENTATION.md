# PoolHub API Documentation

## 1. Nguồn kiểm tra API
Ưu tiên kiểm tra controller trong `PoolHub.API/Controllers` và service map ở `frontend/src/lib/api/endpoints.ts`, sau đó đối chiếu `swagger_poolhub.yaml`.

## 2. Nhóm endpoint chính
- Auth: `/api/auth/*`
- Users/Roles/Customers: `/api/users`, `/api/roles`, `/api/customers`
- Venue: `/api/floors`, `/api/zones`, `/api/table-types`, `/api/venue-tables`, `/api/pricing-plans`
- Booking: `/api/bookings/*`
- Session: `/api/sessions/*`
- Order: `/api/orders/*`
- Invoice/Payment: `/api/invoices/*`, `/api/payments/*`
- Product/Inventory: `/api/products/*`, `/api/inventory-transactions/*`
- Ops: `/api/notifications/*`, `/api/audit-logs/*`, `/api/dashboard/*`, `/api/reports/*`

## 3. Endpoint highlights đã xác nhận trong code
- `POST /api/auth/login`
- `POST /api/auth/register`
- `GET /api/auth/me`
- `POST /api/auth/refresh-token`
- `POST /api/auth/logout`
- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`
- `GET /api/bookings/calendar`
- `GET /api/bookings/public/calendar`
- `POST /api/bookings/public`
- `POST /api/sessions/start`
- `POST /api/sessions/{id}/close`
- `POST /api/sessions/{id}/transfer`
- `POST /api/invoices/generate/{sessionId}`
- `POST /api/invoices/payments`
- `POST /api/invoices/{id}/discounts`
- `POST /api/invoices/{id}/cancel`
- `GET /api/invoices/{id}/export-pdf`

## 4. Public vs protected
- Public: login, register, forgot/reset password, public booking flow, public calendar, venue availability/public layout paths theo controller hiện có.
- Protected: phần lớn CRUD vận hành, session, invoice, order, dashboard, audit, notification.
- Role-sensitive: Admin/Manager/Staff tùy module.

## 5. Ghi chú cho frontend
- Frontend hiện có nhiều service module đã map trực tiếp tới API thật.
- Nếu service gọi 404, ưu tiên kiểm tra controller thực tế trước khi sửa frontend.
- Service frontend cần bám theo route chuẩn trong controller.

## 6. Cách dùng thực tế
1. Login bằng seed account.
2. Copy `accessToken` vào frontend hoặc Swagger.
3. Gọi API protected bằng Bearer token.
4. Dùng refresh token khi access token hết hạn.
