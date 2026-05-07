using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DeliverTableInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReclamation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reclamation",
                columns: table => new
                {
                    ReclamationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    RefundAmount = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reclamation", x => x.ReclamationId);
                    table.ForeignKey(
                        name: "FK_Reclamation_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReclamationItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderItemId = table.Column<int>(type: "integer", nullable: false),
                    ReclamationId = table.Column<int>(type: "integer", nullable: false),
                    HasAttachedImage = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReclamationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReclamationItems_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReclamationItems_Reclamation_ReclamationId",
                        column: x => x.ReclamationId,
                        principalTable: "Reclamation",
                        principalColumn: "ReclamationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reclamation_OrderId",
                table: "Reclamation",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ReclamationItems_OrderItemId",
                table: "ReclamationItems",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ReclamationItems_ReclamationId",
                table: "ReclamationItems",
                column: "ReclamationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReclamationItems");

            migrationBuilder.DropTable(
                name: "Reclamation");
        }
    }
}
