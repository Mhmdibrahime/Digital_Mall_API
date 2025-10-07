using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Mall_API.Migrations
{
    /// <inheritdoc />
    public partial class _ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reels_FashionModels_FashionModelId",
                table: "Reels");

            migrationBuilder.DropIndex(
                name: "IX_Reels_FashionModelId",
                table: "Reels");

            migrationBuilder.DropColumn(
                name: "FashionModelId",
                table: "Reels");

            migrationBuilder.DropColumn(
                name: "SKU",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "Style",
                table: "ProductVariants");

            migrationBuilder.AddColumn<string>(
                name: "PostedByUserType",
                table: "Reels",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Reels_Brands_PostedByUserId",
                table: "Reels",
                column: "PostedByUserId",
                principalTable: "Brands",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reels_Brands_PostedByUserId",
                table: "Reels");

            migrationBuilder.DropColumn(
                name: "PostedByUserType",
                table: "Reels");

            migrationBuilder.AddColumn<string>(
                name: "FashionModelId",
                table: "Reels",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SKU",
                table: "ProductVariants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Style",
                table: "ProductVariants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Reels_FashionModelId",
                table: "Reels",
                column: "FashionModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reels_FashionModels_FashionModelId",
                table: "Reels",
                column: "FashionModelId",
                principalTable: "FashionModels",
                principalColumn: "Id");
        }
    }
}
