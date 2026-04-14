using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DeliverTableInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStripePaymentsCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "LoyaltyTransactions",
                type: "text",
                nullable: false,
                defaultValue: "Committed");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "LoyaltyTransactions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "Committed");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "DiscountCodeRedemptions",
                type: "text",
                nullable: false,
                defaultValue: "Committed");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DiscountCodeRedemptions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "Committed");

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "AspNetUsers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProcessedStripeEvents",
                columns: table => new
                {
                    StripeEventId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedStripeEvents", x => x.StripeEventId);
                });

            migrationBuilder.CreateTable(
                name: "Refunds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PaymentId = table.Column<int>(type: "integer", nullable: false),
                    StripeRefundId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(9,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "EUR"),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Refunds_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Refunds_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_CreatedByUserId",
                table: "Refunds",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_PaymentId",
                table: "Refunds",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_StripeRefundId",
                table: "Refunds",
                column: "StripeRefundId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedStripeEvents");

            migrationBuilder.DropTable(
                name: "Refunds");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "LoyaltyTransactions",
                type: "text",
                nullable: false,
                defaultValue: "Committed",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "LoyaltyTransactions");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DiscountCodeRedemptions",
                type: "text",
                nullable: false,
                defaultValue: "Committed",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "DiscountCodeRedemptions");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "AspNetUsers");
        }
    }
}
