# 🎱 PoolHub – Kế Hoạch Sprint Cho 3 Developer

## Tổng Quan Codebase Hiện Tại (Checkpoint Week 4)

| Layer | Trạng thái |
|---|---|
| Auth / JWT / Refresh Token | ✅ Hoàn thiện |
| User & Role Management | ✅ Hoàn thiện |
| Entities & EF Schema (27 bảng) | ✅ Hoàn thiện |
| Venue CRUD (Floor, Zone, TableType, VenueTable, PricingPlan) | ⚠️ Skeleton — thiếu validation, filter, business logic |
| Booking | ⚠️ Skeleton — thiếu conflict-check, status workflow |
| Session (Start/Close/Transfer) | ⚠️ Skeleton — thiếu concurrency, active-session guard |
| Product & Inventory | ⚠️ Skeleton — thiếu update/delete, stock adjust |
| Order & OrderItem | ⚠️ Skeleton — thiếu cancel, status machine |
| Invoice & Payment | ⚠️ Skeleton — thiếu discount, split-payment, status |
| Notification | ❌ Stub (hardcode) |
| Report / Dashboard | ❌ Chưa có |
| SignalR / Real-time | ❌ Chưa có |
| Unit/Integration Tests | ❌ Project tạo sẵn nhưng chưa có test case |

---

## Phân Công 3 Developer

> **Nguyên tắc:** mỗi người sở hữu hoàn toàn một domain — từ interface → service → controller → test.  
> Các domain có dependency thì người downstream chờ PR của người upstream được merge (hoặc dùng mock).

| Dev | Domain | Entities chính |
|---|---|---|
| **Dev A** | 🏟️ Venue & Booking | Floor, Zone, TableType, VenueTable, PricingPlan, PricingPlanRule, Customer, Booking |
| **Dev B** | 🎱 Session, Order & Invoice | Session, SessionTableAssignment, Order, OrderItem, Invoice, InvoiceLine, InvoiceDiscount, Discount, Payment, PaymentMethod |
| **Dev C** | 📦 Product, Report & Notification | Product, ProductCategory, InventoryTransaction, AuditLog, Notification + Dashboard/Report endpoints |

---

## 🗓️ Sprint 1 — "Hoàn Thiện Business Logic Cốt Lõi" (2 tuần)

**Mục tiêu:** Nâng các skeleton endpoint lên production-ready: validation đầy đủ, error handling, business rule, unit test cơ bản.

---

### Dev A — Venue & Booking

#### Venue (Floor / Zone / TableType / VenueTable)
- [ ] Thêm validation vào tất cả Create/Update DTO (tên không rỗng, `DisplayOrder` ≥ 0, v.v.)
- [ ] Implement `PricingPlansController` — thêm `POST /api/pricing-plans`, `PUT /api/pricing-plans/{id}`, `DELETE /api/pricing-plans/{id}`
- [ ] Implement `POST/PUT/DELETE /api/pricing-plans/{id}/rules` (PricingPlanRule CRUD)
- [ ] Soft-delete cho Floor/Zone/VenueTable (set `IsActive = false` thay vì xóa thật)
- [ ] Filter `isActive` trong query GetAll

#### Booking
- [ ] **Conflict check**: trước khi tạo booking, kiểm tra bàn đó đã có booking `Status=Confirmed` trong khung giờ trùng chưa
- [ ] **Status workflow** (`Status` enum): `Pending(1) → Confirmed(2) → Cancelled(3) → Completed(4)`  
  - `PUT /api/bookings/{id}/confirm` (Admin/Manager/Staff)  
  - `PUT /api/bookings/{id}/cancel` (Admin/Manager hoặc chính customer)  
- [ ] **Customer upsert**: khi tạo booking anonymous, nếu `PhoneNumber` đã tồn tại thì reuse Customer, nếu chưa thì tạo mới
- [ ] Filter booking theo `status`, `date`, `tableId` qua query string
- [ ] Pagination đồng nhất với các endpoint khác

#### Testing
- [ ] Unit test `BookingConflictCheck` (xUnit + FluentAssertions)
- [ ] Unit test soft-delete Venue

#### Deliverable Sprint 1 — Dev A
- Swagger demo: tạo booking → bị conflict → confirm → cancel
- Pricing plan có rule theo giờ/ngày

#### Front-end Pages (Sprint 1) - Dev A
- **Quản lý Khu vực & Bàn (Venue Management):**
  - Trang danh sách Tầng (Floor) & Khu vực (Zone).
  - Trang danh sách Loại bàn (TableType) và Bàn (VenueTable), hỗ trợ filter theo trạng thái hoạt động.
