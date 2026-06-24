# PoolHub Coding Conventions and Workflow

## 1. Quy chuẩn code
- Ưu tiên layered architecture và controller mỏng.
- Dùng async/await cho I/O.
- Dùng DTO ở boundary, tránh expose entity thẳng ra controller.
- Dùng `ApiResponse<T>` cho response nhất quán.
- Không hard-code logic quyền trong UI; dựa vào API và claims/roles.
- Tên file, class và service phải phản ánh nghiệp vụ rõ ràng.

## 2. Quy tắc dữ liệu và bảo mật
- Mật khẩu phải hash.
- Token không được log ra console hoặc audit raw.
- Tác vụ nhiều bước phải dùng transaction nếu có rủi ro trạng thái dở dang.
- Mutation quan trọng cần audit.

## 3. Quy trình làm việc hiện tại
1. Đọc source of truth trước: swagger, seed SQL, docs/ai, rồi mới tới code.
2. Đối chiếu controller thật trước khi thay đổi frontend service.
3. Nếu thêm contract mới, cập nhật cả backend DTO/controller và frontend type/service.
4. Sau khi sửa code, chạy validation phù hợp cho phần đã chạm.

## 4. Testing guidance
- Unit test cho business rule quan trọng.
- Integration test cho auth, booking, session và invoice.
- Nếu thay đổi API contract, cần test lại frontend mapping.

## 5. Workflow review/deploy
- Pull request nên nêu rõ assumptions nếu có lệch giữa tài liệu cũ và code.
- Không merge khi còn TODO ở luồng auth/tiền/hóa đơn.
- Khi deploy, phải có bước xác nhận DB migration và cấu hình environment.