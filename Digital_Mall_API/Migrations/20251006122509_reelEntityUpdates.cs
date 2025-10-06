using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Mall_API.Migrations
{
    /// <inheritdoc />
    public partial class reelEntityUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MuxAssetId",
                table: "Reels",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MuxPlaybackId",
                table: "Reels",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MuxUploadId",
                table: "Reels",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemporaryUploadUrl",
                table: "Reels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UploadError",
                table: "Reels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UploadStatus",
                table: "Reels",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MuxAssetId",
                table: "Reels");

            migrationBuilder.DropColumn(
                name: "MuxPlaybackId",
                table: "Reels");

            migrationBuilder.DropColumn(
                name: "MuxUploadId",
                table: "Reels");

            migrationBuilder.DropColumn(
                name: "TemporaryUploadUrl",
                table: "Reels");

            migrationBuilder.DropColumn(
                name: "UploadError",
                table: "Reels");

            migrationBuilder.DropColumn(
                name: "UploadStatus",
                table: "Reels");
        }
    }
}
