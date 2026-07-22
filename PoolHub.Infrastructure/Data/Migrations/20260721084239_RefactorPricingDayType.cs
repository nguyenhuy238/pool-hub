using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoolHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPricingDayType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "day_of_week",
                table: "pricing_plan_rules",
                newName: "day_type");

            migrationBuilder.RenameIndex(
                name: "IX_pricing_plan_rules_pricing_plan_id_table_type_id_day_of_week_start_time",
                table: "pricing_plan_rules",
                newName: "IX_pricing_plan_rules_pricing_plan_id_table_type_id_day_type_start_time");

            // Data Migration: Map DayOfWeek (0-6) to DayType (1: Weekday, 2: Weekend).
            // Remove rows that will collide on the renamed unique index before updating values.
            migrationBuilder.Sql(@"
                WITH CTE AS (
                    SELECT pricing_plan_rule_id,
                           FIRST_VALUE(pricing_plan_rule_id) OVER (
                               PARTITION BY pricing_plan_id, table_type_id, 
                                 CASE WHEN day_type IN (0, 6) THEN 2 ELSE 1 END, 
                                 start_time
                               ORDER BY pricing_plan_rule_id
                           ) as kept_rule_id
                    FROM pricing_plan_rules
                )
                UPDATE s
                SET s.pricing_plan_rule_id = CTE.kept_rule_id
                FROM session_table_assignments s
                JOIN CTE ON s.pricing_plan_rule_id = CTE.pricing_plan_rule_id
                WHERE CTE.pricing_plan_rule_id <> CTE.kept_rule_id;

                WITH CTE AS (
                    SELECT pricing_plan_rule_id,
                           ROW_NUMBER() OVER (
                               PARTITION BY pricing_plan_id, table_type_id, 
                                 CASE WHEN day_type IN (0, 6) THEN 2 ELSE 1 END, 
                                 start_time
                               ORDER BY pricing_plan_rule_id
                           ) as row_num
                    FROM pricing_plan_rules
                )
                DELETE FROM pricing_plan_rules WHERE pricing_plan_rule_id IN (SELECT pricing_plan_rule_id FROM CTE WHERE row_num > 1);
            ");

            migrationBuilder.Sql("UPDATE pricing_plan_rules SET day_type = CASE WHEN day_type IN (0, 6) THEN 102 ELSE 101 END;");
            migrationBuilder.Sql("UPDATE pricing_plan_rules SET day_type = CASE WHEN day_type = 102 THEN 2 ELSE 1 END;");

            migrationBuilder.CreateTable(
                name: "pricing_special_dates",
                columns: table => new
                {
                    pricing_special_date_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    day_type = table.Column<int>(type: "int", nullable: false),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pricing_special_dates");

            migrationBuilder.RenameColumn(
                name: "day_type",
                table: "pricing_plan_rules",
                newName: "day_of_week");

            migrationBuilder.RenameIndex(
                name: "IX_pricing_plan_rules_pricing_plan_id_table_type_id_day_type_start_time",
                table: "pricing_plan_rules",
                newName: "IX_pricing_plan_rules_pricing_plan_id_table_type_id_day_of_week_start_time");
        }
    }
}
