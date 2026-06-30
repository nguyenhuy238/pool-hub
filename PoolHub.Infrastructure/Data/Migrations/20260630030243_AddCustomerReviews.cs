using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoolHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_reviews",
                columns: table => new
                {
                    customer_review_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_id = table.Column<long>(type: "bigint", nullable: true),
                    booking_id = table.Column<long>(type: "bigint", nullable: true),
                    session_id = table.Column<long>(type: "bigint", nullable: true),
                    invoice_id = table.Column<long>(type: "bigint", nullable: true),
                    rating = table.Column<int>(type: "int", nullable: false),
                    content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    display_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    avatar_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    check_in_image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    is_featured = table.Column<bool>(type: "bit", nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    source = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    approved_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    rejected_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_reviews", x => x.customer_review_id);
                    table.CheckConstraint("CK_customer_reviews_rating", "[rating] >= 1 AND [rating] <= 5");
                    table.CheckConstraint("CK_customer_reviews_status", "[status] IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_customer_reviews_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "booking_id");
                    table.ForeignKey(
                        name: "FK_customer_reviews_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_reviews_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "invoice_id");
                    table.ForeignKey(
                        name: "FK_customer_reviews_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "session_id");
                    table.ForeignKey(
                        name: "FK_customer_reviews_users_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_reviews_approved_by_user_id",
                table: "customer_reviews",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_reviews_booking_id",
                table: "customer_reviews",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_reviews_customer_id_booking_id",
                table: "customer_reviews",
                columns: new[] { "customer_id", "booking_id" },
                unique: true,
                filter: "[customer_id] IS NOT NULL AND [booking_id] IS NOT NULL AND [status] IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_customer_reviews_customer_id_invoice_id",
                table: "customer_reviews",
                columns: new[] { "customer_id", "invoice_id" },
                unique: true,
                filter: "[customer_id] IS NOT NULL AND [invoice_id] IS NOT NULL AND [status] IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_customer_reviews_customer_id_session_id",
                table: "customer_reviews",
                columns: new[] { "customer_id", "session_id" },
                unique: true,
                filter: "[customer_id] IS NOT NULL AND [session_id] IS NOT NULL AND [status] IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_customer_reviews_invoice_id",
                table: "customer_reviews",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_reviews_public_id",
                table: "customer_reviews",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_reviews_session_id",
                table: "customer_reviews",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_reviews_status_is_featured_display_order",
                table: "customer_reviews",
                columns: new[] { "status", "is_featured", "display_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_reviews");
        }
    }
}
