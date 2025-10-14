using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Mall_API.Migrations
{
    /// <inheritdoc />
    public partial class droprealtionbetweenlogsandreels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WebhookLogs_Reels_ReelId",
                table: "WebhookLogs");

            migrationBuilder.DropIndex(
                name: "IX_WebhookLogs_ReelId",
                table: "WebhookLogs");

            migrationBuilder.DropColumn(
                name: "ReelId",
                table: "WebhookLogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReelId",
                table: "WebhookLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebhookLogs_ReelId",
                table: "WebhookLogs",
                column: "ReelId");

            migrationBuilder.AddForeignKey(
                name: "FK_WebhookLogs_Reels_ReelId",
                table: "WebhookLogs",
                column: "ReelId",
                principalTable: "Reels",
                principalColumn: "Id");
        }
    }
}
