using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeMarket.Migrations
{
    /// <inheritdoc />
    public partial class NewVersion9999 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Purchases",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Purchases");
        }
    }
}