- **Quản lý Bảng giá (Pricing):**
  - Trang danh sách Bảng giá (PricingPlan).
  - Trang chi tiết/thêm/sửa Bảng giá và Quy tắc giá (Rules).
- **Quản lý Đặt bàn (Booking):**
  - Trang danh sách Đặt bàn (hiển thị phân trang, filter theo status, ngày, bàn).
  - Modal/Form Tạo Đặt bàn mới (xử lý báo lỗi trực quan khi conflict giờ).
  - Flow Chuyển trạng thái (Confirm/Cancel booking).

---

### Dev B — Session, Order & Invoice

#### Session
- [ ] **Active session guard**: một bàn không thể có 2 session `Status=Active` cùng lúc — throw `BusinessRuleException`
- [ ] `GET /api/sessions` — list session có filter `status`, `tableId`, `date` + pagination
- [ ] `GET /api/sessions/{id}` — detail kèm danh sách bàn đang gán
- [ ] **Transfer table**: validate bàn mới không đang bị session khác dùng
- [ ] Tự động update `VenueTable.OperationalStatus` khi session start/close (1=Available, 2=Occupied)

#### Order & OrderItem
- [ ] `GET /api/orders/{id}` — detail kèm items
- [ ] `GET /api/orders?sessionId=x` — lấy orders theo session
- [ ] `PUT /api/orders/{orderId}/items/{itemId}` — cập nhật số lượng (điều chỉnh stock + inventory transaction)
- [ ] `DELETE /api/orders/{orderId}/items/{itemId}` — xóa item (hoàn stock)
- [ ] `PUT /api/orders/{orderId}/cancel` — huỷ order, hoàn toàn bộ stock

#### Invoice & Payment
- [ ] **Tính tiền bàn**: khi generate invoice, tính thêm phí chơi bàn dựa vào `PricingPlanRule` (giờ × HourlyRate)
- [ ] `POST /api/invoices/{id}/discounts` — áp discount vào invoice (`InvoiceDiscount`)
- [ ] `GET /api/invoices/{id}` — detail kèm lines + discounts + payments
- [ ] **Split payment**: một invoice có thể có nhiều Payment (ví dụ: nửa tiền mặt, nửa chuyển khoản)
- [ ] Validate tổng payment ≤ FinalAmount

#### Testing
- [ ] Unit test active-session guard
- [ ] Unit test invoice generation (pricing calculation)

#### Deliverable Sprint 1 — Dev B
- Swagger demo: start session → add orders → generate invoice với pricing bàn → áp discount → thanh toán split

---

### Dev C — Product, Report & Notification

#### Product & Inventory
- [ ] `PUT /api/products/{id}` — update thông tin sản phẩm
- [ ] `DELETE /api/products/{id}` — soft-delete (`IsActive = false`)
- [ ] `PUT /api/products/categories/{id}` / `DELETE /api/products/categories/{id}`
- [ ] `POST /api/products/{id}/stock-adjust` — điều chỉnh tồn kho thủ công (nhập hàng, kiểm kê)  
  Body: `{ "quantityChange": 50, "reason": "IMPORT" }`
- [ ] `GET /api/inventory-transactions?productId=x` — lịch sử tồn kho có filter + pagination

#### Notification (thay stub bằng real logic)
- [ ] Lưu notification vào DB khi: booking được confirm, session kết thúc
- [ ] `GET /api/notifications` — lấy notifications của user hiện tại (phân trang)
- [ ] `PUT /api/notifications/{id}/read` — đánh dấu đã đọc
- [ ] `PUT /api/notifications/read-all` — đánh dấu tất cả đã đọc

#### AuditLog
- [ ] Implement `AuditMiddleware` (hoặc `AuditService`) — tự động ghi log mỗi khi có thay đổi quan trọng (Create/Update/Delete booking, session, payment)
- [ ] Filter audit log theo `entityName`, `userId`, `dateRange`

#### Testing
- [ ] Unit test stock adjust (positive/negative case)
- [ ] Unit test notification creation

#### Deliverable Sprint 1 — Dev C
- Swagger demo: thêm sản phẩm → nhập kho → xem lịch sử tồn kho
- Notification hiển thị khi booking được confirm

---

### 📋 Sprint 1 Review Meeting Agenda

| # | Nội dung | Thời gian |
|---|---|---|
| 1 | Dev A demo: Venue + Booking workflow | 15 phút |
| 2 | Dev B demo: Session → Invoice + Payment | 15 phút |
| 3 | Dev C demo: Product + Inventory + Notification | 15 phút |
| 4 | Review PR/merge conflicts, dependency mismatch | 10 phút |
| 5 | Retrospective: gì chạy tốt, gì bị block | 10 phút |
| 6 | Cập nhật plan Sprint 2 | 15 phút |

