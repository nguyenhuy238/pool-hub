using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PoolHub.Infrastructure.Data;

#nullable disable

namespace PoolHub.Infrastructure.Data.Migrations;

[DbContext(typeof(PoolHubDbContext))]
[Migration("20260723000000_RemoveCashierRole")]
public partial class RemoveCashierRole : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DECLARE @cashierRoleId bigint = (
    SELECT TOP (1) role_id
    FROM roles
    WHERE LOWER(name) = N'cashier'
    ORDER BY role_id
);

IF @cashierRoleId IS NOT NULL
BEGIN
    DECLARE @staffRoleId bigint = (
        SELECT TOP (1) role_id
        FROM roles
        WHERE LOWER(name) = N'staff'
        ORDER BY role_id
    );

    IF @staffRoleId IS NULL
    BEGIN
        INSERT INTO roles (name, description, is_system, is_active, created_at_utc, updated_at_utc)
        VALUES (N'Staff', N'Floor staff', 1, 1, SYSUTCDATETIME(), NULL);

        SET @staffRoleId = SCOPE_IDENTITY();
    END;

    DECLARE @usersToMigrate int = (
        SELECT COUNT(*)
        FROM user_roles
        WHERE role_id = @cashierRoleId
    );

    INSERT INTO user_roles (user_id, role_id, assigned_at_utc, assigned_by_user_id)
    SELECT ur.user_id, @staffRoleId, SYSUTCDATETIME(), NULL
    FROM user_roles ur
    WHERE ur.role_id = @cashierRoleId
      AND NOT EXISTS (
          SELECT 1
          FROM user_roles existing
          WHERE existing.user_id = ur.user_id
            AND existing.role_id = @staffRoleId
      );

    DELETE FROM user_roles
    WHERE role_id = @cashierRoleId;

    DELETE FROM role_permissions
    WHERE role_id = @cashierRoleId;

    INSERT INTO audit_logs (
        actor_user_id, action, entity_name, entity_id, entity_public_id,
        old_values, new_values, ip_address, user_agent, description,
        created_at_utc, updated_at_utc
    )
    VALUES (
        NULL,
        N'LEGACY_CASHIER_MIGRATED',
        N'Role',
        @cashierRoleId,
        NULL,
        CONCAT(N'{"role":"Cashier","userRoleCount":', @usersToMigrate, N'}'),
        CONCAT(N'{"role":"Staff","staffRoleId":', @staffRoleId, N'}'),
        NULL,
        N'EF Migration',
        CONCAT(N'Migrated ', @usersToMigrate, N' Cashier user-role assignment(s) to Staff and removed the retired role.'),
        SYSUTCDATETIME(),
        NULL
    );

    DELETE FROM roles
    WHERE role_id = @cashierRoleId;
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM roles WHERE LOWER(name) = N'cashier')
BEGIN
    INSERT INTO roles (name, description, is_system, is_active, created_at_utc, updated_at_utc)
    VALUES (N'Cashier', N'Retired cashier role restored by migration rollback.', 1, 1, SYSUTCDATETIME(), NULL);
END;

DECLARE @cashierRoleId bigint = (
    SELECT TOP (1) role_id
    FROM roles
    WHERE LOWER(name) = N'cashier'
    ORDER BY role_id
);

IF @cashierRoleId IS NOT NULL
BEGIN
    INSERT INTO role_permissions (role_id, permission_id, assigned_at_utc, assigned_by_user_id)
    SELECT @cashierRoleId, p.permission_id, SYSUTCDATETIME(), NULL
    FROM permissions p
    WHERE p.code IN (N'discounts.manage', N'payments.manage')
      AND p.is_active = 1
      AND NOT EXISTS (
          SELECT 1
          FROM role_permissions rp
          WHERE rp.role_id = @cashierRoleId
            AND rp.permission_id = p.permission_id
      );
END;
""");
    }
}
