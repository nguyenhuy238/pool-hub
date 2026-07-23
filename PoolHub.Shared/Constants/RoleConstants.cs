namespace PoolHub.Shared.Constants;

public static class RoleConstants
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Staff = "Staff";
    public const string Cashier = "Cashier";
    public const string Customer = "Customer";
    public const string Guest = "Guest";
    public const string Operation = Admin + "," + Manager + "," + Staff;
    public static readonly string[] All = [Admin, Manager, Staff, Cashier, Customer, Guest];
    public static readonly string[] SystemInternal = [Admin, Manager, Staff, Cashier];
    public static readonly string[] Retired = ["LegacyCashier"];
}
