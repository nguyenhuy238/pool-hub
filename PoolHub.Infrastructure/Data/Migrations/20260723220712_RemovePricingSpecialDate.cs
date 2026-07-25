using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoolHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovePricingSpecialDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pricing_special_dates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pricing_special_dates",
                columns: table => new
                {
                    pricing_special_date_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    day_type = table.Column<int>(type: "int", nullable: false),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pricing_special_dates", x => x.pricing_special_date_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pricing_special_dates_date",
                table: "pricing_special_dates",
                column: "date",
                unique: true);
        }
    }
}
