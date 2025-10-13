using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Mall_API.Migrations
{
    /// <inheritdoc />
    public partial class checkOutAndRefundLogicAndPromoCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PromoCodeUsages_Customers_CustomerId",
                table: "PromoCodeUsages");

            migrationBuilder.DropForeignKey(
                name: "FK_PromoCodeUsages_Orders_OrderId",
                table: "PromoCodeUsages");

            migrationBuilder.DropForeignKey(
                name: "FK_PromoCodeUsages_PromoCodes_PromoCodeId",
                table: "PromoCodeUsages");

            migrationBuilder.DropForeignKey(
                name: "FK_RefundRequests_Customers_CustomerId",
                table: "RefundRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_RefundRequests_OrderItems_OrderItemId",
                table: "RefundRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_RefundRequests_Orders_OrderId",
                table: "RefundRequests");

            migrationBuilder.DropIndex(
                name: "IX_RefundRequests_OrderItemId",
                table: "RefundRequests");

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "RefundRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BrandId",
                table: "PromoCodes",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRefunded",
                table: "OrderItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RefundRequestId",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WalletBalance",
                table: "Customers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "BrandStatistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrandId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TotalRefunds = table.Column<int>(type: "int", nullable: false),
                    TotalRefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrandStatistics_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RefundTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RefundRequestId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefundTransactions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RefundTransactions_RefundRequests_RefundRequestId",
                        column: x => x.RefundRequestId,
                        principalTable: "RefundRequests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_OrderItemId",
                table: "RefundRequests",
                column: "OrderItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodes_BrandId",
                table: "PromoCodes",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_BrandStatistics_BrandId",
                table: "BrandStatistics",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundTransactions_CustomerId",
                table: "RefundTransactions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundTransactions_RefundRequestId",
                table: "RefundTransactions",
                column: "RefundRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_PromoCodes_Brands_BrandId",
                table: "PromoCodes",
                column: "BrandId",
                principalTable: "Brands",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PromoCodeUsages_Customers_CustomerId",
                table: "PromoCodeUsages",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PromoCodeUsages_Orders_OrderId",
                table: "PromoCodeUsages",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PromoCodeUsages_PromoCodes_PromoCodeId",
                table: "PromoCodeUsages",
                column: "PromoCodeId",
                principalTable: "PromoCodes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RefundRequests_Customers_CustomerId",
                table: "RefundRequests",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RefundRequests_OrderItems_OrderItemId",
                table: "RefundRequests",
                column: "OrderItemId",
                principalTable: "OrderItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RefundRequests_Orders_OrderId",
                table: "RefundRequests",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PromoCodes_Brands_BrandId",
                table: "PromoCodes");

            migrationBuilder.DropForeignKey(
                name: "FK_PromoCodeUsages_Customers_CustomerId",
                table: "PromoCodeUsages");

            migrationBuilder.DropForeignKey(
                name: "FK_PromoCodeUsages_Orders_OrderId",
                table: "PromoCodeUsages");

            migrationBuilder.DropForeignKey(
                name: "FK_PromoCodeUsages_PromoCodes_PromoCodeId",
                table: "PromoCodeUsages");

            migrationBuilder.DropForeignKey(
                name: "FK_RefundRequests_Customers_CustomerId",
                table: "RefundRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_RefundRequests_OrderItems_OrderItemId",
                table: "RefundRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_RefundRequests_Orders_OrderId",
                table: "RefundRequests");

            migrationBuilder.DropTable(
                name: "BrandStatistics");

            migrationBuilder.DropTable(
                name: "RefundTransactions");

            migrationBuilder.DropIndex(
                name: "IX_RefundRequests_OrderItemId",
                table: "RefundRequests");

            migrationBuilder.DropIndex(
                name: "IX_PromoCodes_BrandId",
                table: "PromoCodes");

            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "BrandId",
                table: "PromoCodes");

            migrationBuilder.DropColumn(
                name: "IsRefunded",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "RefundRequestId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "WalletBalance",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_OrderItemId",
                table: "RefundRequests",
                column: "OrderItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_PromoCodeUsages_Customers_CustomerId",
                table: "PromoCodeUsages",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PromoCodeUsages_Orders_OrderId",
                table: "PromoCodeUsages",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PromoCodeUsages_PromoCodes_PromoCodeId",
                table: "PromoCodeUsages",
                column: "PromoCodeId",
                principalTable: "PromoCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RefundRequests_Customers_CustomerId",
                table: "RefundRequests",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RefundRequests_OrderItems_OrderItemId",
                table: "RefundRequests",
                column: "OrderItemId",
                principalTable: "OrderItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RefundRequests_Orders_OrderId",
                table: "RefundRequests",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
