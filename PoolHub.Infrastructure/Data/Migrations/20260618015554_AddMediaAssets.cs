using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoolHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "media_assets",
                columns: table => new
                {
                    media_asset_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    original_file_name = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    stored_file_name = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    folder = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    content_type = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    media_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    alt_text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    uploaded_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_assets", x => x.media_asset_id);
                    table.ForeignKey(
                        name: "FK_media_assets_users_uploaded_by_user_id",
                        column: x => x.uploaded_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_media_assets_public_id",
                table: "media_assets",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_media_assets_uploaded_by_user_id",
                table: "media_assets",
                column: "uploaded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_media_assets_url",
                table: "media_assets",
                column: "url",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "media_assets");
        }
    }
}
