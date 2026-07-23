# AI_DEVELOPMENT_RULES - PoolHub

## A. Rule Ưu Tiên Nguồn Sự Thật
1. Khi có mâu thuẫn, ưu tiên theo thứ tự:
- `swagger_poolhub.yaml`
- ERD/SQL (`poolhub_seed_data_27_tables.sql`, tài liệu ERD PDF/DOCX)
- Tài liệu đề xuất/tuần
- Code hiện tại
2. Không tự ý đổi tên endpoint/table/field nếu chưa cập nhật đồng bộ source of truth.

## B. Rule Thiết Kế API
1. Bám đúng contract OpenAPI: path, method, request/response shape, status code.
2. Response phải thống nhất envelope (`success`, `message`, `data`, `errors` nếu có).
3. Validate input ở boundary (DTO/request model), không để business rule rơi xuống controller.
4. Bắt buộc xử lý phân quyền theo role (Admin/Manager/Staff).

## C. Rule Data & Transaction
1. Không hard-code business enum; tạo enum/constants rõ nghĩa và mapping DB tường minh.
2. Tác vụ nhiều bước (session end, invoice finalize, payment) phải có transaction.
3. Lưu audit cho thao tác nhạy cảm: auth, giá, hóa đơn, hoàn/hủy.
4. Soft-delete hoặc status-driven lifecycle cho entity nghiệp vụ chính.

## D. Rule Security
1. Dùng JWT access token + refresh token theo tài liệu.
2. Password phải hash mạnh (BCrypt/Argon2), không lưu plain text.
3. Không log secret/token/raw password.
4. Bảo vệ endpoint nhạy cảm bằng policy-based authorization.

## E. Rule Kiến Trúc .NET
1. Tách lớp tối thiểu: API -> Application -> Infrastructure -> Domain.
2. Controller mỏng, logic ở service/use-case.
3. Tất cả dependency qua DI, không new trực tiếp service/repository trong controller.
4. Dùng async/await cho I/O bound operations.

## F. Rule Chất Lượng
1. Mỗi feature phải có:
- Unit test cho business rules chính.
- Integration test cho endpoint quan trọng.
2. Không merge code còn TODO blocker cho luồng tiền/hoá đơn/auth.
3. Update docs khi thay đổi contract/schema.

## G. Rule Làm Việc Của AI Agent
1. Trước khi code, AI phải đọc:
- `docs/ai/SYSTEM_CONTEXT.md`
- `docs/ai/DOC_SOURCE_MAP.md`
- `swagger_poolhub.yaml`
2. Khi tạo file mới, đặt tên rõ nghiệp vụ, tránh tên chung chung.
3. Khi giả định nghiệp vụ chưa rõ, thêm section `Assumptions` trong PR/commit message.
4. Nếu phát hiện mâu thuẫn giữa tài liệu, AI phải ghi rõ conflict và đề xuất hướng chuẩn hóa.
