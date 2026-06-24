# PoolHub AI Context Hub

Tài liệu này là điểm vào nhanh cho AI và thành viên mới trong nhóm. Mọi tài liệu bên dưới được viết theo trạng thái hiện tại của repo, có đối chiếu với code backend, frontend, seed SQL và tài liệu thiết kế sẵn có.

## Bộ tài liệu
- [Tổng quan sản phẩm](docs/ai/PROJECT_CONTEXT_OVERVIEW.md)
- [Kiến trúc hệ thống](docs/ai/SYSTEM_ARCHITECTURE.md)
- [Đặc tả kỹ thuật](docs/ai/TECHNICAL_SPECIFICATION.md)
- [API documentation](docs/ai/API_DOCUMENTATION.md)
- [User flows](docs/ai/USER_FLOWS.md)
- [Tiến độ và roadmap](docs/ai/ROADMAP_AND_PROGRESS.md)
- [Backlog, blockers và rủi ro](docs/ai/BACKLOG_BLOCKERS.md)
- [Environment setup](docs/ai/ENVIRONMENT_SETUP.md)
- [Coding conventions và workflow](docs/ai/CODING_CONVENTIONS_WORKFLOW.md)

## Source of truth
Ưu tiên đối chiếu theo thứ tự:
1. `swagger_poolhub.yaml`
2. `poolhub_seed_data_27_tables.sql`
3. Tài liệu `docs/ai/*` và các bản phân tích đã có trong repo
4. Code hiện tại trong `PoolHub.API`, `PoolHub.Services`, `PoolHub.Infrastructure`, `PoolHub.Core`, `frontend`

## Lưu ý hiện trạng
- Backend đã có triển khai thực tế, không còn là skeleton thuần.
- API, DTO và frontend service đang có một số khác biệt nhỏ giữa contract legacy và code hiện tại, nên khi phát triển mới cần kiểm tra controller thật trước.
- Tài liệu tiến độ bên dưới là snapshot hiện tại của repo, không phải roadmap cam kết tương lai.