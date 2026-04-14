using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliverTableInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailJobAttachmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentFilename",
                table: "EmailJobs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentStoragePath",
                table: "EmailJobs",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentFilename",
                table: "EmailJobs");

            migrationBuilder.DropColumn(
                name: "AttachmentStoragePath",
                table: "EmailJobs");
        }
    }
}
