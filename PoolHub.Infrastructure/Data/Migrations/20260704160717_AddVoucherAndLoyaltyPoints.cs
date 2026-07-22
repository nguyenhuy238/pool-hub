using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoolHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVoucherAndLoyaltyPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "customer_id",
                table: "discounts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_voucher",
                table: "discounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "max_usage",
                table: "discounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "points_required",
                table: "discounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "usage_count",
                table: "discounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "loyalty_points",
                table: "customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "total_points_earned",
                table: "customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "customer_point_histories",
                columns: table => new
                {
                    customer_point_history_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    customer_id = table.Column<long>(type: "bigint", nullable: false),
                    points = table.Column<int>(type: "int", nullable: false),
                    transaction_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    reference_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_point_histories", x => x.customer_point_history_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_point_histories_customer_id",
                table: "customer_point_histories",
                column: "customer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_point_histories");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "discounts");

            migrationBuilder.DropColumn(
                name: "is_voucher",
                table: "discounts");

            migrationBuilder.DropColumn(
                name: "max_usage",
                table: "discounts");

            migrationBuilder.DropColumn(
                name: "points_required",
                table: "discounts");

            migrationBuilder.DropColumn(
                name: "usage_count",
                table: "discounts");

            migrationBuilder.DropColumn(
                name: "loyalty_points",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "total_points_earned",
                table: "customers");
        }
    }
}
