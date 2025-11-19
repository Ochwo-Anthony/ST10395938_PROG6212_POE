using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST10395938_POEPart2.Migrations
{
    /// <inheritdoc />
    public partial class NameForThisChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "LecturerClaims",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedBy",
                table: "LecturerClaims",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "LecturerClaims");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "LecturerClaims");
        }
    }
}