**Definition of Done Sprint 1:**
- [ ] Tất cả endpoint mới pass Swagger manual test
- [ ] Mỗi dev ≥ 5 unit test case xanh
- [ ] Không còn hardcode magic-number (thay bằng constant/enum)
- [ ] Tất cả exception trả về đúng format `ApiResponse.Fail(...)` qua middleware

---

## 🗓️ Sprint 2 — "Real-time, Dashboard & Hardening" (2 tuần)

**Mục tiêu:** Thêm tính năng nâng cao, dashboard, real-time notification, và integration test.

---

### Dev A — Booking Management & Venue Layout

**Các Jira Subtasks (Phần Booking):**
- [x] **POOL1-72**: Migration: tạo bảng Bookings (id, customerId, tableId, startTimeUtc, endTimeUtc, status, v.v.)
- [x] **POOL1-73**: `GET /api/tables/availability?date=&type=` — (Đã triển khai thông qua `GET /api/bookings/calendar` để Frontend tự map lịch trống).
- [x] **POOL1-74**: `POST /api/bookings` — Guest/Cashier đặt lịch, check conflict, INSERT booking.
- [x] **POOL1-75**: `PATCH /api/bookings/{id}/confirm` — Staff xác nhận booking.
- [x] **POOL1-76**: `PATCH /api/bookings/{id}/cancel` — Huỷ booking.
- [ ] **POOL1-77**: Background job (IHostedService): auto-release bàn sau 15 phút nếu khách NoShow.
- [x] **POOL1-78**: `GET /api/bookings` — Staff/Manager xem danh sách bookings (paginated, filter).

**Các tính năng khác đã làm:**
- [x] `GET /api/venue-tables/layout` — Trả về toàn bộ bàn kèm cấu trúc sơ đồ (tầng/khu vực) và `currentStatus` (realtime từ session).
- [x] Thêm `Customer` endpoints: `GET /api/customers`, `GET /api/customers/{id}`, `PUT /api/customers/{id}`.

#### Front-end Pages (Sprint 2) - Dev A
- **Sơ đồ Bàn trực quan (Venue Layout / POS View):**
  - Giao diện sơ đồ hiển thị các bàn theo Tầng/Khu vực.
  - Hiển thị trực quan trạng thái bàn hiện tại (Trống, Đang chơi, Đã đặt...).
- **Lịch Đặt Bàn (Booking Calendar):**
  - Giao diện lịch (Calendar view) để xem trực quan các lịch đặt bàn theo ngày/tuần/tháng.
- **Quản lý Khách hàng (Customer Management):**
  - Trang danh sách Khách hàng.
  - Trang chi tiết và chỉnh sửa thông tin Khách hàng (hiển thị thêm lịch sử booking của khách).

---

### Dev B — SignalR Real-time & Invoice Export

- [ ] **SignalR Hub** `TableStatusHub`:  
  - Event `TableStatusChanged` khi session start/close/transfer  
  - Event `NewOrderCreated` khi có order mới trong session  
- [ ] Kết nối `SessionService` → `IHubContext` để push event
- [ ] `GET /api/invoices/{id}/export-pdf` — xuất invoice PDF (dùng `QuestPDF` hoặc `iTextSharp`)
- [ ] `GET /api/sessions/{id}/summary` — tổng hợp: tổng giờ chơi, tổng đồ ăn/uống, tổng tiền
- [ ] Integration test: start session → add order → generate invoice

---

### Dev C — Dashboard & Report

- [ ] `GET /api/reports/daily-revenue?date=` — doanh thu ngày (bàn + F&B)
- [ ] `GET /api/reports/monthly-summary?year=&month=` — tổng hợp tháng
- [ ] `GET /api/reports/top-products?from=&to=&limit=` — sản phẩm bán chạy
- [ ] `GET /api/reports/table-utilization?from=&to=` — tỷ lệ sử dụng bàn theo giờ
- [ ] `GET /api/dashboard/stats` — KPI nhanh: sessions hôm nay, doanh thu hôm nay, sản phẩm sắp hết hàng, booking chờ confirm
- [ ] Low-stock alert notification: khi `StockQuantity < threshold` sau mỗi lần order, tạo notification cho Manager

---

### 📋 Sprint 2 Review Meeting Agenda

