using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YangSpace.PostgreSql.Migrations;

[DbContext(typeof(DirectoryDb))]
[Migration("20261116213000_F2bIdentityAbuseProtection")]
public partial class F2bIdentityAbuseProtection : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "IdentityAbuseCounters",
            columns: table => new
            {
                Operation = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                KeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                WindowStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Count = table.Column<int>(type: "integer", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
