using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoolHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerUserLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "user_id",
                table: "customers",
                type: "bigint",
                nullable: true);

            // Claim only unambiguous existing customer profiles. Guest profiles remain
            // unlinked and can be claimed later through the authenticated registration
            // flow.
            migrationBuilder.Sql(
                """
                UPDATE customer
                SET user_id = account.user_id
                FROM customers AS customer
                INNER JOIN users AS account
                    ON LOWER(account.email) = LOWER(customer.email)
                INNER JOIN user_roles AS user_role
                    ON user_role.user_id = account.user_id
                INNER JOIN roles AS role
                    ON role.role_id = user_role.role_id
                WHERE customer.user_id IS NULL
                  AND customer.email IS NOT NULL
                  AND customer.status = 1
                  AND account.status = 1
                  AND
                  (
                      customer.phone_number IS NULL
                      OR account.phone_number IS NULL
                      OR account.phone_number = customer.phone_number
                  )
                  AND role.name = N'Customer'
                  AND role.is_active = 1
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM customers AS already_linked
                      WHERE already_linked.user_id = account.user_id
                  );

                UPDATE customer
                SET user_id = account.user_id
                FROM customers AS customer
                INNER JOIN users AS account
                    ON account.phone_number = customer.phone_number
                INNER JOIN user_roles AS user_role
                    ON user_role.user_id = account.user_id
                INNER JOIN roles AS role
                    ON role.role_id = user_role.role_id
                WHERE customer.user_id IS NULL
                  AND customer.status = 1
                  AND account.status = 1
                  AND role.name = N'Customer'
                  AND role.is_active = 1
                  AND
                  (
                      customer.email IS NULL
                      OR LOWER(account.email) = LOWER(customer.email)
                  )
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM customers AS already_linked
                      WHERE already_linked.user_id = account.user_id
                  );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_customers_user_id",
                table: "customers",
                column: "user_id",
                unique: true,
                filter: "[user_id] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_customers_users_user_id",
                table: "customers",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_customers_users_user_id",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_customers_user_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "customers");
        }
    }
}
