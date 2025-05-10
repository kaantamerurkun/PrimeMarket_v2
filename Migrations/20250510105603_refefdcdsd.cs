using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeMarket.Migrations
{
    /// <inheritdoc />
    public partial class refefdcdsd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Comment",
                table: "SellerRatings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "SellerRatings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
