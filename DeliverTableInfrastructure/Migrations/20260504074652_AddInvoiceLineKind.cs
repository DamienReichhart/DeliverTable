using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliverTableInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceLineKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "InvoiceLines",
                type: "text",
                nullable: false,
                defaultValue: "Item");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kind",
                table: "InvoiceLines");
        }
    }
}
