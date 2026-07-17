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
    public const string CustomerCreated = "CUSTOMER_CREATED";
    public const string CustomerUpdated = "CUSTOMER_UPDATED";
    public const string CustomerStatusChanged = "CUSTOMER_STATUS_CHANGED";
    public const string CustomerReviewApproved = "CUSTOMER_REVIEW_APPROVED";
    public const string CustomerReviewRejected = "CUSTOMER_REVIEW_REJECTED";
    public const string CustomerReviewUpdated = "CUSTOMER_REVIEW_UPDATED";
    public const string CustomerReviewHidden = "CUSTOMER_REVIEW_HIDDEN";
    public const string CustomerReviewInvitationCreated = "CUSTOMER_REVIEW_INVITATION_CREATED";
    public const string CustomerReviewInvitationUsed = "CUSTOMER_REVIEW_INVITATION_USED";
    public const string CustomerVoucherExchanged = "CUSTOMER_VOUCHER_EXCHANGED";
    public const string CustomerPointsEarned = "CUSTOMER_POINTS_EARNED";
}
