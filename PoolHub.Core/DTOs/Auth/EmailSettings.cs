namespace PoolHub.Core.DTOs.Auth;

public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "PoolHub";
    public string FrontendBaseUrl { get; set; } = "http://localhost:3000";
    public int PasswordResetExpirationMinutes { get; set; } = 30;
}
