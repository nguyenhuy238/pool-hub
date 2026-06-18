using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Auth;

public class ForgotPasswordRequest
{
    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; set; } = string.Empty;
}
