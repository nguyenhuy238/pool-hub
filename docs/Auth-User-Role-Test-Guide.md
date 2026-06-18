# PoolHub Auth/User/Role API Test Guide

Base URL: `http://localhost:5056`

Swagger: `http://localhost:5056/swagger`

Default demo account:

- Email: `admin@poolhub.com`
- Password: `Admin@123`
- Role: `Admin`

For protected endpoints, add:

```http
Authorization: Bearer <accessToken>
```

## Recommended test order

1. `POST /api/auth/login` — public — expect `200`

```json
{
  "email": "admin@poolhub.com",
  "password": "Admin@123"
}
```

2. `GET /api/auth/me` — authenticated — expect `200`

3. `POST /api/auth/refresh-token` — public — expect `200`; the old refresh token must then return `401`

```json
{
  "refreshToken": "<refreshToken>"
}
```

4. `POST /api/auth/logout` — authenticated — expect `200`; the logged-out refresh token must then return `401`

5. `POST /api/auth/forgot-password` — public — expect `200` with the same generic message for known and unknown emails

```json
{
  "email": "user@example.com"
}
```

6. `POST /api/auth/reset-password` — public — expect `200`

```json
{
  "email": "user@example.com",
  "token": "<token-from-development-log>",
  "newPassword": "NewPassword@123",
  "confirmPassword": "NewPassword@123"
}
```

7. `POST /api/users` — Admin — expect `201`

```json
{
  "fullName": "Nguyen Van A",
  "email": "a@example.com",
  "password": "Password@123",
  "confirmPassword": "Password@123",
  "phoneNumber": "0900000000",
  "roleIds": [3]
}
```

8. `GET /api/users?pageNumber=1&pageSize=10&keyword=a&status=Active&roleId=3` — Admin/Manager — expect `200`

9. `GET /api/users/{id}` — Admin/Manager — expect `200`

10. `PUT /api/users/{id}` — Admin/Manager — expect `200`

```json
{
  "fullName": "Nguyen Van A Updated",
  "phoneNumber": "0900000001",
  "avatarUrl": null,
  "emailConfirmed": true
}
```

11. `PATCH /api/users/{id}/status` — Admin — expect `200`

```json
{
  "status": "Locked"
}
```

Use `Active` to unlock and `Deleted` for soft delete.

12. `POST /api/roles` — Admin — expect `201`

```json
{
  "name": "Supervisor",
  "description": "Operations supervisor",
  "isSystem": false
}
```

13. `POST /api/users/{id}/roles` — Admin — expect `200`

```json
{
  "roleIds": [2, 3]
}
```

14. `DELETE /api/users/{id}/roles/{roleId}` — Admin — expect `200`

15. `PUT /api/roles/{id}` — Admin — expect `200`

16. `DELETE /api/roles/{id}` — Admin — expect `200` only for non-system roles that are not assigned

17. `GET /api/audit-logs?pageNumber=1&pageSize=20` — Admin/Manager — expect `200`

## Expected authorization behavior

- Missing or invalid access token: `401` with `ApiResponse`.
- Authenticated user without the required role: `403` with `ApiResponse`.
- Locked/deleted user login: `423`.
- Duplicate email or role name: `409`.
- Validation errors: `400`.

Password reset email is mocked. In Development, the reset URL is written to the API log. Configure a real SMTP/provider implementation before production.
