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
            // Older databases can contain assignments that were never ended before the
            // table was assigned again. Close those stale rows at the next assignment's
            // start time so that the filtered unique index can be created without
            // discarding assignment history.
            migrationBuilder.Sql(
                """
                ;WITH ordered_assignments AS
                (
                    SELECT
                        session_table_assignment_id,
                        started_at_utc,
                        LEAD(started_at_utc) OVER
                        (
                            PARTITION BY table_id
                            ORDER BY started_at_utc, session_table_assignment_id
                        ) AS next_started_at_utc
                    FROM session_table_assignments
                    WHERE ended_at_utc IS NULL
                )
                UPDATE assignment
                SET ended_at_utc =
                    CASE
                        WHEN ordered.next_started_at_utc > ordered.started_at_utc
                            THEN ordered.next_started_at_utc
                        ELSE DATEADD(millisecond, 1, ordered.started_at_utc)
                    END
                FROM session_table_assignments AS assignment
                INNER JOIN ordered_assignments AS ordered
                    ON ordered.session_table_assignment_id = assignment.session_table_assignment_id
                WHERE assignment.ended_at_utc IS NULL
                  AND ordered.next_started_at_utc IS NOT NULL;
                """);

            // The first version of this migration could fail after dropping the
            // old index (depending on transaction settings). Make the repair
            // rerunnable for databases left in either state.
            migrationBuilder.Sql(
                """
                IF EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_session_table_assignments_table_id'
                      AND object_id = OBJECT_ID(N'dbo.session_table_assignments')
                )
                BEGIN
                    DROP INDEX [IX_session_table_assignments_table_id]
                        ON [dbo].[session_table_assignments];
                END;
                """);

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
