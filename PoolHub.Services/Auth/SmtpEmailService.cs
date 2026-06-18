using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PoolHub.Core.DTOs.Auth;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Auth;

public class SmtpEmailService(
    IOptions<EmailSettings> options,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly EmailSettings _settings = options.Value;

    public void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost) ||
            _settings.SmtpPort is <= 0 or > 65535 ||
            string.IsNullOrWhiteSpace(_settings.FromEmail) ||
            !Uri.TryCreate(_settings.FrontendBaseUrl, UriKind.Absolute, out _))
        {
            throw new ServiceUnavailableException("Password recovery email service is not configured.");
        }
    }

    public async Task SendPasswordResetAsync(
        string email,
        string resetToken,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var resetUrl = BuildResetUrl(email, resetToken);
        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName, Encoding.UTF8),
            Subject = "Đặt lại mật khẩu PoolHub",
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true,
            Body = BuildHtmlBody(resetUrl)
        };
        message.To.Add(new MailAddress(email));

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = _settings.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Timeout = 30_000
        };
        if (!string.IsNullOrWhiteSpace(_settings.Username))
        {
            client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
        }

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            logger.LogInformation("Password reset email sent to {EmailDomain}", GetEmailDomain(email));
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException)
        {
            logger.LogError(ex, "Failed to send password reset email to domain {EmailDomain}", GetEmailDomain(email));
            throw new ServiceUnavailableException("Password recovery email could not be sent. Please try again later.");
        }
    }

    private string BuildResetUrl(string email, string resetToken)
    {
        var baseUrl = _settings.FrontendBaseUrl.TrimEnd('/');
        return $"{baseUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(resetToken)}";
    }

    private string BuildHtmlBody(string resetUrl)
    {
        var encodedUrl = HtmlEncoder.Default.Encode(resetUrl);
        var minutes = Math.Clamp(_settings.PasswordResetExpirationMinutes, 5, 120);
        return $$"""
            <!doctype html>
            <html lang="vi">
            <body style="margin:0;background:#f4f8f6;font-family:Arial,sans-serif;color:#10201c">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0">
                <tr><td align="center" style="padding:32px 16px">
                  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:560px;background:#fff;border:1px solid #dce7e2;border-radius:12px">
                    <tr><td style="padding:28px">
                      <div style="font-size:22px;font-weight:800;color:#0f5d4b">PoolHub</div>
                      <h1 style="font-size:24px;margin:24px 0 12px">Đặt lại mật khẩu</h1>
                      <p style="line-height:1.6;color:#60746d">Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
                      <p style="margin:28px 0">
                        <a href="{{encodedUrl}}" style="display:inline-block;padding:12px 18px;background:#0f5d4b;color:#fff;text-decoration:none;border-radius:8px;font-weight:700">Đặt lại mật khẩu</a>
                      </p>
                      <p style="line-height:1.6;color:#60746d">Liên kết có hiệu lực trong {{minutes}} phút và chỉ sử dụng được một lần.</p>
                      <p style="line-height:1.6;color:#60746d">Nếu bạn không yêu cầu thao tác này, hãy bỏ qua email.</p>
                      <hr style="border:0;border-top:1px solid #dce7e2;margin:24px 0">
                      <p style="font-size:12px;color:#60746d;word-break:break-all">Nếu nút không hoạt động, mở liên kết: {{encodedUrl}}</p>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string GetEmailDomain(string email)
    {
        var at = email.LastIndexOf('@');
        return at >= 0 ? email[(at + 1)..] : "unknown";
    }
}
