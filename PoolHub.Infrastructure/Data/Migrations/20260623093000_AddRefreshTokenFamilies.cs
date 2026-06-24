using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PoolHub.Infrastructure.Data;

#nullable disable

namespace PoolHub.Infrastructure.Data.Migrations;

[DbContext(typeof(PoolHubDbContext))]
[Migration("20260623093000_AddRefreshTokenFamilies")]
public partial class AddRefreshTokenFamilies : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "family_id",
            table: "refresh_tokens",
            type: "uniqueidentifier",
            nullable: false,
            defaultValueSql: "NEWID()");

        migrationBuilder.AddColumn<string>(
            name: "replaced_by_token_hash",
            table: "refresh_tokens",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_refresh_tokens_user_id_family_id",
            table: "refresh_tokens",
            columns: new[] { "user_id", "family_id" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_refresh_tokens_user_id_family_id", table: "refresh_tokens");
        migrationBuilder.DropColumn(name: "family_id", table: "refresh_tokens");
        migrationBuilder.DropColumn(name: "replaced_by_token_hash", table: "refresh_tokens");
    }
}
