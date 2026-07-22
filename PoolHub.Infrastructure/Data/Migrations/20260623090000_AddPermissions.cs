using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PoolHub.Infrastructure.Data;

#nullable disable

namespace PoolHub.Infrastructure.Data.Migrations;

[DbContext(typeof(PoolHubDbContext))]
[Migration("20260623090000_AddPermissions")]
public partial class AddPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "permissions",
            columns: table => new
            {
                permission_id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                code = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                group = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                is_active = table.Column<bool>(type: "bit", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_permissions", x => x.permission_id));

        migrationBuilder.CreateTable(
            name: "role_permissions",
            columns: table => new
            {
                role_id = table.Column<long>(type: "bigint", nullable: false),
                permission_id = table.Column<long>(type: "bigint", nullable: false),
                assigned_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                assigned_by_user_id = table.Column<long>(type: "bigint", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_role_permissions", x => new { x.role_id, x.permission_id });
                table.ForeignKey("FK_role_permissions_permissions_permission_id", x => x.permission_id,
                    "permissions", "permission_id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_role_permissions_roles_role_id", x => x.role_id,
                    "roles", "role_id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_role_permissions_users_assigned_by_user_id", x => x.assigned_by_user_id,
                    "users", "user_id", onDelete: ReferentialAction.NoAction);
            });

        migrationBuilder.CreateIndex("IX_permissions_code", "permissions", "code", unique: true);
        migrationBuilder.CreateIndex("IX_permissions_group_is_active", "permissions", new[] { "group", "is_active" });
        migrationBuilder.CreateIndex("IX_role_permissions_assigned_by_user_id", "role_permissions", "assigned_by_user_id");
        migrationBuilder.CreateIndex("IX_role_permissions_permission_id", "role_permissions", "permission_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "role_permissions");
        migrationBuilder.DropTable(name: "permissions");
    }
}
