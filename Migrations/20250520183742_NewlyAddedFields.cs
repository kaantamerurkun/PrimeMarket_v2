using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeMarket.Migrations
{
    /// <inheritdoc />
    public partial class NewlyAddedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CardholderName",
                table: "Purchases",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastFourDigits",
                table: "Purchases",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress",
                table: "Purchases",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CardholderName",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "LastFourDigits",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "ShippingAddress",
                table: "Purchases");
        }
    }
}
