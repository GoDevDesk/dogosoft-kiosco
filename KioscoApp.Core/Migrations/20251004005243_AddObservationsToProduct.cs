using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KioscoApp.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddObservationsToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Observations",
                table: "Products",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Observations",
                table: "Products");
        }
    }
}
