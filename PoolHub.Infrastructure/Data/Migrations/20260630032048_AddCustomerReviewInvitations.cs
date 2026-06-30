using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoolHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerReviewInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_review_invitations",
                columns: table => new
                {
                    customer_review_invitation_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    token_hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    customer_id = table.Column<long>(type: "bigint", nullable: true),
                    session_id = table.Column<long>(type: "bigint", nullable: false),
                    invoice_id = table.Column<long>(type: "bigint", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    used_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_review_invitations", x => x.customer_review_invitation_id);
                    table.CheckConstraint("CK_customer_review_invitations_status", "[status] IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_customer_review_invitations_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_review_invitations_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "invoice_id");
                    table.ForeignKey(
                        name: "FK_customer_review_invitations_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "session_id");
                    table.ForeignKey(
                        name: "FK_customer_review_invitations_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_review_invitations_created_by_user_id",
                table: "customer_review_invitations",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_review_invitations_customer_id",
                table: "customer_review_invitations",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_review_invitations_invoice_id_status",
                table: "customer_review_invitations",
                columns: new[] { "invoice_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_review_invitations_public_id",
                table: "customer_review_invitations",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_review_invitations_session_id",
                table: "customer_review_invitations",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_review_invitations_token_hash",
                table: "customer_review_invitations",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_review_invitations");
        }
    }
}
