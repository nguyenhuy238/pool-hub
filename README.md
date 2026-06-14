# PoolHub - Entertainment Center Management Backend

## Overview
Backend cho hệ thống quản lý trung tâm giải trí/bida PoolHub (checkpoint Week 4), gồm Auth JWT, phân quyền role-based, core domain APIs và EF Core schema 27 bảng.

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

### JwtSettings
`PoolHub.API/appsettings.json`
- `SecretKey`
- `Issuer = PoolHub.API`
- `Audience = PoolHub.Client`
- `ExpirationHours = 8`

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
