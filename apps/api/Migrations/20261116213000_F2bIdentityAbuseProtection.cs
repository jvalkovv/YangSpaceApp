using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YangSpace.Api.Migrations;

public partial class F2bIdentityAbuseProtection : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "IdentityAbuseCounters",
            columns: table => new
            {
                Operation = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                KeyHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                WindowStartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                Count = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "PK_IdentityAbuseCounters",
                    x => new { x.Operation, x.KeyHash, x.WindowStartUtc });
            });

        migrationBuilder.CreateIndex(
            name: "IX_IdentityAbuseCounters_WindowStartUtc",
            table: "IdentityAbuseCounters",
            column: "WindowStartUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "IdentityAbuseCounters");
}
