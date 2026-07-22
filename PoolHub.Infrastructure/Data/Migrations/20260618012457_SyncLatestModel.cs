using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoolHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncLatestModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "site_settings",
                columns: table => new
                {
                    site_setting_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    setting_key = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    setting_value_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_settings", x => x.site_setting_id);
                    table.ForeignKey(
                        name: "FK_site_settings_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_site_settings_setting_key",
                table: "site_settings",
                column: "setting_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_site_settings_updated_by_user_id",
                table: "site_settings",
                column: "updated_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "site_settings");
        }
    }
}
