using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartMailNotifier.Migrations
{
    /// <inheritdoc />
    public partial class AddGmailRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GmailRefreshToken",
                table: "Users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GmailRefreshToken",
                table: "Users");
        }
    }
}
