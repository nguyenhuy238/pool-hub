using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoolHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDepositRefundPublicWorkflowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "customer_bank_account_number_encrypted",
                table: "booking_deposit_refunds",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "customer_bank_account_name_encrypted",
                table: "booking_deposit_refunds",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "cash_pickup_code_expires_at_utc",
                table: "booking_deposit_refunds",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cash_pickup_code_hash",
                table: "booking_deposit_refunds",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "cash_pickup_code_used_at_utc",
                table: "booking_deposit_refunds",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "customer_token_generated_at_utc",
                table: "booking_deposit_refunds",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "customer_verified_at_utc",
                table: "booking_deposit_refunds",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "verification_code_expires_at_utc",
                table: "booking_deposit_refunds",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "verification_code_hash",
                table: "booking_deposit_refunds",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "verification_code_sent_at_utc",
                table: "booking_deposit_refunds",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "verification_failed_attempts",
                table: "booking_deposit_refunds",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_booking_deposit_refunds_customer_token_hash",
                table: "booking_deposit_refunds",
                column: "customer_token_hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_booking_deposit_refunds_customer_token_hash",
                table: "booking_deposit_refunds");

            migrationBuilder.DropColumn(
                name: "cash_pickup_code_expires_at_utc",
                table: "booking_deposit_refunds");

            migrationBuilder.DropColumn(
                name: "cash_pickup_code_hash",
                table: "booking_deposit_refunds");

            migrationBuilder.DropColumn(
                name: "cash_pickup_code_used_at_utc",
                table: "booking_deposit_refunds");

            migrationBuilder.DropColumn(
                name: "customer_token_generated_at_utc",
                table: "booking_deposit_refunds");

            migrationBuilder.DropColumn(
                name: "customer_verified_at_utc",
                table: "booking_deposit_refunds");

            migrationBuilder.DropColumn(
                name: "verification_code_expires_at_utc",
                table: "booking_deposit_refunds");

            migrationBuilder.DropColumn(
                name: "verification_code_hash",
                table: "booking_deposit_refunds");

            migrationBuilder.DropColumn(
                name: "verification_code_sent_at_utc",
                table: "booking_deposit_refunds");

            migrationBuilder.DropColumn(
                name: "verification_failed_attempts",
                table: "booking_deposit_refunds");

            migrationBuilder.AlterColumn<string>(
                name: "customer_bank_account_number_encrypted",
                table: "booking_deposit_refunds",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "customer_bank_account_name_encrypted",
                table: "booking_deposit_refunds",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2048)",
                oldMaxLength: 2048,
                oldNullable: true);
        }
    }
}
