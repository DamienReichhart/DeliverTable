using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliverTableServer.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundAmountToReclamation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "Reclamation",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "Reclamation");
        }
    }
}