| # | Nội dung | Thời gian |
|---|---|---|
| 1 | Dev A demo: Venue layout + Booking calendar | 15 phút |
| 2 | Dev B demo: SignalR live + Invoice PDF | 15 phút |
| 3 | Dev C demo: Dashboard stats + Report | 15 phút |
| 4 | Review performance (query N+1, missing index) | 10 phút |
| 5 | Security review (authorization đúng role chưa) | 10 phút |
| 6 | Plan Sprint 3 | 10 phút |

**Definition of Done Sprint 2:**
- [ ] SignalR event bắn đúng khi session thay đổi
- [ ] Dashboard `/api/dashboard/stats` trả về < 500ms
- [ ] Report endpoint có cache (MemoryCache hoặc Redis) cho query nặng
- [ ] ≥ 3 integration test per dev

---

## 🗓️ Sprint 3 — "Polish, Performance & Deploy" (2 tuần)

**Mục tiêu:** Cleanup, optimize, viết test coverage đầy đủ, chuẩn bị deploy.

---

### Dev A — Polish, Performance & Handoff
- [ ] Seed data thực tế (5+ floor, 20+ bàn, pricing plan theo giờ vàng/thường)
- [ ] Postman collection update đầy đủ cho tất cả endpoint Booking + Venue
- [ ] Review và đồng nhất response schema toàn bộ Booking/Venue endpoint
- [ ] Thêm index cho `Booking(TableId, StartTimeUtc, EndTimeUtc)` trong migration để tối ưu check conflict
- [ ] Tối ưu query N+1 cho các endpoint Venue và Booking

#### Front-end Pages (Sprint 3) - Dev A
- **Tối ưu trải nghiệm & UI Polish:**
  - Đồng bộ hiển thị lỗi (Error handling/Toasts) trên các form Venue/Booking dựa trên error response API.
  - Thêm các Skeleton Loading, Empty State cho sơ đồ bàn và danh sách.
  - Cải thiện UX/UI phần check conflict đặt bàn trên Calendar.

### Dev B
- [ ] Concurrency: `RowVersion` optimistic locking cho `Product.StockQuantity` khi nhiều order cùng lúc
- [ ] Rate limiting cho Invoice generate (tránh double-generate)
- [ ] Postman collection Session + Invoice
- [ ] Load test nhẹ với k6 hoặc BenchmarkDotNet

### Dev C
- [ ] Swagger documentation: mô tả đầy đủ `summary`, `remarks`, response code cho tất cả endpoint
- [ ] Health check endpoint `GET /health` (database ping, disk)
- [ ] `.github/workflows/ci.yml` — CI pipeline: restore → build → test
- [ ] Docker `Dockerfile` + `docker-compose.yml` (API + SQL Server)
- [ ] README cập nhật đầy đủ cho Sprint 3

---

### 📋 Sprint 3 Final Review & Demo Day

| # | Nội dung |
|---|---|
| 1 | Full end-to-end demo: booking → session → order → invoice → payment |
| 2 | Demo Dashboard & Report |
| 3 | Xem CI pipeline chạy xanh |
| 4 | Demo Docker compose chạy local |
| 5 | Tổng kết deliverable, quyết định backlog tiếp theo |

---

## 🔗 Dependency Map

```
Dev A (Venue/Booking)
    └─► Dev B cần TableId hợp lệ từ Venue để Start Session
    └─► Dev B cần BookingId từ Booking để link vào Session

Dev B (Session/Order/Invoice)
    └─► Dev C cần Product CRUD (update/delete) để Order hoạt động đúng
    └─► Dev C nhận event từ Dev B để trigger Notification

Dev C (Product/Report/Notification)
    └─► Report cần Invoice/Payment data từ Dev B
    └─► Dashboard cần Session data từ Dev B + Booking data từ Dev A
```

> **Giải pháp tránh bị block:** mỗi dev tạo interface mock (dùng `Moq` hoặc stub trong test) để làm độc lập, chỉ integrate khi PR upstream đã merge vào `main`.

---

## 📁 Quy Ước Làm Việc

| Quy tắc | Chi tiết |
|---|---|
| **Branch naming** | `feature/devA-venue-conflict-check`, `feature/devB-signalr`, `feature/devC-dashboard` |
| **PR size** | Mỗi PR ≤ 400 LOC để review nhanh |
| **Commit style** | `feat:`, `fix:`, `test:`, `docs:` |
| **API versioning** | Chưa cần, giữ `/api/` cho Sprint 1-2 |
| **Error format** | Luôn dùng `ApiResponse.Fail(message)` qua `ExceptionMiddleware` |
| **Status codes** | Dùng đúng HTTP code: 200/201/204/400/401/403/404/409/422 |

