namespace PoolHub.Shared.Constants;

public static class AuditActions
{
    public const string LoginSuccess = "AUTH_LOGIN_SUCCESS";
    public const string LoginFailed = "AUTH_LOGIN_FAILED";
    public const string Logout = "AUTH_LOGOUT";
    public const string RefreshToken = "AUTH_REFRESH_TOKEN";
    public const string Register = "AUTH_REGISTER";
    public const string ForgotPassword = "AUTH_FORGOT_PASSWORD";
    public const string ForgotPasswordEmailFailed = "AUTH_FORGOT_PASSWORD_EMAIL_FAILED";
    public const string ResetPasswordSuccess = "AUTH_RESET_PASSWORD_SUCCESS";
    public const string ResetPasswordFailed = "AUTH_RESET_PASSWORD_FAILED";
    public const string ChangePassword = "AUTH_CHANGE_PASSWORD";
    public const string UserCreated = "USER_CREATED";
    public const string UserUpdated = "USER_UPDATED";
    public const string UserStatusChanged = "USER_STATUS_CHANGED";
    public const string RoleCreated = "ROLE_CREATED";
    public const string RoleUpdated = "ROLE_UPDATED";
    public const string RoleDeleted = "ROLE_DELETED";
    public const string UserRoleAssigned = "USER_ROLE_ASSIGNED";
    public const string UserRoleRemoved = "USER_ROLE_REMOVED";
}
