
/*
============================================================
 PoolHub Seed Data Script
 Target DB: SQL Server
 Scope: 27-table ERD
 Notes:
 - This script assumes tables already exist.
 - Uses explicit identity values for predictable demo data.
 - Password hashes are demo placeholders; replace with BCrypt hashes in production.
 - Run in a clean database or after deleting existing seed data.
============================================================
*/

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    ------------------------------------------------------------
    -- Optional cleanup
    -- Uncomment this block if you want to reset seed data.
    ------------------------------------------------------------
    /*
    DELETE FROM audit_logs;
    DELETE FROM notifications;
    DELETE FROM payments;
    DELETE FROM payment_methods;
    DELETE FROM invoice_discounts;
    DELETE FROM invoice_lines;
    DELETE FROM invoices;
    DELETE FROM discounts;
    DELETE FROM order_items;
    DELETE FROM orders;
    DELETE FROM inventory_transactions;
    DELETE FROM products;
    DELETE FROM product_categories;
    DELETE FROM session_table_assignments;
    DELETE FROM sessions;
    DELETE FROM bookings;
    DELETE FROM pricing_plan_rules;
    DELETE FROM pricing_plans;
    DELETE FROM venue_tables;
    DELETE FROM table_types;
    DELETE FROM zones;
    DELETE FROM floors;
    DELETE FROM customers;
    DELETE FROM refresh_tokens;
    DELETE FROM user_roles;
    DELETE FROM roles;
    DELETE FROM users;
    */

    ------------------------------------------------------------
    -- 1. roles
    ------------------------------------------------------------
    SET IDENTITY_INSERT roles ON;

    INSERT INTO roles (
        role_id, name, description, is_system, created_at_utc, updated_at_utc
    )
    VALUES
        (1, N'Admin',   N'Full system administrator', 1, SYSUTCDATETIME(), NULL),
        (2, N'Manager', N'Business and operation manager', 1, SYSUTCDATETIME(), NULL),
        (3, N'Staff',   N'Floor staff who manages tables, sessions, orders, invoices and payments', 1, SYSUTCDATETIME(), NULL);

    SET IDENTITY_INSERT roles OFF;


    ------------------------------------------------------------
    -- 2. users
    ------------------------------------------------------------
    SET IDENTITY_INSERT users ON;

    INSERT INTO users (
        user_id, public_id, full_name, email, password_hash,
        phone_number, avatar_url, email_confirmed, status,
        last_login_at_utc, created_at_utc, updated_at_utc
    )
    VALUES
        (1, '11111111-1111-1111-1111-111111111111', N'PoolHub Admin',   N'admin@poolhub.local',   N'$2a$12$Ahs/.D69Cz.P2K377Jb8eOZeP4W55IaIhIUmicWwREtqGa7ggMAre', N'0900000001', NULL, 1, 1, NULL, SYSUTCDATETIME(), NULL),
        (2, '22222222-2222-2222-2222-222222222222', N'Nguyễn Quản Lý',  N'manager@poolhub.local', N'$2a$12$surFTrJVqBrnaBf/y/50SeH342ENq6wBs94y7hGHt056zMSteK2IW', N'0900000002', NULL, 1, 1, NULL, SYSUTCDATETIME(), NULL),
        (3, '33333333-3333-3333-3333-333333333333', N'Trần Nhân Viên',  N'staff@poolhub.local',   N'$2a$12$eLAs9QmdqQ25QYqMESH6Rehjczu8HXLDLi/RfsIMQT1dmwQ/j2jqO', N'0900000003', NULL, 1, 1, NULL, SYSUTCDATETIME(), NULL),
        (4, '44444444-4444-4444-4444-444444444444', N'Lê Nhân Viên 2',  N'staff2@poolhub.local',  N'$2a$12$Uqx2RtOx2kTGELQujgemzeWn0i.K9Nk6lHTOt3tGPNohzlTCjWpQa', N'0900000004', NULL, 1, 1, NULL, SYSUTCDATETIME(), NULL);

    SET IDENTITY_INSERT users OFF;


    ------------------------------------------------------------
    -- 3. user_roles
    ------------------------------------------------------------
    INSERT INTO user_roles (
        user_id, role_id, assigned_at_utc, assigned_by_user_id
    )
    VALUES
        (1, 1, SYSUTCDATETIME(), NULL),
        (2, 2, SYSUTCDATETIME(), 1),
        (3, 3, SYSUTCDATETIME(), 1),
        (4, 3, SYSUTCDATETIME(), 1);


    ------------------------------------------------------------
    -- 4. refresh_tokens
    ------------------------------------------------------------
    SET IDENTITY_INSERT refresh_tokens ON;

    INSERT INTO refresh_tokens (
        refresh_token_id, user_id, token_hash, expires_at_utc,
        revoked_at_utc, created_at_utc, created_by_ip, revoked_by_ip, is_revoked
    )
    VALUES
        (1, 1, N'DEMO_REFRESH_TOKEN_HASH_ADMIN', DATEADD(DAY, 7, SYSUTCDATETIME()), NULL, SYSUTCDATETIME(), N'127.0.0.1', NULL, 0),
        (2, 3, N'DEMO_REFRESH_TOKEN_HASH_STAFF', DATEADD(DAY, 7, SYSUTCDATETIME()), NULL, SYSUTCDATETIME(), N'127.0.0.1', NULL, 0);

    SET IDENTITY_INSERT refresh_tokens OFF;


    ------------------------------------------------------------
    -- 5. customers
    ------------------------------------------------------------
    SET IDENTITY_INSERT customers ON;

    INSERT INTO customers (
        customer_id, public_id, full_name, phone_number, email,
        note, status, created_at_utc, updated_at_utc
    )
    VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', N'Phạm Minh Anh', N'0911111111', N'minhanh@example.com', N'Khách quen, thích bàn VIP', 1, SYSUTCDATETIME(), NULL),
        (2, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', N'Hoàng Đức Nam', N'0922222222', N'ducnam@example.com', NULL, 1, SYSUTCDATETIME(), NULL),
        (3, 'cccccccc-cccc-cccc-cccc-cccccccccccc', N'Vũ Gia Huy',    N'0933333333', NULL, N'Khách vãng lai', 1, SYSUTCDATETIME(), NULL);

    SET IDENTITY_INSERT customers OFF;


    ------------------------------------------------------------
    -- 6. floors
    ------------------------------------------------------------
    SET IDENTITY_INSERT floors ON;

    INSERT INTO floors (
        floor_id, name, description, display_order, is_active, created_at_utc
    )
    VALUES
        (1, N'Tầng 1', N'Khu vực chính gần quầy thu ngân', 1, 1, SYSUTCDATETIME()),
        (2, N'Tầng 2', N'Khu vực VIP và phòng riêng', 2, 1, SYSUTCDATETIME());

    SET IDENTITY_INSERT floors OFF;


    ------------------------------------------------------------
    -- 7. zones
    ------------------------------------------------------------
    SET IDENTITY_INSERT zones ON;

    INSERT INTO zones (
        zone_id, floor_id, name, description, display_order, is_active, created_at_utc
    )
    VALUES
        (1, 1, N'Zone A', N'Khu bàn thường tầng 1', 1, 1, SYSUTCDATETIME()),
        (2, 1, N'Zone B', N'Khu gần quầy bar', 2, 1, SYSUTCDATETIME()),
        (3, 2, N'VIP Zone', N'Khu bàn VIP tầng 2', 1, 1, SYSUTCDATETIME());

    SET IDENTITY_INSERT zones OFF;


    ------------------------------------------------------------
    -- 8. table_types
    ------------------------------------------------------------
    SET IDENTITY_INSERT table_types ON;

    INSERT INTO table_types (
        table_type_id, name, code, description, default_capacity, is_active, created_at_utc
    )
    VALUES
        (1, N'Pool Standard', N'POOL_STD', N'Bàn pool tiêu chuẩn', 4, 1, SYSUTCDATETIME()),
        (2, N'Pool VIP',      N'POOL_VIP', N'Bàn pool VIP', 6, 1, SYSUTCDATETIME()),
        (3, N'Carom',         N'CAROM',    N'Bàn carom', 4, 1, SYSUTCDATETIME());

    SET IDENTITY_INSERT table_types OFF;


    ------------------------------------------------------------
    -- 9. venue_tables
    -- operational_status: 1 Active, 2 Maintenance, 3 Inactive
    ------------------------------------------------------------
    SET IDENTITY_INSERT venue_tables ON;

    INSERT INTO venue_tables (
        table_id, public_id, zone_id, table_type_id, table_code, table_name,
        capacity, position_x, position_y, operational_status, created_at_utc, updated_at_utc
    )
    VALUES
        (1, '10000000-0000-0000-0000-000000000001', 1, 1, N'A01', N'Bàn A01', 4, 100.00, 120.00, 1, SYSUTCDATETIME(), NULL),
        (2, '10000000-0000-0000-0000-000000000002', 1, 1, N'A02', N'Bàn A02', 4, 260.00, 120.00, 1, SYSUTCDATETIME(), NULL),
        (3, '10000000-0000-0000-0000-000000000003', 2, 3, N'B01', N'Bàn B01 Carom', 4, 100.00, 300.00, 1, SYSUTCDATETIME(), NULL),
        (4, '10000000-0000-0000-0000-000000000004', 3, 2, N'V01', N'Bàn VIP 01', 6, 160.00, 160.00, 1, SYSUTCDATETIME(), NULL),
        (5, '10000000-0000-0000-0000-000000000005', 3, 2, N'V02', N'Bàn VIP 02', 6, 320.00, 160.00, 2, SYSUTCDATETIME(), NULL);

    SET IDENTITY_INSERT venue_tables OFF;


    ------------------------------------------------------------
    -- 10. pricing_plans
    ------------------------------------------------------------
    SET IDENTITY_INSERT pricing_plans ON;

    INSERT INTO pricing_plans (
        pricing_plan_id, name, description, is_default, is_active,
        starts_at_utc, ends_at_utc, created_at_utc, updated_at_utc
    )
    VALUES
        (1, N'Bảng giá mặc định 2026', N'Áp dụng cho ngày thường và cuối tuần', 1, 1, '2026-01-01T00:00:00', NULL, SYSUTCDATETIME(), NULL);

    SET IDENTITY_INSERT pricing_plans OFF;


    ------------------------------------------------------------
    -- 11. pricing_plan_rules
    -- day_of_week: 0 Sunday, 1 Monday, ..., 6 Saturday
    ------------------------------------------------------------
    SET IDENTITY_INSERT pricing_plan_rules ON;

    INSERT INTO pricing_plan_rules (
        pricing_plan_rule_id, pricing_plan_id, table_type_id, day_of_week,
        start_time, end_time, hourly_rate, minimum_minutes,
        billing_block_minutes, is_active, created_at_utc
    )
    VALUES
        -- Pool Standard weekday
        (1, 1, 1, 1, '08:00', '17:00', 50000.0000, 30, 15, 1, SYSUTCDATETIME()),
        (2, 1, 1, 1, '17:00', '23:59', 70000.0000, 30, 15, 1, SYSUTCDATETIME()),
        -- Pool VIP weekday
        (3, 1, 2, 1, '08:00', '17:00', 90000.0000, 30, 15, 1, SYSUTCDATETIME()),
        (4, 1, 2, 1, '17:00', '23:59', 120000.0000, 30, 15, 1, SYSUTCDATETIME()),
        -- Carom weekday
        (5, 1, 3, 1, '08:00', '17:00', 60000.0000, 30, 15, 1, SYSUTCDATETIME()),
        (6, 1, 3, 1, '17:00', '23:59', 80000.0000, 30, 15, 1, SYSUTCDATETIME()),
        -- Weekend sample
        (7, 1, 1, 6, '08:00', '23:59', 80000.0000, 30, 15, 1, SYSUTCDATETIME()),
        (8, 1, 2, 6, '08:00', '23:59', 140000.0000, 30, 15, 1, SYSUTCDATETIME()),
        (9, 1, 3, 6, '08:00', '23:59', 90000.0000, 30, 15, 1, SYSUTCDATETIME());

    SET IDENTITY_INSERT pricing_plan_rules OFF;


    ------------------------------------------------------------
    -- 12. bookings
    -- status: 1 Pending, 2 Confirmed, 3 Cancelled, 4 Completed, 5 NoShow
    ------------------------------------------------------------
    SET IDENTITY_INSERT bookings ON;

    INSERT INTO bookings (
        booking_id, public_id, customer_id, table_id, table_type_id, booking_code,
        start_time_utc, end_time_utc, number_of_guests, status, note,
        confirmed_by_user_id, confirmed_at_utc, cancelled_at_utc,
        created_at_utc, updated_at_utc
    )
    VALUES
        (1, '20000000-0000-0000-0000-000000000001', 1, 4, 2, N'BK-20260524-0001',
         '2026-05-24T12:00:00', '2026-05-24T14:00:00', 4, 2, N'Khách đặt bàn VIP',
         3, SYSUTCDATETIME(), NULL, SYSUTCDATETIME(), NULL),

        (2, '20000000-0000-0000-0000-000000000002', 2, NULL, 1, N'BK-20260524-0002',
         '2026-05-24T15:00:00', '2026-05-24T17:00:00', 3, 1, N'Khách chỉ chọn loại bàn Pool Standard',
         NULL, NULL, NULL, SYSUTCDATETIME(), NULL);

    SET IDENTITY_INSERT bookings OFF;


    ------------------------------------------------------------
    -- 13. sessions
    -- status: 1 Open, 2 Closed, 3 Cancelled
    ------------------------------------------------------------
    SET IDENTITY_INSERT sessions ON;

    INSERT INTO sessions (
        session_id, public_id, session_code, customer_id, booking_id,
        status, started_at_utc, ended_at_utc,
        opened_by_user_id, closed_by_user_id, note,
        created_at_utc, updated_at_utc
    )
    VALUES
        (1, '30000000-0000-0000-0000-000000000001', N'SS-20260524-0001', 1, 1,
         2, '2026-05-24T12:05:00', '2026-05-24T14:05:00',
         3, 3, N'Phiên chơi từ booking VIP', SYSUTCDATETIME(), NULL),

        (2, '30000000-0000-0000-0000-000000000002', N'SS-20260524-0002', 3, NULL,
         1, '2026-05-24T13:30:00', NULL,
         3, NULL, N'Khách vãng lai đang chơi', SYSUTCDATETIME(), NULL);

    SET IDENTITY_INSERT sessions OFF;


    ------------------------------------------------------------
    -- 14. session_table_assignments
    ------------------------------------------------------------
    SET IDENTITY_INSERT session_table_assignments ON;

    INSERT INTO session_table_assignments (
        session_table_assignment_id, session_id, table_id, pricing_plan_rule_id,
        started_at_utc, ended_at_utc, duration_minutes,
        hourly_rate_snapshot, amount, assigned_by_user_id, note, created_at_utc
    )
    VALUES
        (1, 1, 4, 8, '2026-05-24T12:05:00', '2026-05-24T14:05:00', 120,
         140000.0000, 280000.0000, 3, N'Sử dụng bàn VIP V01', SYSUTCDATETIME()),

        (2, 2, 1, 7, '2026-05-24T13:30:00', NULL, NULL,
         80000.0000, NULL, 3, N'Phiên đang mở bàn A01', SYSUTCDATETIME());

    SET IDENTITY_INSERT session_table_assignments OFF;


    ------------------------------------------------------------
    -- 15. product_categories
    ------------------------------------------------------------
    SET IDENTITY_INSERT product_categories ON;

    INSERT INTO product_categories (
        product_category_id, name, description, display_order, is_active, created_at_utc
    )
    VALUES
        (1, N'Nước giải khát', N'Soft drinks and bottled drinks', 1, 1, SYSUTCDATETIME()),
        (2, N'Đồ ăn nhẹ', N'Snacks and quick food', 2, 1, SYSUTCDATETIME()),
        (3, N'Dịch vụ khác', N'Other services', 3, 1, SYSUTCDATETIME());

    SET IDENTITY_INSERT product_categories OFF;


    ------------------------------------------------------------
    -- 16. products
    ------------------------------------------------------------
    SET IDENTITY_INSERT products ON;

    INSERT INTO products (
        product_id, public_id, product_category_id, sku, name, description, image_url,
        unit_price, stock_quantity, low_stock_threshold,
        is_stock_tracked, is_active, created_at_utc, updated_at_utc
    )
    VALUES
        (1, '40000000-0000-0000-0000-000000000001', 1, N'DRINK-COCA', N'Coca Cola', N'Coca lon 330ml', NULL,
         15000.0000, 100, 20, 1, 1, SYSUTCDATETIME(), NULL),

        (2, '40000000-0000-0000-0000-000000000002', 1, N'DRINK-WATER', N'Nước suối', N'Chai nước suối 500ml', NULL,
         10000.0000, 120, 30, 1, 1, SYSUTCDATETIME(), NULL),

        (3, '40000000-0000-0000-0000-000000000003', 2, N'SNACK-CHIPS', N'Snack khoai tây', N'Gói snack khoai tây', NULL,
         20000.0000, 50, 10, 1, 1, SYSUTCDATETIME(), NULL),

        (4, '40000000-0000-0000-0000-000000000004', 3, N'SERVICE-GLOVE', N'Thuê găng tay', N'Dịch vụ thuê găng tay chơi bi-a', NULL,
         10000.0000, 0, NULL, 0, 1, SYSUTCDATETIME(), NULL);

    SET IDENTITY_INSERT products OFF;


    ------------------------------------------------------------
    -- 17. inventory_transactions
    -- transaction_type: 1 Import, 2 Export, 3 Adjustment, 4 Sale, 5 CancelSale
    ------------------------------------------------------------
    SET IDENTITY_INSERT inventory_transactions ON;

    INSERT INTO inventory_transactions (
        inventory_transaction_id, product_id, transaction_type, quantity, unit_cost,
        reference_type, reference_id, note, created_by_user_id, created_at_utc
    )
    VALUES
        (1, 1, 1, 100, 10000.0000, N'MANUAL', NULL, N'Nhập kho ban đầu Coca Cola', 2, SYSUTCDATETIME()),
        (2, 2, 1, 120, 6000.0000,  N'MANUAL', NULL, N'Nhập kho ban đầu nước suối', 2, SYSUTCDATETIME()),
        (3, 3, 1, 50,  12000.0000, N'MANUAL', NULL, N'Nhập kho ban đầu snack', 2, SYSUTCDATETIME());

    SET IDENTITY_INSERT inventory_transactions OFF;


    ------------------------------------------------------------
    -- 18. orders
    -- status: 1 Pending, 2 Confirmed, 3 Served, 4 Cancelled
    ------------------------------------------------------------
    SET IDENTITY_INSERT orders ON;

    INSERT INTO orders (
        order_id, public_id, order_code, session_id, ordered_by_user_id,
        status, subtotal_amount, note, created_at_utc, updated_at_utc
    )
    VALUES
        (1, '50000000-0000-0000-0000-000000000001', N'OD-20260524-0001', 1, 3,
         3, 60000.0000, N'Order cho phiên VIP', SYSUTCDATETIME(), NULL),

        (2, '50000000-0000-0000-0000-000000000002', N'OD-20260524-0002', 2, 3,
         2, 30000.0000, N'Order cho phiên đang mở', SYSUTCDATETIME(), NULL);

    SET IDENTITY_INSERT orders OFF;


    ------------------------------------------------------------
    -- 19. order_items
    ------------------------------------------------------------
    SET IDENTITY_INSERT order_items ON;

    INSERT INTO order_items (
        order_item_id, order_id, product_id, product_name_snapshot,
        unit_price_snapshot, quantity, line_total_amount, note, created_at_utc
    )
    VALUES
        (1, 1, 1, N'Coca Cola', 15000.0000, 2, 30000.0000, NULL, SYSUTCDATETIME()),
        (2, 1, 3, N'Snack khoai tây', 20000.0000, 1, 20000.0000, NULL, SYSUTCDATETIME()),
        (3, 1, 4, N'Thuê găng tay', 10000.0000, 1, 10000.0000, NULL, SYSUTCDATETIME()),
        (4, 2, 1, N'Coca Cola', 15000.0000, 2, 30000.0000, NULL, SYSUTCDATETIME());

    SET IDENTITY_INSERT order_items OFF;


    ------------------------------------------------------------
    -- Additional inventory transactions for sales
    ------------------------------------------------------------
    SET IDENTITY_INSERT inventory_transactions ON;

    INSERT INTO inventory_transactions (
        inventory_transaction_id, product_id, transaction_type, quantity, unit_cost,
        reference_type, reference_id, note, created_by_user_id, created_at_utc
    )
    VALUES
        (4, 1, 4, -2, NULL, N'ORDER', 1, N'Bán Coca cho order 1', 3, SYSUTCDATETIME()),
        (5, 3, 4, -1, NULL, N'ORDER', 1, N'Bán snack cho order 1', 3, SYSUTCDATETIME()),
        (6, 1, 4, -2, NULL, N'ORDER', 2, N'Bán Coca cho order 2', 3, SYSUTCDATETIME());

    SET IDENTITY_INSERT inventory_transactions OFF;


    ------------------------------------------------------------
    -- 20. discounts
    -- discount_type: PERCENTAGE, FIXED_AMOUNT
    -- applies_to: TIME only
    ------------------------------------------------------------
    SET IDENTITY_INSERT discounts ON;

    INSERT INTO discounts (
        discount_id, discount_code, name, discount_type, value,
        max_amount, min_time_subtotal, applies_to,
        starts_at_utc, ends_at_utc, is_active, created_at_utc, updated_at_utc
    )
    VALUES
        (1, N'TIME20', N'Giảm 20% tiền giờ', N'PERCENTAGE', 20.0000,
         50000.0000, 100000.0000, N'TIME',
         '2026-01-01T00:00:00', '2026-12-31T23:59:59', 1, SYSUTCDATETIME(), NULL),

        (2, N'TIME50K', N'Giảm 50.000 tiền giờ', N'FIXED_AMOUNT', 50000.0000,
         NULL, 200000.0000, N'TIME',
         '2026-01-01T00:00:00', '2026-12-31T23:59:59', 1, SYSUTCDATETIME(), NULL);

    SET IDENTITY_INSERT discounts OFF;


    ------------------------------------------------------------
    -- 21. invoices
    -- payment_status: 1 Unpaid, 2 PartiallyPaid, 3 Paid, 4 Refunded
    -- status: 1 Draft, 2 Issued, 3 Cancelled
    ------------------------------------------------------------
    SET IDENTITY_INSERT invoices ON;

    INSERT INTO invoices (
        invoice_id, public_id, invoice_code, session_id, customer_id,
        time_subtotal_amount, product_subtotal_amount, subtotal_amount,
        discount_amount, tax_amount, grand_total_amount, paid_amount,
        payment_status, status, issued_by_user_id, issued_at_utc, note,
        created_at_utc, updated_at_utc
    )
    VALUES
        (1, '60000000-0000-0000-0000-000000000001', N'INV-20260524-0001', 1, 1,
         280000.0000, 60000.0000, 340000.0000,
         50000.0000, 0.0000, 290000.0000, 290000.0000,
         3, 2, 4, '2026-05-24T14:10:00', N'Hóa đơn phiên VIP đã thanh toán',
         SYSUTCDATETIME(), NULL);

    SET IDENTITY_INSERT invoices OFF;


    ------------------------------------------------------------
    -- 22. invoice_lines
    -- line_type: TIME_CHARGE, PRODUCT
    ------------------------------------------------------------
    SET IDENTITY_INSERT invoice_lines ON;

    INSERT INTO invoice_lines (
        invoice_line_id, invoice_id, line_type, reference_id,
        description, quantity, unit_price, line_total_amount, created_at_utc
    )
    VALUES
        (1, 1, N'TIME_CHARGE', 1, N'Tiền bàn VIP V01 từ 12:05 đến 14:05', 2.0000, 140000.0000, 280000.0000, SYSUTCDATETIME()),
        (2, 1, N'PRODUCT', 1, N'Coca Cola x2', 2.0000, 15000.0000, 30000.0000, SYSUTCDATETIME()),
        (3, 1, N'PRODUCT', 2, N'Snack khoai tây x1', 1.0000, 20000.0000, 20000.0000, SYSUTCDATETIME()),
        (4, 1, N'PRODUCT', 3, N'Thuê găng tay x1', 1.0000, 10000.0000, 10000.0000, SYSUTCDATETIME());

    SET IDENTITY_INSERT invoice_lines OFF;


    ------------------------------------------------------------
    -- 23. invoice_discounts
    ------------------------------------------------------------
    SET IDENTITY_INSERT invoice_discounts ON;

    INSERT INTO invoice_discounts (
        invoice_discount_id, invoice_id, discount_id, applied_by_user_id,
        amount_applied, description_snapshot, created_at_utc
    )
    VALUES
        (1, 1, 2, 4, 50000.0000, N'TIME50K - Giảm 50.000 tiền giờ', SYSUTCDATETIME());

    SET IDENTITY_INSERT invoice_discounts OFF;


    ------------------------------------------------------------
    -- 24. payment_methods
    ------------------------------------------------------------
    SET IDENTITY_INSERT payment_methods ON;

    INSERT INTO payment_methods (
        payment_method_id, name, code, description, is_active, created_at_utc
    )
    VALUES
        (1, N'Tiền mặt', N'CASH', N'Thanh toán bằng tiền mặt', 1, SYSUTCDATETIME()),
        (2, N'Chuyển khoản', N'BANK_TRANSFER', N'Thanh toán chuyển khoản ngân hàng', 1, SYSUTCDATETIME()),
        (3, N'MoMo', N'MOMO', N'Thanh toán ví MoMo mock', 1, SYSUTCDATETIME()),
        (4, N'VNPay Mock', N'VNPAY_MOCK', N'Thanh toán VNPay mock', 1, SYSUTCDATETIME()),
        (5, N'Thẻ', N'CARD', N'Thanh toán bằng thẻ', 1, SYSUTCDATETIME());

    SET IDENTITY_INSERT payment_methods OFF;


    ------------------------------------------------------------
    -- 25. payments
    -- payment_status: 1 Pending, 2 Completed, 3 Failed, 4 Refunded
    ------------------------------------------------------------
    SET IDENTITY_INSERT payments ON;

    INSERT INTO payments (
        payment_id, public_id, invoice_id, payment_method_id, amount,
        payment_status, transaction_code, paid_at_utc,
        received_by_user_id, note, created_at_utc
    )
    VALUES
        (1, '70000000-0000-0000-0000-000000000001', 1, 1, 290000.0000,
         2, N'CASH-20260524-0001', '2026-05-24T14:12:00',
         4, N'Thanh toán tiền mặt đủ hóa đơn', SYSUTCDATETIME());

    SET IDENTITY_INSERT payments OFF;


    ------------------------------------------------------------
    -- 26. notifications
    ------------------------------------------------------------
    SET IDENTITY_INSERT notifications ON;

    INSERT INTO notifications (
        notification_id, user_id, customer_id, title, message,
        notification_type, is_read, read_at_utc, created_at_utc
    )
    VALUES
        (1, 3, NULL, N'Booking mới', N'Có booking mới cần xác nhận.', N'BOOKING', 0, NULL, SYSUTCDATETIME()),
        (2, 4, NULL, N'Hóa đơn đã tạo', N'Hóa đơn INV-20260524-0001 đã được tạo.', N'INVOICE', 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (3, NULL, 1, N'Xác nhận đặt bàn', N'Booking BK-20260524-0001 đã được xác nhận.', N'BOOKING_CONFIRMATION', 0, NULL, SYSUTCDATETIME());

    SET IDENTITY_INSERT notifications OFF;


    ------------------------------------------------------------
    -- 27. audit_logs
    ------------------------------------------------------------
    SET IDENTITY_INSERT audit_logs ON;

    INSERT INTO audit_logs (
        audit_log_id, actor_user_id, action, entity_name, entity_id, entity_public_id,
        old_values, new_values, ip_address, user_agent, description, created_at_utc
    )
    VALUES
        (1, 1, N'CREATE_USER', N'users', 3, '33333333-3333-3333-3333-333333333333',
         NULL,
         N'{"email":"staff@poolhub.local","fullName":"Trần Nhân Viên"}',
         N'127.0.0.1', N'SeedScript', N'Admin created demo staff account', SYSUTCDATETIME()),

        (2, 3, N'CONFIRM_BOOKING', N'bookings', 1, '20000000-0000-0000-0000-000000000001',
         N'{"status":1}',
         N'{"status":2,"confirmedByUserId":3}',
         N'127.0.0.1', N'SeedScript', N'Staff confirmed booking BK-20260524-0001', SYSUTCDATETIME()),

        (3, 3, N'OPEN_SESSION', N'sessions', 1, '30000000-0000-0000-0000-000000000001',
         NULL,
         N'{"status":1,"tableId":4,"startedAtUtc":"2026-05-24T12:05:00"}',
         N'127.0.0.1', N'SeedScript', N'Staff opened session SS-20260524-0001', SYSUTCDATETIME()),

        (4, 3, N'CLOSE_SESSION', N'sessions', 1, '30000000-0000-0000-0000-000000000001',
         N'{"status":1}',
         N'{"status":2,"endedAtUtc":"2026-05-24T14:05:00"}',
         N'127.0.0.1', N'SeedScript', N'Staff closed session SS-20260524-0001', SYSUTCDATETIME()),

        (5, 4, N'ISSUE_INVOICE', N'invoices', 1, '60000000-0000-0000-0000-000000000001',
         NULL,
         N'{"invoiceCode":"INV-20260524-0001","grandTotalAmount":290000}',
         N'127.0.0.1', N'SeedScript', N'Staff issued invoice INV-20260524-0001', SYSUTCDATETIME()),

        (6, 4, N'RECEIVE_PAYMENT', N'payments', 1, '70000000-0000-0000-0000-000000000001',
         NULL,
         N'{"amount":290000,"paymentMethod":"CASH"}',
         N'127.0.0.1', N'SeedScript', N'Staff received payment for invoice INV-20260524-0001', SYSUTCDATETIME());

    SET IDENTITY_INSERT audit_logs OFF;


    COMMIT TRANSACTION;

    PRINT N'PoolHub seed data inserted successfully.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    PRINT N'PoolHub seed data insert failed.';
    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
