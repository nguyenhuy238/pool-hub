namespace PoolHub.Core.Interfaces.Services;

public interface IEmailService
{
    void EnsureConfigured();
    Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken);
}
