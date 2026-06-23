namespace PoolHub.Shared.Constants;

public static class PermissionConstants
{
    public const string ClaimType = "permission";
    public const string UsersManage = "users.manage";
    public const string RolesManage = "roles.manage";
    public const string CustomersManage = "customers.manage";
    public const string VenueManage = "venue.manage";
    public const string PricingManage = "pricing.manage";
    public const string ProductsManage = "products.manage";
    public const string InventoryManage = "inventory.manage";
    public const string DiscountsManage = "discounts.manage";
    public const string PaymentsManage = "payments.manage";
    public const string LandingManage = "landing.manage";
    public const string ReportsView = "reports.view";
    public const string AuditView = "audit.view";

    public static readonly string[] All =
    [
        UsersManage, RolesManage, CustomersManage, VenueManage, PricingManage, ProductsManage,
        InventoryManage, DiscountsManage, PaymentsManage, LandingManage, ReportsView, AuditView
    ];
}
