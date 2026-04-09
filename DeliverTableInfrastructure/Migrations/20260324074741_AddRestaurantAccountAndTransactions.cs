using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DeliverTableInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantAccountAndTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Balance",
                table: "Restaurants",
                type: "numeric(9,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "RestaurantTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RestaurantId = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(9,2)", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "numeric(9,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(9,2)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(9,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantTransactions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RestaurantTransactions_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTransactions_OrderId",
                table: "RestaurantTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTransactions_RestaurantId",
                table: "RestaurantTransactions",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTransactions_Type",
                table: "RestaurantTransactions",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestaurantTransactions");

            migrationBuilder.DropColumn(
                name: "Balance",
                table: "Restaurants");
        }
    }
}
