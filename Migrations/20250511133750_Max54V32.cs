using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeMarket.Migrations
{
    /// <inheritdoc />
    public partial class Max54V32 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FaceImagePath",
                table: "VerificationDocuments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaceImagePath",
                table: "VerificationDocuments");
        }
    }
}
