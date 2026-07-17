using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoolHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueTableIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('venue_tables', 'is_active') IS NULL
                BEGIN
                    ALTER TABLE [venue_tables] ADD [is_active] bit NOT NULL DEFAULT CAST(1 AS bit);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('venue_tables', 'is_active') IS NOT NULL
                BEGIN
                    DECLARE @constraintName sysname;
                    SELECT @constraintName = dc.name
                    FROM sys.default_constraints dc
                    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                    INNER JOIN sys.tables t ON t.object_id = c.object_id
                    WHERE t.name = 'venue_tables' AND c.name = 'is_active';

                    IF @constraintName IS NOT NULL
                    BEGIN
                        EXEC('ALTER TABLE [venue_tables] DROP CONSTRAINT [' + @constraintName + ']');
                    END

                    ALTER TABLE [venue_tables] DROP COLUMN [is_active];
                END
                """);
        }
    }
}
