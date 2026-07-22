using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoolHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingDeposits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "approved_at_utc",
                table: "bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "approved_by_user_id",
                table: "bookings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                table: "bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "estimated_amount",
                table: "bookings",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "hold_expires_at_utc",
                table: "bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "no_show_at_utc",
                table: "bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "requires_approval",
                table: "bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "bookings",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "booking_deposits",
                columns: table => new
                {
                    booking_deposit_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    required_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    paid_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    applied_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    refunded_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    forfeited_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    payment_method_id = table.Column<long>(type: "bigint", nullable: true),
                    transaction_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    paid_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    applied_to_invoice_id = table.Column<long>(type: "bigint", nullable: true),
                    refunded_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    forfeited_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    due_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_deposits", x => x.booking_deposit_id);
                    table.ForeignKey(
                        name: "FK_booking_deposits_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "booking_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_booking_deposits_invoices_applied_to_invoice_id",
                        column: x => x.applied_to_invoice_id,
                        principalTable: "invoices",
                        principalColumn: "invoice_id");
                    table.ForeignKey(
                        name: "FK_booking_deposits_payment_methods_payment_method_id",
                        column: x => x.payment_method_id,
                        principalTable: "payment_methods",
                        principalColumn: "payment_method_id");
                });

            migrationBuilder.CreateTable(
                name: "booking_tables",
                columns: table => new
                {
                    booking_table_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    table_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_tables", x => x.booking_table_id);
                    table.ForeignKey(
                        name: "FK_booking_tables_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "booking_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_booking_tables_venue_tables_table_id",
                        column: x => x.table_id,
                        principalTable: "venue_tables",
                        principalColumn: "table_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_approved_by_user_id",
                table: "bookings",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_deposits_applied_to_invoice_id",
                table: "booking_deposits",
                column: "applied_to_invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_deposits_booking_id",
                table: "booking_deposits",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_deposits_payment_method_id",
                table: "booking_deposits",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_tables_booking_id_table_id",
                table: "booking_tables",
                columns: new[] { "booking_id", "table_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_tables_table_id",
                table: "booking_tables",
                column: "table_id");

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_users_approved_by_user_id",
                table: "bookings",
                column: "approved_by_user_id",
                principalTable: "users",
                principalColumn: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_users_approved_by_user_id",
                table: "bookings");

            migrationBuilder.DropTable(
                name: "booking_deposits");

            migrationBuilder.DropTable(
                name: "booking_tables");

            migrationBuilder.DropIndex(
                name: "IX_bookings_approved_by_user_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "approved_at_utc",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "approved_by_user_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "estimated_amount",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "hold_expires_at_utc",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "no_show_at_utc",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "requires_approval",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "source",
                table: "bookings");
        }
    }
}
