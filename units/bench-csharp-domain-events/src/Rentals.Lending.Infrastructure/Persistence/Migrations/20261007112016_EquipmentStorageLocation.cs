using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rentals.Lending.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EquipmentStorageLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Location_Branch",
                table: "equipment",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location_Shelf",
                table: "equipment",
                type: "TEXT",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location_Branch",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "Location_Shelf",
                table: "equipment");
        }
    }
}
