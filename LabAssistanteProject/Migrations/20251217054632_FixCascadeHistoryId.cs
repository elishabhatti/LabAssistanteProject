using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabAssistanteProject.Migrations
{
    /// <inheritdoc />
    public partial class FixCascadeHistoryId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HistoryId",
                table: "History",
                newName: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "History",
                newName: "HistoryId");
        }
    }
}
