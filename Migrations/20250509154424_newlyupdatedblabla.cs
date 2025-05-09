using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeMarket.Migrations
{
    /// <inheritdoc />
    public partial class newlyupdatedblabla : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Purchases_ListingId",
                table: "Purchases");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_ListingId",
                table: "Purchases",
                column: "ListingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Purchases_ListingId",
                table: "Purchases");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_ListingId",
                table: "Purchases",
                column: "ListingId",
                unique: true);
        }
    }
}
