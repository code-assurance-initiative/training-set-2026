using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rentals.Lending.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OneOpenLoanPerUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_loans_open_unit",
                table: "loans",
                column: "UnitId",
                unique: true,
                filter: "\"Status\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_loans_open_unit",
                table: "loans");
        }
    }
}
