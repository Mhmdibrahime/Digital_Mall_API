using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Mall_API.Migrations
{
    /// <inheritdoc />
    public partial class brandIdInProductDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BrandId",
                table: "ProductDiscounts",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDiscounts_BrandId",
                table: "ProductDiscounts",
                column: "BrandId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductDiscounts_Brands_BrandId",
                table: "ProductDiscounts",
                column: "BrandId",
                principalTable: "Brands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductDiscounts_Brands_BrandId",
                table: "ProductDiscounts");

            migrationBuilder.DropIndex(
                name: "IX_ProductDiscounts_BrandId",
                table: "ProductDiscounts");

            migrationBuilder.DropColumn(
                name: "BrandId",
                table: "ProductDiscounts");
        }
    }
}
