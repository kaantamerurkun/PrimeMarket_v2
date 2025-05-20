using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeMarket.Migrations
{
    /// <inheritdoc />
    public partial class HaydeHaydeNeWwAY : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComputerAccessories_Listings_ListingId",
                table: "ComputerAccessories");

            migrationBuilder.DropForeignKey(
                name: "FK_PhoneAccessories_Listings_ListingId",
                table: "PhoneAccessories");

            migrationBuilder.DropForeignKey(
                name: "FK_TabletAccessories_Listings_ListingId",
                table: "TabletAccessories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TabletAccessories",
                table: "TabletAccessories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PhoneAccessories",
                table: "PhoneAccessories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ComputerAccessories",
                table: "ComputerAccessories");

            migrationBuilder.DropColumn(
                name: "CompatibleModels",
                table: "Monitors");

            migrationBuilder.DropColumn(
                name: "Warranty",
                table: "Monitors");

            migrationBuilder.DropColumn(
                name: "CompatibleModels",
                table: "ComputerComponents");

            migrationBuilder.DropColumn(
                name: "Warranty",
                table: "ComputerComponents");

            migrationBuilder.DropColumn(
                name: "CompatibleModels",
                table: "TabletAccessories");

            migrationBuilder.DropColumn(
                name: "Warranty",
                table: "TabletAccessories");

            migrationBuilder.DropColumn(
                name: "CompatibleModels",
                table: "ComputerAccessories");

            migrationBuilder.DropColumn(
                name: "ConnectionType",
                table: "ComputerAccessories");

            migrationBuilder.DropColumn(
                name: "Warranty",
                table: "ComputerAccessories");

            migrationBuilder.RenameTable(
                name: "TabletAccessories",
                newName: "TabletAccessorys");

            migrationBuilder.RenameTable(
                name: "PhoneAccessories",
                newName: "PhoneAccessorys");

            migrationBuilder.RenameTable(
                name: "ComputerAccessories",
                newName: "ComputerAccessorys");

            migrationBuilder.RenameIndex(
                name: "IX_TabletAccessories_ListingId",
                table: "TabletAccessorys",
                newName: "IX_TabletAccessorys_ListingId");

            migrationBuilder.RenameIndex(
                name: "IX_PhoneAccessories_ListingId",
                table: "PhoneAccessorys",
                newName: "IX_PhoneAccessorys_ListingId");

            migrationBuilder.RenameIndex(
                name: "IX_ComputerAccessories_ListingId",
                table: "ComputerAccessorys",
                newName: "IX_ComputerAccessorys_ListingId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TabletAccessorys",
                table: "TabletAccessorys",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PhoneAccessorys",
                table: "PhoneAccessorys",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ComputerAccessorys",
                table: "ComputerAccessorys",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ComputerAccessorys_Listings_ListingId",
                table: "ComputerAccessorys",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PhoneAccessorys_Listings_ListingId",
                table: "PhoneAccessorys",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TabletAccessorys_Listings_ListingId",
                table: "TabletAccessorys",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComputerAccessorys_Listings_ListingId",
                table: "ComputerAccessorys");

            migrationBuilder.DropForeignKey(
                name: "FK_PhoneAccessorys_Listings_ListingId",
                table: "PhoneAccessorys");

            migrationBuilder.DropForeignKey(
                name: "FK_TabletAccessorys_Listings_ListingId",
                table: "TabletAccessorys");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TabletAccessorys",
                table: "TabletAccessorys");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PhoneAccessorys",
                table: "PhoneAccessorys");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ComputerAccessorys",
                table: "ComputerAccessorys");

            migrationBuilder.RenameTable(
                name: "TabletAccessorys",
                newName: "TabletAccessories");

            migrationBuilder.RenameTable(
                name: "PhoneAccessorys",
                newName: "PhoneAccessories");

            migrationBuilder.RenameTable(
                name: "ComputerAccessorys",
                newName: "ComputerAccessories");

            migrationBuilder.RenameIndex(
                name: "IX_TabletAccessorys_ListingId",
                table: "TabletAccessories",
                newName: "IX_TabletAccessories_ListingId");

            migrationBuilder.RenameIndex(
                name: "IX_PhoneAccessorys_ListingId",
                table: "PhoneAccessories",
                newName: "IX_PhoneAccessories_ListingId");

            migrationBuilder.RenameIndex(
                name: "IX_ComputerAccessorys_ListingId",
                table: "ComputerAccessories",
                newName: "IX_ComputerAccessories_ListingId");

            migrationBuilder.AddColumn<string>(
                name: "CompatibleModels",
                table: "Monitors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Warranty",
                table: "Monitors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompatibleModels",
                table: "ComputerComponents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Warranty",
                table: "ComputerComponents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompatibleModels",
                table: "TabletAccessories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Warranty",
                table: "TabletAccessories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompatibleModels",
                table: "ComputerAccessories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConnectionType",
                table: "ComputerAccessories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Warranty",
                table: "ComputerAccessories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TabletAccessories",
                table: "TabletAccessories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PhoneAccessories",
                table: "PhoneAccessories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ComputerAccessories",
                table: "ComputerAccessories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ComputerAccessories_Listings_ListingId",
                table: "ComputerAccessories",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PhoneAccessories_Listings_ListingId",
                table: "PhoneAccessories",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TabletAccessories_Listings_ListingId",
                table: "TabletAccessories",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
