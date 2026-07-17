using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoolHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerReviewPrivacyAndUniqueTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_customer_reviews_booking_id",
                table: "customer_reviews");

            migrationBuilder.DropIndex(
                name: "IX_customer_reviews_invoice_id",
                table: "customer_reviews");

            migrationBuilder.DropIndex(
                name: "IX_customer_reviews_session_id",
                table: "customer_reviews");

            migrationBuilder.AlterColumn<string>(
                name: "content",
                table: "customer_reviews",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<bool>(
                name: "is_anonymous",
                table: "customer_reviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_verified",
                table: "customer_reviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_customer_reviews_booking_id",
                table: "customer_reviews",
                column: "booking_id",
                unique: true,
                filter: "[booking_id] IS NOT NULL AND [status] IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_customer_reviews_invoice_id",
                table: "customer_reviews",
                column: "invoice_id",
                unique: true,
                filter: "[invoice_id] IS NOT NULL AND [status] IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_customer_reviews_session_id",
                table: "customer_reviews",
                column: "session_id",
                unique: true,
                filter: "[session_id] IS NOT NULL AND [status] IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_customer_reviews_booking_id",
                table: "customer_reviews");

            migrationBuilder.DropIndex(
                name: "IX_customer_reviews_invoice_id",
                table: "customer_reviews");

            migrationBuilder.DropIndex(
                name: "IX_customer_reviews_session_id",
                table: "customer_reviews");

            migrationBuilder.DropColumn(
                name: "is_anonymous",
                table: "customer_reviews");

            migrationBuilder.DropColumn(
                name: "is_verified",
                table: "customer_reviews");

            migrationBuilder.AlterColumn<string>(
                name: "content",
                table: "customer_reviews",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_reviews_booking_id",
                table: "customer_reviews",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_reviews_invoice_id",
                table: "customer_reviews",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_reviews_session_id",
                table: "customer_reviews",
                column: "session_id");
        }
    }
}
