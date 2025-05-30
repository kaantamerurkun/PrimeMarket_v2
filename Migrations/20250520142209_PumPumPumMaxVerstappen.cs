﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeMarket.Migrations
{
    /// <inheritdoc />
    public partial class PumPumPumMaxVerstappen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompatibleModels",
                table: "PhoneAccessories");

            migrationBuilder.DropColumn(
                name: "Warranty",
                table: "PhoneAccessories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompatibleModels",
                table: "PhoneAccessories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Warranty",
                table: "PhoneAccessories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
