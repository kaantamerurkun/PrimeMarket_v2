using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeMarket.Migrations
{
    /// <inheritdoc />
    public partial class NewVersion556 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OfferId",
                table: "Purchases",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCounterOffer",
                table: "Offers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MessageId",
                table: "Offers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginalOfferId",
                table: "Offers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResponseDate",
                table: "Offers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Stock",
                table: "Listings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PurchaseConfirmations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseId = table.Column<int>(type: "int", nullable: false),
                    SellerShippedProduct = table.Column<bool>(type: "bit", nullable: false),
                    ShippingConfirmedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BuyerReceivedProduct = table.Column<bool>(type: "bit", nullable: false),
                    ReceiptConfirmedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentReleased = table.Column<bool>(type: "bit", nullable: false),
                    PaymentReleasedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrackingNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShippingProvider = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseConfirmations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseConfirmations_Purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_OfferId",
                table: "Purchases",
                column: "OfferId",
                unique: true,
                filter: "[OfferId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_MessageId",
                table: "Offers",
                column: "MessageId",
                unique: true,
                filter: "[MessageId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_OriginalOfferId",
                table: "Offers",
                column: "OriginalOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseConfirmations_PurchaseId",
                table: "PurchaseConfirmations",
                column: "PurchaseId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Messages_MessageId",
                table: "Offers",
                column: "MessageId",
                principalTable: "Messages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Offers_OriginalOfferId",
                table: "Offers",
                column: "OriginalOfferId",
                principalTable: "Offers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Offers_OfferId",
                table: "Purchases",
                column: "OfferId",
                principalTable: "Offers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Offers_Messages_MessageId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_Offers_OriginalOfferId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Offers_OfferId",
                table: "Purchases");

            migrationBuilder.DropTable(
                name: "PurchaseConfirmations");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_OfferId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Offers_MessageId",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_Offers_OriginalOfferId",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "OfferId",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "IsCounterOffer",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "OriginalOfferId",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "ResponseDate",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "Stock",
                table: "Listings");
        }
    }
}
