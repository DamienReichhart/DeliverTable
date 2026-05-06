using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliverTableInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OwnerRestaurantCompanyFieldsShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "RestaurantOwners");

            migrationBuilder.DropColumn(
                name: "VatNumber",
                table: "RestaurantOwners");

            migrationBuilder.AddColumn<string>(
                name: "VatNumber",
                table: "Restaurants",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VatNumber",
                table: "Restaurants");

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "RestaurantOwners",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VatNumber",
                table: "RestaurantOwners",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
