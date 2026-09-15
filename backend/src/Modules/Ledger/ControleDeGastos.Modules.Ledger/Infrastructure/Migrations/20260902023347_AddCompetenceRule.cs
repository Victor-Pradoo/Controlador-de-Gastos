using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleDeGastos.Modules.Ledger.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetenceRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "competence_closures",
                schema: "ledger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    ClosedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competence_closures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "competence_settings",
                schema: "ledger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClosingDay = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competence_settings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_competence_closures_UserId_Year_Month",
                schema: "ledger",
                table: "competence_closures",
                columns: new[] { "UserId", "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "competence_closures",
                schema: "ledger");

            migrationBuilder.DropTable(
                name: "competence_settings",
                schema: "ledger");
        }
    }
}
