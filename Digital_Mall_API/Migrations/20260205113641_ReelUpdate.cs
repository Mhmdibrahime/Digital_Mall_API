using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Mall_API.Migrations
{
    /// <inheritdoc />
    public partial class ReelUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VimeoEmbedHtml",
                table: "Reels",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VimeoPlayerUrl",
                table: "Reels",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VimeoUploadUrl",
                table: "Reels",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VimeoVideoId",
                table: "Reels",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VimeoEmbedHtml",
                table: "Reels");

            migrationBuilder.DropColumn(
                name: "VimeoPlayerUrl",
                table: "Reels");

            migrationBuilder.DropColumn(
                name: "VimeoUploadUrl",
                table: "Reels");

            migrationBuilder.DropColumn(
                name: "VimeoVideoId",
                table: "Reels");
        }
    }
}
