# PoolHub - Entertainment Center Management Backend

## Overview
Backend cho hệ thống quản lý trung tâm giải trí/bida PoolHub (checkpoint Week 4), gồm Auth JWT, phân quyền role-based, core domain APIs và EF Core schema 27 bảng.

## Documentation Index
Tài liệu ngữ cảnh đầy đủ cho AI và người mới được gom tại [docs/ai/AI_CONTEXT_HUB.md](docs/ai/AI_CONTEXT_HUB.md).

- [Tổng quan sản phẩm](docs/ai/PROJECT_CONTEXT_OVERVIEW.md)
- [Kiến trúc hệ thống](docs/ai/SYSTEM_ARCHITECTURE.md)
- [Đặc tả kỹ thuật](docs/ai/TECHNICAL_SPECIFICATION.md)
- [API documentation](docs/ai/API_DOCUMENTATION.md)
- [User flows](docs/ai/USER_FLOWS.md)
- [Tiến độ và roadmap](docs/ai/ROADMAP_AND_PROGRESS.md)
- [Backlog, blockers và rủi ro](docs/ai/BACKLOG_BLOCKERS.md)
- [Environment setup](docs/ai/ENVIRONMENT_SETUP.md)
- [Coding conventions và workflow](docs/ai/CODING_CONVENTIONS_WORKFLOW.md)

## Tech Stack
- ASP.NET Core Web API (.NET 10 SDK)
- Entity Framework Core + SQL Server
- JWT Bearer Authentication
- BCrypt password hashing (work factor 12)
- Swagger/OpenAPI
- Layered architecture: API/Core/Infrastructure/Services/Shared

## Solution Structure
- `PoolHub.API`: controllers, middleware, Program, Swagger, auth config
- `PoolHub.Core`: entities, DTOs, interfaces
- `PoolHub.Infrastructure`: DbContext, migrations, seed
- `PoolHub.Services`: business services
- `PoolHub.Shared`: response wrapper, exceptions, constants, extensions

## Configuration
### Connection String
`PoolHub.API/appsettings.json`

### Development Config
Copy the example config before running locally if you do not already have a personal development config:

```powershell
copy PoolHub.API\appsettings.Development.example.json PoolHub.API\appsettings.Development.json
```

Git Bash/macOS/Linux:

```bash
cp PoolHub.API/appsettings.Development.example.json PoolHub.API/appsettings.Development.json
```

If you already have `PoolHub.API/appsettings.Development.json`, do not overwrite it. Just make sure `Redis:ConnectionString` points to `127.0.0.1:6379`.

### JwtSettings
`PoolHub.API/appsettings.json`
- `SecretKey`
- `Issuer = PoolHub.API`
- `Audience = PoolHub.Client`
- `ExpirationHours = 8`

## Local Development - Redis
PoolHub uses Redis to store refresh tokens in the auth flow. If Redis is not running, login can fail or the backend can timeout while saving the refresh token.

Recommended local Redis startup:

```powershell
docker compose -f docker-compose.dev.yml up -d
```

Check Redis:

```powershell
docker exec -it poolhub-redis redis-cli ping
```

Expected result:

```text
PONG
```

Stop Redis when needed:

```powershell
docker compose -f docker-compose.dev.yml down
```

If Docker reports that the container already exists, start it:

```powershell
docker start poolhub-redis
```

Or remove the old container and recreate it:

```powershell
docker rm -f poolhub-redis
docker compose -f docker-compose.dev.yml up -d
```

Suggested local startup order:

1. Start SQL Server/local database according to your local setup.
2. Start Redis:

```powershell
docker compose -f docker-compose.dev.yml up -d
```

3. Run backend `PoolHub.API`.
4. Run frontend.

Redis does not start automatically after pulling code. Run Docker Compose once before logging in locally.

## Run Commands
1. `dotnet restore pool-hub.sln`
2. `dotnet build pool-hub.sln`
3. `dotnet ef database update --project PoolHub.Infrastructure --startup-project PoolHub.API`
4. `dotnet run --project PoolHub.API`

## Frontend
1. `cd frontend`
2. `npm install`
3. Copy `.env.example` to `.env.local` and set `NEXT_PUBLIC_API_BASE_URL`
4. `npm run build`
5. `npm run dev`

## Seed Accounts
- `admin@poolhub.com / Admin@123` (Admin)
- `manager@poolhub.com / Manager@123` (Manager)
- `staff1@poolhub.com / Staff@123` (Staff)
- `staff2@poolhub.com / Staff@123` (Staff)
- `cashier@poolhub.com / Cashier@123` (Cashier)

## Swagger Auth Test
1. Login `POST /api/auth/login`
2. Copy `accessToken`
3. Click **Authorize** trên `/swagger`
4. Paste `Bearer {accessToken}`

## Postman
- Collection: `docs/postman/PoolHub_Week4_Auth.postman_collection.json`
- Environment: `docs/postman/PoolHub_Week4_Environment.postman_environment.json`

## Main Endpoints (Week 4)
- Auth: `/api/auth/*`
- System: `GET /health`
- Users/Roles/Customers: `/api/users*`, `/api/roles`, `/api/customers`
- Venue: `/api/floors`, `/api/zones`, `/api/table-types`, `/api/venue-tables`, `/api/pricing-plans`
- Booking: `/api/bookings`
- Session: `/api/sessions/*`, including `POST /api/sessions/{id}/close`
- Product: `/api/products*`
- Order: `/api/orders*`
- Invoice/Payment: `/api/invoices*`
- Audit/Notification: `/api/audit-logs`, `/api/notifications`

## Demo Notes
- Customer management UI: `/management/customers`
- Customer phone and email are unique at service and database level.
- Pagination response exposes `items`, `pageNumber`, `pageSize`, `totalItems`, `totalPages`.
- Remaining gaps for later phases: file upload, full reports, discount CRUD, payment-method CRUD, advanced booking availability, and full audit coverage for every mutation.

## Week 1-4 Checklist
- Week 1: Domain alignment PoolHub + roles + nghiệp vụ chính
- Week 2: EF schema 27 bảng + migration + seed
- Week 3: Layered backend + service/repository style + core APIs
- Week 4: JWT auth + role authorization + user management + postman demo
