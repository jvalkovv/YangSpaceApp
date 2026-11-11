using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YangSpace.Api.Migrations
{
    /// <inheritdoc />
    public partial class PublicationFreshnessTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Offers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAtUtc",
                table: "Offers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Offers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Jobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAtUtc",
                table: "Jobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Jobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Brochures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAtUtc",
                table: "Brochures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Brochures",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "PublishedAtUtc",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PublishedAtUtc",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Brochures");

            migrationBuilder.DropColumn(
                name: "PublishedAtUtc",
                table: "Brochures");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Brochures");
        }
    }
}
