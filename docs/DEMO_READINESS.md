# PoolHub Demo Readiness

## Local Run

Backend:

```bash
dotnet run --project PoolHub.API/PoolHub.API.csproj
```

Frontend:

```bash
cd frontend
npm install
npm run dev
```

Default local API URL for frontend and E2E:

```bash
NEXT_PUBLIC_API_BASE_URL=http://localhost:5056
```

## Demo Accounts

These accounts are created by the development database seeder and are only for local/demo environments.

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@poolhub.com` | `Admin@123` |
| Manager | `manager@poolhub.com` | `Manager@123` |
| Staff | `staff1@poolhub.com` | `Staff@123` |
| Staff | `staff2@poolhub.com` | `Staff@123` |
| Cashier | `cashier@poolhub.com` | `Cashier@123` |

## Seed Data

The seeder prepares the core demo data:

- Floors, zones, table types, and venue tables.
- Pricing plans and pricing rules.
- Customers, bookings, sessions, orders, invoices, and payments.
- Product categories, products with stock, payment methods, and TIME discounts.
- Admin, Manager, Staff, and Cashier accounts with role permissions.

## E2E Smoke Tests

Start the backend first. The E2E command starts the Next.js dev server automatically.

```bash
cd frontend
npm run test:e2e
```

Optional overrides:

```bash
NEXT_PUBLIC_API_BASE_URL=http://localhost:5056
E2E_BASE_URL=http://127.0.0.1:3000
E2E_ADMIN_EMAIL=admin@poolhub.com
E2E_ADMIN_PASSWORD=Admin@123
E2E_BROWSER_CHANNEL=msedge
```

The tests fail clearly if the backend is not reachable.

## Demo Flow

1. Login as Admin and open `/admin/dashboard`.
2. Review users, roles, and audit logs.
3. Review venue setup: floors, zones, table types, venue tables.
4. Review pricing plans and rules.
5. Login as Staff and open the floor map.
6. Start or review a session, then add an order item.
7. Login as Cashier and create/review invoice, apply discount, take payment, and print bill.
8. Login as Manager and review reports and audit logs.

## Manual Guard Checks

- Admin can access admin, management, operation, dashboard, reports, and audit.
- Manager can access management, reports, audit, and operation, but not users/roles admin screens.
- Staff can access operation routes but not admin-only or management routes.
- Cashier can access invoices/payments/discounts and operation routes, but not users/roles or management routes.
- Anonymous users can access public pages and are redirected to login for protected routes.

## Export Status

- Invoice print uses the browser print dialog with real invoice data.
- Backend PDF/Excel/CSV file export is intentionally not faked. Implement a real PDF/Excel/CSV generator before enabling file download buttons.
