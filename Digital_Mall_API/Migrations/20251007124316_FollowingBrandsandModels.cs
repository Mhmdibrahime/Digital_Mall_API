using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Mall_API.Migrations
{
    /// <inheritdoc />
    public partial class FollowingBrandsandModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FollowingBrands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BrandId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FollowedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowingBrands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FollowingBrands_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FollowingBrands_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FollowingModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FashionModelId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FollowedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowingModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FollowingModels_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FollowingModels_FashionModels_FashionModelId",
                        column: x => x.FashionModelId,
                        principalTable: "FashionModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FollowingBrands_BrandId",
                table: "FollowingBrands",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowingBrands_CustomerId_BrandId",
                table: "FollowingBrands",
                columns: new[] { "CustomerId", "BrandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FollowingModels_CustomerId_FashionModelId",
                table: "FollowingModels",
                columns: new[] { "CustomerId", "FashionModelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FollowingModels_FashionModelId",
                table: "FollowingModels",
                column: "FashionModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FollowingBrands");

            migrationBuilder.DropTable(
                name: "FollowingModels");
        }
    }
}
