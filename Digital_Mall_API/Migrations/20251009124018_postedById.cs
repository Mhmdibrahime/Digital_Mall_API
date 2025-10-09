using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Mall_API.Migrations
{
    /// <inheritdoc />
    public partial class postedById : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reels_Brands_PostedByUserId",
                table: "Reels");

            migrationBuilder.DropForeignKey(
                name: "FK_Reels_FashionModels_PostedByUserId",
                table: "Reels");

            migrationBuilder.DropForeignKey(
                name: "FK_TshirtDesignOrderImage_TshirtDesignOrders_TshirtDesignOrderId",
                table: "TshirtDesignOrderImage");

            migrationBuilder.DropForeignKey(
                name: "FK_TshirtDesignSubmissionImage_TshirtDesignSubmissions_SubmissionId",
                table: "TshirtDesignSubmissionImage");

            migrationBuilder.DropForeignKey(
                name: "FK_TshirtOrderText_TshirtDesignOrders_TshirtDesignOrderId",
                table: "TshirtOrderText");

            migrationBuilder.DropIndex(
                name: "IX_Reels_PostedByUserId",
                table: "Reels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TshirtOrderText",
                table: "TshirtOrderText");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TshirtDesignSubmissionImage",
                table: "TshirtDesignSubmissionImage");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TshirtDesignOrderImage",
                table: "TshirtDesignOrderImage");

           

            migrationBuilder.RenameTable(
                name: "TshirtOrderText",
                newName: "TshirtOrderTexts");

            migrationBuilder.RenameTable(
                name: "TshirtDesignSubmissionImage",
                newName: "TshirtDesignSubmissionImages");

            migrationBuilder.RenameTable(
                name: "TshirtDesignOrderImage",
                newName: "TshirtDesignOrderImages");

           

            migrationBuilder.RenameIndex(
                name: "IX_TshirtOrderText_TshirtDesignOrderId",
                table: "TshirtOrderTexts",
                newName: "IX_TshirtOrderTexts_TshirtDesignOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_TshirtDesignSubmissionImage_SubmissionId",
                table: "TshirtDesignSubmissionImages",
                newName: "IX_TshirtDesignSubmissionImages_SubmissionId");

            migrationBuilder.RenameIndex(
                name: "IX_TshirtDesignOrderImage_TshirtDesignOrderId",
                table: "TshirtDesignOrderImages",
                newName: "IX_TshirtDesignOrderImages_TshirtDesignOrderId");

            migrationBuilder.AlterColumn<string>(
                name: "PostedByUserType",
                table: "Reels",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "PostedByUserId",
                table: "Reels",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "PostedByBrandId",
                table: "Reels",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostedByModelId",
                table: "Reels",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TshirtOrderTexts",
                table: "TshirtOrderTexts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TshirtDesignSubmissionImages",
                table: "TshirtDesignSubmissionImages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TshirtDesignOrderImages",
                table: "TshirtDesignOrderImages",
                column: "Id");

            //migrationBuilder.CreateTable(
            //    name: "FollowingBrands",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        CustomerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
            //        BrandId = table.Column<string>(type: "nvarchar(450)", nullable: false),
            //        FollowedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_FollowingBrands", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_FollowingBrands_Brands_BrandId",
            //            column: x => x.BrandId,
            //            principalTable: "Brands",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_FollowingBrands_Customers_CustomerId",
            //            column: x => x.CustomerId,
            //            principalTable: "Customers",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "FollowingModels",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        CustomerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
            //        FashionModelId = table.Column<string>(type: "nvarchar(450)", nullable: false),
            //        FollowedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_FollowingModels", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_FollowingModels_Customers_CustomerId",
            //            column: x => x.CustomerId,
            //            principalTable: "Customers",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_FollowingModels_FashionModels_FashionModelId",
            //            column: x => x.FashionModelId,
            //            principalTable: "FashionModels",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "ReelLikes",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        ReelId = table.Column<int>(type: "int", nullable: false),
            //        CustomerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
            //        LikedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_ReelLikes", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_ReelLikes_Customers_CustomerId",
            //            column: x => x.CustomerId,
            //            principalTable: "Customers",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_ReelLikes_Reels_ReelId",
            //            column: x => x.ReelId,
            //            principalTable: "Reels",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    });

            migrationBuilder.CreateIndex(
                name: "IX_Reels_PostedByBrandId",
                table: "Reels",
                column: "PostedByBrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Reels_PostedByModelId",
                table: "Reels",
                column: "PostedByModelId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_FollowingBrands_BrandId",
            //    table: "FollowingBrands",
            //    column: "BrandId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_FollowingBrands_CustomerId",
            //    table: "FollowingBrands",
            //    column: "CustomerId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_FollowingModels_CustomerId",
            //    table: "FollowingModels",
            //    column: "CustomerId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_FollowingModels_FashionModelId",
            //    table: "FollowingModels",
            //    column: "FashionModelId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_ReelLikes_CustomerId_ReelId",
            //    table: "ReelLikes",
            //    columns: new[] { "CustomerId", "ReelId" },
            //    unique: true);

            //migrationBuilder.CreateIndex(
            //    name: "IX_ReelLikes_ReelId",
            //    table: "ReelLikes",
            //    column: "ReelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reels_Brands_PostedByBrandId",
                table: "Reels",
                column: "PostedByBrandId",
                principalTable: "Brands",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reels_FashionModels_PostedByModelId",
                table: "Reels",
                column: "PostedByModelId",
                principalTable: "FashionModels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TshirtDesignOrderImages_TshirtDesignOrders_TshirtDesignOrderId",
                table: "TshirtDesignOrderImages",
                column: "TshirtDesignOrderId",
                principalTable: "TshirtDesignOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TshirtDesignSubmissionImages_TshirtDesignSubmissions_SubmissionId",
                table: "TshirtDesignSubmissionImages",
                column: "SubmissionId",
                principalTable: "TshirtDesignSubmissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TshirtOrderTexts_TshirtDesignOrders_TshirtDesignOrderId",
                table: "TshirtOrderTexts",
                column: "TshirtDesignOrderId",
                principalTable: "TshirtDesignOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reels_Brands_PostedByBrandId",
                table: "Reels");

            migrationBuilder.DropForeignKey(
                name: "FK_Reels_FashionModels_PostedByModelId",
                table: "Reels");

            migrationBuilder.DropForeignKey(
                name: "FK_TshirtDesignOrderImages_TshirtDesignOrders_TshirtDesignOrderId",
                table: "TshirtDesignOrderImages");

            migrationBuilder.DropForeignKey(
                name: "FK_TshirtDesignSubmissionImages_TshirtDesignSubmissions_SubmissionId",
                table: "TshirtDesignSubmissionImages");

            migrationBuilder.DropForeignKey(
                name: "FK_TshirtOrderTexts_TshirtDesignOrders_TshirtDesignOrderId",
                table: "TshirtOrderTexts");

            migrationBuilder.DropTable(
                name: "FollowingBrands");

            migrationBuilder.DropTable(
                name: "FollowingModels");

            migrationBuilder.DropTable(
                name: "ReelLikes");

            migrationBuilder.DropIndex(
                name: "IX_Reels_PostedByBrandId",
                table: "Reels");

            migrationBuilder.DropIndex(
                name: "IX_Reels_PostedByModelId",
                table: "Reels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TshirtOrderTexts",
                table: "TshirtOrderTexts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TshirtDesignSubmissionImages",
                table: "TshirtDesignSubmissionImages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TshirtDesignOrderImages",
                table: "TshirtDesignOrderImages");

            migrationBuilder.DropColumn(
                name: "PostedByBrandId",
                table: "Reels");

            migrationBuilder.DropColumn(
                name: "PostedByModelId",
                table: "Reels");

            migrationBuilder.RenameTable(
                name: "TshirtOrderTexts",
                newName: "TshirtOrderText");

            migrationBuilder.RenameTable(
                name: "TshirtDesignSubmissionImages",
                newName: "TshirtDesignSubmissionImage");

            migrationBuilder.RenameTable(
                name: "TshirtDesignOrderImages",
                newName: "TshirtDesignOrderImage");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "Discounts",
                newName: "Description");

            migrationBuilder.RenameIndex(
                name: "IX_TshirtOrderTexts_TshirtDesignOrderId",
                table: "TshirtOrderText",
                newName: "IX_TshirtOrderText_TshirtDesignOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_TshirtDesignSubmissionImages_SubmissionId",
                table: "TshirtDesignSubmissionImage",
                newName: "IX_TshirtDesignSubmissionImage_SubmissionId");

            migrationBuilder.RenameIndex(
                name: "IX_TshirtDesignOrderImages_TshirtDesignOrderId",
                table: "TshirtDesignOrderImage",
                newName: "IX_TshirtDesignOrderImage_TshirtDesignOrderId");

            migrationBuilder.AlterColumn<string>(
                name: "PostedByUserType",
                table: "Reels",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PostedByUserId",
                table: "Reels",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Discounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Discounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TshirtOrderText",
                table: "TshirtOrderText",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TshirtDesignSubmissionImage",
                table: "TshirtDesignSubmissionImage",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TshirtDesignOrderImage",
                table: "TshirtDesignOrderImage",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Reels_PostedByUserId",
                table: "Reels",
                column: "PostedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reels_Brands_PostedByUserId",
                table: "Reels",
                column: "PostedByUserId",
                principalTable: "Brands",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reels_FashionModels_PostedByUserId",
                table: "Reels",
                column: "PostedByUserId",
                principalTable: "FashionModels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TshirtDesignOrderImage_TshirtDesignOrders_TshirtDesignOrderId",
                table: "TshirtDesignOrderImage",
                column: "TshirtDesignOrderId",
                principalTable: "TshirtDesignOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TshirtDesignSubmissionImage_TshirtDesignSubmissions_SubmissionId",
                table: "TshirtDesignSubmissionImage",
                column: "SubmissionId",
                principalTable: "TshirtDesignSubmissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TshirtOrderText_TshirtDesignOrders_TshirtDesignOrderId",
                table: "TshirtOrderText",
                column: "TshirtDesignOrderId",
                principalTable: "TshirtDesignOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
