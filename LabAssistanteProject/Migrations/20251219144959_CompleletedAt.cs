using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabAssistanteProject.Migrations
{
    /// <inheritdoc />
    public partial class CompleletedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Requests",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Requests");
        }
    }
}
