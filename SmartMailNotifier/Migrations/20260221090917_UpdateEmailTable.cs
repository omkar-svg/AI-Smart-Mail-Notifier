using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartMailNotifier.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEmailTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Createdat",
                table: "Emails",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "IsImportent",
                table: "Emails",
                newName: "Sender");

            migrationBuilder.RenameColumn(
                name: "Body",
                table: "Emails",
                newName: "MessageId");

            migrationBuilder.AddColumn<string>(
                name: "IsImportant",
                table: "Emails",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsImportant",
                table: "Emails");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Emails",
                newName: "Createdat");

            migrationBuilder.RenameColumn(
                name: "Sender",
                table: "Emails",
                newName: "IsImportent");

            migrationBuilder.RenameColumn(
                name: "MessageId",
                table: "Emails",
                newName: "Body");
        }
    }
}
