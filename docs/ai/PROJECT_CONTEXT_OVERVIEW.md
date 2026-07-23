# PoolHub Product Context

## 1. Product Vision
PoolHub là hệ thống quản lý trung tâm giải trí/bida với mục tiêu số hóa các nghiệp vụ vận hành chính: đặt bàn, mở phiên chơi, gọi món, xuất hóa đơn, thanh toán, quản lý giá và giám sát hoạt động.

## 2. Người dùng mục tiêu
- Khách hàng: xem bàn trống, đặt bàn, tra cứu trạng thái đặt chỗ.
- Staff: mở phiên, chuyển bàn, ghi nhận order, tạo invoice và thanh toán.
- Manager/Admin: quản lý danh mục, giá, báo cáo, quyền truy cập, audit và cấu hình hệ thống.

## 3. Vấn đề hệ thống giải quyết
- Giảm thao tác thủ công khi quản lý lịch đặt và phiên chơi.
- Đồng bộ dữ liệu giữa bàn, booking, session, order và invoice.
- Chuẩn hóa phân quyền để từng vai trò chỉ thấy đúng phần việc.
- Có audit và báo cáo để theo dõi vận hành.

## 4. Mục tiêu thành công hiện tại
- Backend chạy được với JWT, role-based authorization và EF Core.
- Frontend có thể gọi các API chính cho booking, session, invoice, dashboard và danh mục vận hành.
- Seed data đủ để demo các luồng cốt lõi.
- Tài liệu hóa đầy đủ để AI có thể hiểu ngữ cảnh mà không phải suy đoán từ đầu.

## 5. Phạm vi sản phẩm đang thể hiện trong repo
- Authentication và refresh token.
- Quản lý người dùng, role, quyền và customer.
- Quản lý venue: floors, zones, table types, venue tables, pricing plans.
- Booking và calendar view.
- Session lifecycle và chuyển bàn.
- Product, order, invoice, payment, discount.
- Notifications, audit logs, dashboard và reports.

## 6. Ghi chú về khác biệt tài liệu cũ
- Một số tài liệu cũ mô tả backend là net8.0 skeleton; code hiện tại đã nâng lên net10.0 và có nhiều controller/service thật.
- Swagger/OpenAPI legacy vẫn hữu ích để hiểu ý định ban đầu, nhưng controller trong `PoolHub.API` là nguồn kiểm tra thực tế trước khi code.
