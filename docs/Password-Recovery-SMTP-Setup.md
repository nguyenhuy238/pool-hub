# PoolHub Password Recovery SMTP Setup

PoolHub sends password reset links through SMTP. Reset tokens are stored only as SHA-256 hashes and are never written to logs.

## Required environment variables

Use environment variables, deployment secrets, or ASP.NET Core user-secrets. Do not commit SMTP credentials.

```text
EmailSettings__SmtpHost=smtp.gmail.com
EmailSettings__SmtpPort=587
EmailSettings__UseSsl=true
EmailSettings__Username=your-account@gmail.com
EmailSettings__Password=your-app-password
EmailSettings__FromEmail=your-account@gmail.com
EmailSettings__FromName=PoolHub
EmailSettings__FrontendBaseUrl=https://your-poolhub-domain.com
EmailSettings__PasswordResetExpirationMinutes=30
```

For local development:

```powershell
dotnet user-secrets init --project PoolHub.API
dotnet user-secrets set "EmailSettings:SmtpHost" "smtp.gmail.com" --project PoolHub.API
dotnet user-secrets set "EmailSettings:SmtpPort" "587" --project PoolHub.API
dotnet user-secrets set "EmailSettings:UseSsl" "true" --project PoolHub.API
dotnet user-secrets set "EmailSettings:Username" "your-account@gmail.com" --project PoolHub.API
dotnet user-secrets set "EmailSettings:Password" "your-app-password" --project PoolHub.API
dotnet user-secrets set "EmailSettings:FromEmail" "your-account@gmail.com" --project PoolHub.API
dotnet user-secrets set "EmailSettings:FromName" "PoolHub" --project PoolHub.API
dotnet user-secrets set "EmailSettings:FrontendBaseUrl" "http://localhost:3000" --project PoolHub.API
```

Gmail requires two-step verification and an App Password. Do not use the normal account password.

Other SMTP providers such as Microsoft 365, SendGrid SMTP, Mailgun, Amazon SES, or Postmark can be configured with the provider's host, port, username, and password.

## Behavior

- `POST /api/auth/forgot-password` always returns the same message for known and unknown emails.
- The endpoint is limited to five requests per minute per IP.
- A user can receive at most one reset email per minute.
- The reset link expires after the configured period and can only be used once.
- Existing refresh tokens are revoked after a successful password reset.
- SMTP delivery failures are logged without recipient address, token, or reset URL.
- If SMTP is not configured, the endpoint returns `503 Service Unavailable` for every email address.

## Production requirements

- Set `EmailSettings__FrontendBaseUrl` to the HTTPS frontend origin.
- Configure SPF, DKIM, and DMARC for the sender domain.
- Use a dedicated transactional email account/API credential.
- Monitor `AUTH_FORGOT_PASSWORD_EMAIL_FAILED` audit events and application error logs.
