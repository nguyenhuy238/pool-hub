# PoolHub Role Permission Matrix

Updated: 2026-07-23

## Role Model

| Role | Type | Notes |
| --- | --- | --- |
| Admin | System internal | Must keep every configured permission because backend policies rely on JWT permission claims. |
| Manager | System internal | Operational management. No IAM administration and no audit-log access by default. |
| Staff | System internal | Daily operations. Cashier invoice/payment responsibilities are consolidated here. |
| Customer | System | Used by registration/login for public customer accounts; not an internal operating role. |
| Guest | System | Seeded legacy/public role; not used by current protected internal controllers. |

`Cashier` was retired. The migration `20260723000000_RemoveCashierRole` moves existing Cashier user-role assignments to Staff, deletes Cashier role permissions/user-role links, audits the migrated count, then deletes the role.

## Permission Catalog

| Key | Display Name | Module | Protected backend surfaces | Admin | Manager | Staff |
| --- | --- | --- | --- | --- | --- | --- |
| `users.manage` | Quản lý người dùng | Người dùng | `api/users` class-level read/update; mutations still require Admin role | Yes | No | No |
| `roles.manage` | Quản lý vai trò | Vai trò | `api/roles` class-level list/detail/catalog; mutations still require Admin role | Yes | No | No |
| `customers.manage` | Quản lý khách hàng | Khách hàng | `api/customers` class-level customer reads/history; selected mutations require Admin/Manager | Yes | Yes | Yes |
| `venue.manage` | Quản lý cơ sở | Cơ sở | floors, zones, table types, venue table mutations | Yes | Yes | No |
| `pricing.manage` | Quản lý bảng giá | Bảng giá | pricing plans and pricing special dates | Yes | Yes | No |
| `products.manage` | Quản lý sản phẩm | Sản phẩm | product/category mutations | Yes | Yes | No |
| `inventory.manage` | Quản lý tồn kho | Tồn kho | `api/inventory-transactions` | Yes | Yes | No |
| `discounts.manage` | Quản lý giảm giá | Giảm giá | `api/discounts` read/validate; create/update/status still Admin-only | Yes | Yes | Yes |
| `payments.manage` | Quản lý thanh toán | Thanh toán | `api/payments`, `api/payment-methods`; invoice/payment operations via invoice controller role guard | Yes | Yes | Yes |
| `landing.manage` | Cấu hình trang chủ | Trang chủ | authenticated landing settings mutations | Yes | Yes | No |
| `reports.view` | Xem báo cáo | Báo cáo | admin dashboard and reports controllers | Yes | Yes | No |
| `audit.view` | Xem nhật ký hệ thống | Bảo mật | `api/audit-logs` with Admin/Manager role gate | Yes | No | No |

## Endpoint Matrix

| Frontend route/function | API endpoint | Method | Controller/action | Current backend authorization | Final authorization |
| --- | --- | --- | --- | --- | --- |
| `/admin/roles` list/search/page | `/api/roles` | GET | `RolesController.Get` | Admin/Manager + `roles.manage` | Admin or Manager with `roles.manage`; seeded Admin only |
| `/admin/roles` detail | `/api/roles/{id}` | GET | `RolesController.GetById` | Admin/Manager + `roles.manage` | Admin or Manager with `roles.manage`; seeded Admin only |
| permission catalog | `/api/roles/permissions` | GET | `RolesController.GetPermissions` | Admin/Manager + `roles.manage` | Admin or Manager with `roles.manage`; seeded Admin only |
| create role | `/api/roles` | POST | `RolesController.Create` | Admin | Admin |
| update role | `/api/roles/{id}` | PUT | `RolesController.Update` | Admin | Admin; system role names immutable |
| delete role | `/api/roles/{id}` | DELETE | `RolesController.Delete` | Admin | Admin; system/assigned roles blocked |
| replace role permissions | `/api/roles/{id}/permissions` | PUT | `RolesController.SetPermissions` | Admin | Admin; validates permissions and keeps Admin full permission set |
| `/admin/users` list/search/filter | `/api/users` | GET | `UsersController.Get` | Admin/Manager + `users.manage` | Admin or Manager with `users.manage`; seeded Admin only |
| create user | `/api/users` | POST | `UsersController.Create` | Admin | Admin |
| assign roles | `/api/users/{id}/roles` | POST/PUT | `UsersController.AssignRoles` | Admin | Admin; retired role blocked; refresh tokens revoked |
| remove role | `/api/users/{id}/roles/{roleId}` | DELETE | `UsersController.RemoveRole` | Admin | Admin; last active Admin protected; refresh tokens revoked |
| lock/delete user | `/api/users/{id}/status` | PATCH | `UsersController.UpdateStatus` | Admin | Admin; self/last active Admin protected |
| operation floor/bookings/sessions/orders | `/api/bookings`, `/api/sessions`, `/api/orders` | mixed | operation controllers | Admin/Manager/Staff/Cashier on many actions | Admin/Manager/Staff |
| invoices and invoice payments | `/api/invoices` | mixed | `InvoicesController` | Admin/Manager/Staff/Cashier | Admin/Manager/Staff |
| payment methods/history | `/api/payment-methods`, `/api/payments` | GET/mixed | admin payment controllers | Admin/Manager/Staff/Cashier or Admin/Cashier + `payments.manage` | Admin/Manager/Staff + `payments.manage`; create/update methods remain Admin-only where coded |
| discounts | `/api/discounts` | GET/POST/PUT/PATCH | `DiscountsController` | Admin/Cashier + `discounts.manage`; mutations Admin | Admin/Manager/Staff + `discounts.manage`; mutations Admin |
| reports/dashboard | `/api/reports`, `/api/admin-dashboard` | GET | report controllers | `reports.view` policy | `reports.view`; seeded Admin/Manager |
| audit logs | `/api/audit-logs` | GET | `AuditLogsController` | Admin/Manager + `audit.view` | Admin/Manager + `audit.view`; seeded Admin only |

## Notes

- Backend policies are generated from `PermissionConstants.All` and enforced through JWT `permission` claims.
- Frontend route/menu guards now require both role and permission when both are declared, matching ASP.NET `[Authorize(Roles=..., Policy=...)]`.
- Customer and Guest remain because Customer registration uses the Customer role and there is no proof that Guest can be safely removed in this scope.
