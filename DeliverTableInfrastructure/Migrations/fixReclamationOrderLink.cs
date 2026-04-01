using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliverTableServer.Migrations
{
    /// <inheritdoc />
    public partial class fixReclamationOrderLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderId1",
                table: "Reclamation",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reclamation_OrderId1",
                table: "Reclamation",
                column: "OrderId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Reclamation_Orders_OrderId1",
                table: "Reclamation",
                column: "OrderId1",
                principalTable: "Orders",
                principalColumn: "Id");
        }
    }
}
