using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YangSpaceBackEnd.Migrations
{
    /// <inheritdoc />
    public partial class EditBookTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "Bookings");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Bookings",
                type: "int",
                nullable: false,
                comment: "Status of the Booking (Pending/InProgress/Completed/Cancelled)",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Status of the Booking (Pending/Confirmed/Cancelled)");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true,
                comment: "Optional Notes for the Booking");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Bookings",
                type: "datetime2",
                nullable: true,
                comment: "Last Updated Date for the Booking");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Bookings");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Bookings",
                type: "int",
                nullable: false,
                comment: "Status of the Booking (Pending/Confirmed/Cancelled)",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Status of the Booking (Pending/InProgress/Completed/Cancelled)");

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
