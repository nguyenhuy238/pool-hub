using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PoolHub.Core.DTOs.Auth;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared.Time;
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
            string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            throw new ServiceUnavailableException("Password recovery email service is not configured.");
        }
    }

    public async Task SendPasswordResetOtpAsync(
        string email,
        string otp,
        int expirationMinutes,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName, Encoding.UTF8),
            Subject = "Mã OTP đặt lại mật khẩu PoolHub",
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true,
            Body = BuildPasswordResetOtpHtml(otp, expirationMinutes)
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

    private static string BuildPasswordResetOtpHtml(string otp, int expirationMinutes)
    {
        var encodedOtp = HtmlEncoder.Default.Encode(otp);
        var minutes = Math.Clamp(expirationMinutes, 5, 120);
        return $$"""
            <!doctype html>
            <html lang="vi">
            <body style="margin:0;background:#f4f8f6;font-family:Arial,sans-serif;color:#10201c">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0">
                <tr><td align="center" style="padding:32px 16px">
                  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:560px;background:#fff;border:1px solid #dce7e2;border-radius:12px">
                    <tr><td style="padding:28px">
                      <div style="font-size:22px;font-weight:800;color:#0f5d4b">PoolHub</div>
                      <h1 style="font-size:24px;margin:24px 0 12px">Mã OTP đặt lại mật khẩu</h1>
                      <p style="line-height:1.6;color:#60746d">Nhập mã OTP sau trên trang quên mật khẩu để tạo mật khẩu mới:</p>
                      <div style="margin:28px 0;padding:18px;background:#edf7f3;border:1px solid #c7e0d7;border-radius:10px;text-align:center;font-size:34px;font-weight:800;letter-spacing:10px;color:#0f5d4b">{{encodedOtp}}</div>
                      <p style="line-height:1.6;color:#60746d">Mã có hiệu lực trong {{minutes}} phút và chỉ sử dụng được một lần.</p>
                      <p style="line-height:1.6;color:#60746d">Nếu bạn không yêu cầu thao tác này, hãy bỏ qua email.</p>
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

    public async Task SendBookingConfirmedAsync(string email, string customerName, string phoneNumber, string bookingCode,
        string tableName, DateTime startTimeUtc, DateTime endTimeUtc, int numberOfGuests, CancellationToken ct)
    {
        EnsureConfigured();
        var vnStart = TimeZoneInfo.ConvertTimeFromUtc(BusinessTime.NormalizeUtc(startTimeUtc), BusinessTime.TimeZone);
        var vnEnd = TimeZoneInfo.ConvertTimeFromUtc(BusinessTime.NormalizeUtc(endTimeUtc), BusinessTime.TimeZone);
        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName, Encoding.UTF8),
            Subject = $"✅ Đặt bàn thành công – Mã {bookingCode}",
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true,
            Body = BuildBookingConfirmedHtml(customerName, phoneNumber, bookingCode, tableName, vnStart, vnEnd, numberOfGuests)
        };
        message.To.Add(new MailAddress(email));
        using var client = BuildSmtpClient();
        try
        {
            await client.SendMailAsync(message, ct);
            logger.LogInformation("Booking confirmed email sent to {EmailDomain} for booking {BookingCode}", GetEmailDomain(email), bookingCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send booking confirmed email for {BookingCode}", bookingCode);
        }
    }

    public async Task SendBookingCancelledAsync(string email, string customerName, string phoneNumber, string bookingCode,
        string tableName, DateTime startTimeUtc, DateTime endTimeUtc, int numberOfGuests, string reason, CancellationToken ct)
    {
        EnsureConfigured();
        var vnStart = TimeZoneInfo.ConvertTimeFromUtc(BusinessTime.NormalizeUtc(startTimeUtc), BusinessTime.TimeZone);
        var vnEnd = TimeZoneInfo.ConvertTimeFromUtc(BusinessTime.NormalizeUtc(endTimeUtc), BusinessTime.TimeZone);
        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName, Encoding.UTF8),
            Subject = $"❌ Đặt bàn bị hủy – Mã {bookingCode}",
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true,
            Body = BuildBookingCancelledHtml(customerName, phoneNumber, bookingCode, tableName, vnStart, vnEnd, numberOfGuests, reason)
        };
        message.To.Add(new MailAddress(email));
        using var client = BuildSmtpClient();
        try
        {
            await client.SendMailAsync(message, ct);
            logger.LogInformation("Booking cancelled email sent to {EmailDomain} for booking {BookingCode}", GetEmailDomain(email), bookingCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send booking cancelled email for {BookingCode}", bookingCode);
        }
    }

    private SmtpClient BuildSmtpClient()
    {
        var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = _settings.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Timeout = 30_000
        };
        if (!string.IsNullOrWhiteSpace(_settings.Username))
            client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
        return client;
    }

    private static string BuildBookingConfirmedHtml(string customerName, string phoneNumber, string bookingCode,
        string tableName, DateTime start, DateTime end, int numberOfGuests)
    {
        var codeDisplay = bookingCode.StartsWith('#') ? bookingCode : $"#{bookingCode}";
        var durationHours = Math.Round((end - start).TotalHours, 1);
        return $$"""
            <!doctype html>
            <html lang="vi">
            <body style="margin:0;background:#f4f8f6;font-family:Arial,sans-serif;color:#10201c">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0">
                <tr><td align="center" style="padding:32px 16px">
                  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#fff;border:1px solid #dce7e2;border-radius:16px;box-shadow:0 4px 12px rgba(0,0,0,0.05)">
                    <tr><td style="padding:36px">
                      <div style="font-size:26px;font-weight:800;color:#0f5d4b;text-align:center;letter-spacing:-0.5px">PoolHub</div>
                      <h1 style="font-size:22px;margin:24px 0 16px;color:#10201c;text-align:center;font-weight:700">Xác nhận Đặt Bàn Thành Công!</h1>
                      <p style="color:#4a5568;line-height:1.6;font-size:15px;margin-bottom:24px">Kính chào <strong style="color:#1a202c">{{customerName}}</strong>,<br><br>Cảm ơn quý khách đã tin tưởng và lựa chọn dịch vụ tại <strong>PoolHub</strong>. Chúng tôi rất vui mừng thông báo yêu cầu đặt bàn của quý khách đã được xác nhận thành công.</p>
                      
                      <div style="background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:24px;margin:28px 0">
                        <div style="font-size:14px;font-weight:700;color:#0f5d4b;text-transform:uppercase;letter-spacing:0.5px">THÔNG TIN ĐẶT BÀN</div>
                        <hr style="border:0;border-top:1px solid #e2e8f0;margin:14px 0 16px">
                        <table style="width:100%;border-collapse:collapse;font-size:15px">
                          <tr><td style="padding:8px 0;color:#718096;width:40%">Mã Booking:</td><td style="padding:8px 0;font-family:monospace;font-weight:700;color:#1a202c">{{codeDisplay}}</td></tr>
                          <tr><td style="padding:8px 0;color:#718096">Khách hàng:</td><td style="padding:8px 0;font-weight:600;color:#1a202c">{{customerName}}</td></tr>
                          <tr><td style="padding:8px 0;color:#718096">Số điện thoại:</td><td style="padding:8px 0;font-weight:600;color:#1a202c">{{phoneNumber}}</td></tr>
                          <tr><td style="padding:8px 0;color:#718096">Bàn:</td><td style="padding:8px 0;font-weight:600;color:#0f5d4b">{{tableName}}</td></tr>
                          <tr><td style="padding:8px 0;color:#718096">Thời gian chơi:</td><td style="padding:8px 0;font-weight:600;color:#1a202c">{{start:HH:mm}} - {{end:HH:mm}}</td></tr>
                          <tr><td style="padding:8px 0;color:#718096">Ngày:</td><td style="padding:8px 0;font-weight:600;color:#1a202c">{{start:dd/MM/yyyy}}</td></tr>
                          <tr><td style="padding:8px 0;color:#718096">Thời lượng:</td><td style="padding:8px 0;font-weight:600;color:#1a202c">{{durationHours}} giờ</td></tr>
                          <tr><td style="padding:8px 0;color:#718096">Số lượng khách:</td><td style="padding:8px 0;font-weight:600;color:#1a202c">{{numberOfGuests}} người</td></tr>
                        </table>
                      </div>

                      <div style="margin:28px 0">
                        <div style="font-weight:700;color:#1a202c;font-size:15px;margin-bottom:12px">💡 Lưu ý quan trọng:</div>
                        <ul style="color:#4a5568;line-height:1.7;font-size:14px;padding-left:20px;margin:0">
                          <li style="margin-bottom:8px">Vui lòng có mặt tại quán <strong style="color:#1a202c">trước 10 phút</strong> so với giờ đặt để được nhân viên hỗ trợ nhận bàn tốt nhất.</li>
                          <li style="margin-bottom:8px">Trường hợp quý khách đến trễ quá 15 phút mà không có thông báo trước, hệ thống có thể tự động hủy bàn để nhường cho khách hàng khác.</li>
                          <li style="margin-bottom:8px">Nếu có bất kỳ thay đổi nào về thời gian hoặc số lượng người, xin vui lòng liên hệ với chúng tôi qua hotline để được hỗ trợ kịp thời.</li>
                        </ul>
                      </div>

                      <p style="color:#4a5568;line-height:1.6;font-size:15px;margin:28px 0 32px">Một lần nữa, xin chân thành cảm ơn quý khách. Hẹn gặp lại quý khách tại PoolHub để cùng trải nghiệm những giây phút giải trí tuyệt vời!</p>

                      <hr style="border:0;border-top:1px solid #e2e8f0;margin:32px 0 24px">
                      <div style="text-align:center;font-size:13px;color:#718096;line-height:1.6">
                        <strong style="color:#4a5568">Trung tâm Giải trí PoolHub</strong><br>
                        Hotline: 1900 xxxx | Email: <a href="mailto:support@poolhub.vn" style="color:#0f5d4b;text-decoration:none">support@poolhub.vn</a>
                      </div>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string BuildBookingCancelledHtml(string customerName, string phoneNumber, string bookingCode,
        string tableName, DateTime start, DateTime end, int numberOfGuests, string reason)
    {
        var codeDisplay = bookingCode.StartsWith('#') ? bookingCode : $"#{bookingCode}";
        var durationHours = Math.Round((end - start).TotalHours, 1);
        return $$"""
            <!doctype html>
            <html lang="vi">
            <body style="margin:0;background:#f4f8f6;font-family:Arial,sans-serif;color:#10201c">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0">
                <tr><td align="center" style="padding:32px 16px">
                  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#fff;border:1px solid #f5c6cb;border-radius:16px;box-shadow:0 4px 12px rgba(0,0,0,0.05)">
                    <tr><td style="padding:36px">
                      <div style="font-size:26px;font-weight:800;color:#0f5d4b;text-align:center;letter-spacing:-0.5px">PoolHub</div>
                      <h1 style="font-size:22px;margin:24px 0 16px;color:#c0392b;text-align:center;font-weight:700">Thông Báo Hủy Đặt Bàn</h1>
                      <p style="color:#4a5568;line-height:1.6;font-size:15px;margin-bottom:24px">Kính chào <strong style="color:#1a202c">{{customerName}}</strong>,<br><br>Rất tiếc khi phải thông báo rằng yêu cầu đặt bàn tại <strong>PoolHub</strong> của quý khách đã bị hủy. Chúng tôi thành thật xin lỗi vì sự bất tiện này.</p>
                      
                      <div style="background:#fffcfc;border:1px solid #f8d7da;border-radius:12px;padding:24px;margin:28px 0">
                        <div style="font-size:14px;font-weight:700;color:#c0392b;text-transform:uppercase;letter-spacing:0.5px">THÔNG TIN ĐẶT BÀN ĐÃ HỦY</div>
                        <hr style="border:0;border-top:1px solid #f8d7da;margin:14px 0 16px">
                        <table style="width:100%;border-collapse:collapse;font-size:15px">
                          <tr><td style="padding:8px 0;color:#718096;width:40%">Mã Booking:</td><td style="padding:8px 0;font-family:monospace;font-weight:700;color:#c0392b">{{codeDisplay}}</td></tr>
                          <tr><td style="padding:8px 0;color:#718096">Khách hàng:</td><td style="padding:8px 0;font-weight:600;color:#1a202c">{{customerName}}</td></tr>
                          <tr><td style="padding:8px 0;color:#718096">Số điện thoại:</td><td style="padding:8px 0;font-weight:600;color:#1a202c">{{phoneNumber}}</td></tr>
                          <tr><td style="padding:8px 0;color:#718096">Bàn:</td><td style="padding:8px 0;font-weight:600;color:#1a202c">{{tableName}}</td></tr>
                          <tr><td style="padding:8px 0;color:#718096">Thời gian chơi:</td><td style="padding:8px 0;font-weight:600;color:#1a202c">{{start:HH:mm}} - {{end:HH:mm}}</td></tr>
                          <tr><td style="padding:8px 0;color:#718096">Ngày:</td><td style="padding:8px 0;font-weight:600;color:#1a202c">{{start:dd/MM/yyyy}}</td></tr>
                          <tr><td style="padding:8px 0;color:#718096">Thời lượng:</td><td style="padding:8px 0;font-weight:600;color:#1a202c">{{durationHours}} giờ</td></tr>
                          <tr><td style="padding:8px 0;color:#718096">Lý do hủy:</td><td style="padding:8px 0;font-weight:700;color:#c0392b">{{reason}}</td></tr>
                        </table>
                      </div>

                      <div style="margin:28px 0">
                        <div style="font-weight:700;color:#1a202c;font-size:15px;margin-bottom:12px">💡 Hỗ trợ khách hàng:</div>
                        <ul style="color:#4a5568;line-height:1.7;font-size:14px;padding-left:20px;margin:0">
                          <li style="margin-bottom:8px">Quý khách hoàn toàn có thể lựa chọn và đặt lại một khung giờ khác trên website của chúng tôi.</li>
                          <li style="margin-bottom:8px">Nếu quý khách có bất kỳ thắc mắc hay khiếu nại nào liên quan đến việc hủy đặt bàn, vui lòng liên hệ ngay qua Hotline để được giải quyết ưu tiên.</li>
                        </ul>
                      </div>

                      <p style="color:#4a5568;line-height:1.6;font-size:15px;margin:28px 0 32px">Chúng tôi rất hy vọng sẽ sớm có cơ hội được đón tiếp và phục vụ quý khách tại PoolHub vào một dịp khác!</p>

                      <hr style="border:0;border-top:1px solid #f5c6cb;margin:32px 0 24px">
                      <div style="text-align:center;font-size:13px;color:#718096;line-height:1.6">
                        <strong style="color:#4a5568">Trung tâm Giải trí PoolHub</strong><br>
                        Hotline: 1900 xxxx | Email: <a href="mailto:support@poolhub.vn" style="color:#0f5d4b;text-decoration:none">support@poolhub.vn</a>
                      </div>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }
}

