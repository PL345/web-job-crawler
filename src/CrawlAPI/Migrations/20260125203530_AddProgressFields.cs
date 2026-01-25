using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrawlAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddProgressFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentUrl",
                table: "CrawlJobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PagesProcessed",
                table: "CrawlJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentUrl",
                table: "CrawlJobs");

            migrationBuilder.DropColumn(
                name: "PagesProcessed",
                table: "CrawlJobs");
        }
    }
}
