using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YangSpace.Api.Migrations
{
    /// <inheritdoc />
    public partial class F2bIdentityEmailOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdentityEmailOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Purpose = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    QueuedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AvailableAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LeaseId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LeasedUntilUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeadLetterAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastFailureCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Revision = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityEmailOutbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdentityEmailOutbox_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityEmailOutbox_Status_AvailableAtUtc",
                table: "IdentityEmailOutbox",
                columns: new[] { "Status", "AvailableAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityEmailOutbox_UserId_Purpose_Status",
                table: "IdentityEmailOutbox",
                columns: new[] { "UserId", "Purpose", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_IdentityEmailOutbox_PendingUserPurpose",
                table: "IdentityEmailOutbox",
                columns: new[] { "UserId", "Purpose" },
                unique: true,
                filter: "\"Status\" = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentityEmailOutbox");
        }
    }
}
