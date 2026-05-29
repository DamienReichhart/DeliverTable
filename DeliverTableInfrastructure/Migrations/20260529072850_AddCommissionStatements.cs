using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DeliverTableInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissionStatements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CommissionRefundStatementId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommissionStatementId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommissionStatementCounters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NextNumber = table.Column<int>(type: "integer", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionStatementCounters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommissionStatements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    RecipientRestaurantId = table.Column<int>(type: "integer", nullable: false),
                    PeriodYear = table.Column<int>(type: "integer", nullable: false),
                    PeriodMonth = table.Column<int>(type: "integer", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalHt = table.Column<decimal>(type: "numeric(9,2)", nullable: false),
                    TotalVat = table.Column<decimal>(type: "numeric(9,2)", nullable: false),
                    TotalTtc = table.Column<decimal>(type: "numeric(9,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IssuerLegalSnapshotJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    RecipientSnapshotJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    RelatedStatementId = table.Column<int>(type: "integer", nullable: true),
                    RecipientEmailSnapshot = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionStatements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionStatements_CommissionStatements_RelatedStatementId",
                        column: x => x.RelatedStatementId,
                        principalTable: "CommissionStatements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommissionStatements_Restaurants_RecipientRestaurantId",
                        column: x => x.RecipientRestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommissionStatementLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommissionStatementId = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OrderCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OrderTotalAmount = table.Column<decimal>(type: "numeric(9,2)", nullable: false),
                    CommissionRateSnapshot = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    LineHt = table.Column<decimal>(type: "numeric(9,2)", nullable: false),
                    LineVat = table.Column<decimal>(type: "numeric(9,2)", nullable: false),
                    LineTtc = table.Column<decimal>(type: "numeric(9,2)", nullable: false),
                    RefundEventId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionStatementLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionStatementLines_CommissionStatements_CommissionSta~",
                        column: x => x.CommissionStatementId,
                        principalTable: "CommissionStatements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommissionStatementLines_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CommissionRefundStatementId",
                table: "Orders",
                column: "CommissionRefundStatementId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CommissionStatementId",
                table: "Orders",
                column: "CommissionStatementId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionStatementLines_CommissionStatementId",
                table: "CommissionStatementLines",
                column: "CommissionStatementId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionStatementLines_OrderId",
                table: "CommissionStatementLines",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "UX_CommissionStatementLine_RefundEventId",
                table: "CommissionStatementLines",
                column: "RefundEventId",
                unique: true,
                filter: "\"RefundEventId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionStatements_Number",
                table: "CommissionStatements",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommissionStatements_RelatedStatementId",
                table: "CommissionStatements",
                column: "RelatedStatementId");

            migrationBuilder.CreateIndex(
                name: "UX_CommissionStatement_Restaurant_Period_Invoice",
                table: "CommissionStatements",
                columns: new[] { "RecipientRestaurantId", "PeriodYear", "PeriodMonth" },
                unique: true,
                filter: "\"Kind\" = 'Invoice'");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_CommissionStatements_CommissionRefundStatementId",
                table: "Orders",
                column: "CommissionRefundStatementId",
                principalTable: "CommissionStatements",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_CommissionStatements_CommissionStatementId",
                table: "Orders",
                column: "CommissionStatementId",
                principalTable: "CommissionStatements",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.InsertData(
                table: "CommissionStatementCounters",
                columns: new[] { "Id", "NextNumber" },
                values: new object[] { 1, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CommissionStatementCounters",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_CommissionStatements_CommissionRefundStatementId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_CommissionStatements_CommissionStatementId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "CommissionStatementCounters");

            migrationBuilder.DropTable(
                name: "CommissionStatementLines");

            migrationBuilder.DropTable(
                name: "CommissionStatements");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CommissionRefundStatementId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CommissionStatementId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CommissionRefundStatementId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CommissionStatementId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Orders");
        }
    }
}
