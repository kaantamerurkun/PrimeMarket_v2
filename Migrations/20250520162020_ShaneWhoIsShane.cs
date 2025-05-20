using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeMarket.Migrations
{
    /// <inheritdoc />
    public partial class ShaneWhoIsShane : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HeadphonesEarphones_Listings_ListingId",
                table: "HeadphonesEarphones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HeadphonesEarphones",
                table: "HeadphonesEarphones");

            migrationBuilder.RenameTable(
                name: "HeadphonesEarphones",
                newName: "HeadphoneEarphones");

            migrationBuilder.RenameIndex(
                name: "IX_HeadphonesEarphones_ListingId",
                table: "HeadphoneEarphones",
                newName: "IX_HeadphoneEarphones_ListingId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HeadphoneEarphones",
                table: "HeadphoneEarphones",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HeadphoneEarphones_Listings_ListingId",
                table: "HeadphoneEarphones",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HeadphoneEarphones_Listings_ListingId",
                table: "HeadphoneEarphones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HeadphoneEarphones",
                table: "HeadphoneEarphones");

            migrationBuilder.RenameTable(
                name: "HeadphoneEarphones",
                newName: "HeadphonesEarphones");

            migrationBuilder.RenameIndex(
                name: "IX_HeadphoneEarphones_ListingId",
                table: "HeadphonesEarphones",
                newName: "IX_HeadphonesEarphones_ListingId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HeadphonesEarphones",
                table: "HeadphonesEarphones",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HeadphonesEarphones_Listings_ListingId",
                table: "HeadphonesEarphones",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
