using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DeliverTableServer.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationUserCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerProfiles_Users_UserId",
                table: "CustomerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CustomerProfiles_UserId",
                table: "CustomerProfiles");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "CustomerProfiles");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "CustomerProfiles",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerProfiles_Users_Id",
                table: "CustomerProfiles",
                column: "Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerProfiles_Users_Id",
                table: "CustomerProfiles");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "CustomerProfiles",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "CustomerProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerProfiles_UserId",
                table: "CustomerProfiles",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerProfiles_Users_UserId",
                table: "CustomerProfiles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
