using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastFoodApp.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddprotectedanddescriptiontoPriceListmodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cost",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Products");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PriceLists",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsProtected",
                table: "PriceLists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "IsProtected",
                table: "PriceLists");

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
