using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoolHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveSessionTableAssignmentUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_session_table_assignments_table_id",
                table: "session_table_assignments");

            migrationBuilder.CreateIndex(
                name: "IX_session_table_assignments_table_id",
                table: "session_table_assignments",
                column: "table_id",
                unique: true,
                filter: "[ended_at_utc] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_session_table_assignments_table_id",
                table: "session_table_assignments");

            migrationBuilder.CreateIndex(
                name: "IX_session_table_assignments_table_id",
                table: "session_table_assignments",
                column: "table_id");
        }
    }
}
