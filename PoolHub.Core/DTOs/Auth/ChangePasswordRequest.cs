using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Auth;

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ConfirmNewPassword
    {
        get => ConfirmPassword;
        set => ConfirmPassword = value ?? string.Empty;
    }
}
