using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoolHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingDepositRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "booking_deposit_refunds",
                columns: table => new
                {
                    booking_deposit_refund_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    booking_deposit_id = table.Column<long>(type: "bigint", nullable: false),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    invoice_id = table.Column<long>(type: "bigint", nullable: true),
                    customer_id = table.Column<long>(type: "bigint", nullable: true),
                    refund_code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    refund_method = table.Column<int>(type: "int", nullable: true),
                    reason_detail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    customer_email_snapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    customer_phone_snapshot = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    customer_token_hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    customer_token_expires_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    customer_info_submitted_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    customer_bank_code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    customer_bank_name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    customer_bank_account_number_encrypted = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    customer_bank_account_last4 = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    customer_bank_account_name_encrypted = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    manual_transfer_code = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    cash_receipt_code = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    proof_media_asset_id = table.Column<long>(type: "bigint", nullable: true),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    requested_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    approved_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    processed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    processing_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    succeeded_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    failed_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    rejected_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    cancelled_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    failure_reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    reject_reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    idempotency_key = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_deposit_refunds", x => x.booking_deposit_refund_id);
                    table.CheckConstraint("ck_booking_deposit_refunds_amount_positive", "amount > 0");
                    table.ForeignKey(
                        name: "FK_booking_deposit_refunds_booking_deposits_booking_deposit_id",
                        column: x => x.booking_deposit_id,
                        principalTable: "booking_deposits",
                        principalColumn: "booking_deposit_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_booking_deposit_refunds_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "booking_id");
                    table.ForeignKey(
                        name: "FK_booking_deposit_refunds_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id");
                    table.ForeignKey(
                        name: "FK_booking_deposit_refunds_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "invoice_id");
                    table.ForeignKey(
                        name: "FK_booking_deposit_refunds_media_assets_proof_media_asset_id",
                        column: x => x.proof_media_asset_id,
                        principalTable: "media_assets",
                        principalColumn: "media_asset_id");
                    table.ForeignKey(
                        name: "FK_booking_deposit_refunds_users_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_booking_deposit_refunds_users_processed_by_user_id",
                        column: x => x.processed_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_booking_deposit_refunds_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_booking_deposit_refunds_approved_by_user_id",
                table: "booking_deposit_refunds",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_deposit_refunds_booking_deposit_id",
                table: "booking_deposit_refunds",
                column: "booking_deposit_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_deposit_refunds_booking_id",
                table: "booking_deposit_refunds",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_deposit_refunds_created_at_utc",
                table: "booking_deposit_refunds",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_booking_deposit_refunds_customer_id",
                table: "booking_deposit_refunds",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_deposit_refunds_idempotency_key",
                table: "booking_deposit_refunds",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_deposit_refunds_invoice_id",
                table: "booking_deposit_refunds",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_deposit_refunds_processed_by_user_id",
                table: "booking_deposit_refunds",
                column: "processed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_deposit_refunds_proof_media_asset_id",
                table: "booking_deposit_refunds",
                column: "proof_media_asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_deposit_refunds_public_id",
                table: "booking_deposit_refunds",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_deposit_refunds_refund_code",
                table: "booking_deposit_refunds",
                column: "refund_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_deposit_refunds_requested_by_user_id",
                table: "booking_deposit_refunds",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_deposit_refunds_status",
                table: "booking_deposit_refunds",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_deposit_refunds");
        }
    }
}
