using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeMarket.Migrations
{
    /// <inheritdoc />
    public partial class AddedUserRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SellerRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BuyerId = table.Column<int>(type: "int", nullable: false),
                    SellerId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PurchaseId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerRatings_Purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SellerRatings_Users_BuyerId",
                        column: x => x.BuyerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SellerRatings_Users_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SellerRatings_BuyerId_SellerId_PurchaseId",
                table: "SellerRatings",
                columns: new[] { "BuyerId", "SellerId", "PurchaseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SellerRatings_PurchaseId",
                table: "SellerRatings",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerRatings_SellerId",
                table: "SellerRatings",
                column: "SellerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SellerRatings");
        }
    }
}
