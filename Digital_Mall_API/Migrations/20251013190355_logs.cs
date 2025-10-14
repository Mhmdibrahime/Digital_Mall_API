using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Mall_API.Migrations
{
    /// <inheritdoc />
    public partial class logs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WebhookLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LogLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WebhookType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReelId = table.Column<int>(type: "int", nullable: true),
                    MuxAssetId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestHeaders = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RequestBody = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebhookLogs_Reels_ReelId",
                        column: x => x.ReelId,
                        principalTable: "Reels",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookLogs_LogDate",
                table: "WebhookLogs",
                column: "LogDate");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookLogs_LogLevel",
                table: "WebhookLogs",
                column: "LogLevel");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookLogs_ReelId",
                table: "WebhookLogs",
                column: "ReelId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookLogs_WebhookType",
                table: "WebhookLogs",
                column: "WebhookType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebhookLogs");
        }
    }
}
