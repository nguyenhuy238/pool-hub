# PoolHub Environment Setup

## 1. Prerequisites
- .NET SDK 10.x.
- Node.js 18+ hoặc bản phù hợp với Next.js 14.
- SQL Server local hoặc instance truy cập được.
- Visual Studio Code hoặc Visual Studio.

## 2. Backend setup
1. Restore solution.
2. Build `pool-hub.sln`.
3. Cấu hình connection string trong `PoolHub.API/appsettings*.json`.
4. Chạy EF migration/update database bằng project `PoolHub.Infrastructure` và startup project `PoolHub.API`.
5. Run `PoolHub.API`.

## 3. Frontend setup
1. Vào thư mục `frontend`.
2. Cài dependencies.
3. Tạo `.env.local` và đặt `NEXT_PUBLIC_API_BASE_URL`.
4. Chạy `npm run dev`.

## 4. Seed data
- Repo có seed SQL 27 bảng và seeder backend.
- Seed account demo được ghi trong README backend.
- Khi cần demo nhanh, đảm bảo seed đã chạy và tài khoản demo còn tồn tại.

## 5. Debug notes
- Nếu frontend báo 401, kiểm tra token store và refresh token rotation.
- Nếu frontend báo 404, ưu tiên đối chiếu controller thật trước khi sửa UI.
- Nếu backend lỗi DB, kiểm tra migration, connection string và seed state.

## 6. Môi trường triển khai
- Dev: local backend + local frontend + local SQL Server.
- Staging/Production: chưa có manifest triển khai chính thức trong repo, nên hiện cần cấu hình theo hạ tầng đích khi phát hành.