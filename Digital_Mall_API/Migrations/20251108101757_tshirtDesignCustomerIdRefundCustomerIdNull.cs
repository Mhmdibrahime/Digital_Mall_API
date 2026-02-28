using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Mall_API.Migrations
{
    /// <inheritdoc />
    public partial class tshirtDesignCustomerIdRefundCustomerIdNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefundRequests_Customers_CustomerId",
                table: "RefundRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_RefundTransactions_Customers_CustomerId",
                table: "RefundTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_TshirtDesignOrders_Customers_CustomerUserId",
                table: "TshirtDesignOrders");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerUserId",
                table: "TshirtDesignOrders",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerId",
                table: "RefundTransactions",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerId",
                table: "RefundRequests",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_RefundRequests_Customers_CustomerId",
                table: "RefundRequests",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RefundTransactions_Customers_CustomerId",
                table: "RefundTransactions",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TshirtDesignOrders_Customers_CustomerUserId",
                table: "TshirtDesignOrders",
                column: "CustomerUserId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefundRequests_Customers_CustomerId",
                table: "RefundRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_RefundTransactions_Customers_CustomerId",
                table: "RefundTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_TshirtDesignOrders_Customers_CustomerUserId",
                table: "TshirtDesignOrders");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerUserId",
                table: "TshirtDesignOrders",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerId",
                table: "RefundTransactions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerId",
                table: "RefundRequests",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RefundRequests_Customers_CustomerId",
                table: "RefundRequests",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RefundTransactions_Customers_CustomerId",
                table: "RefundTransactions",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TshirtDesignOrders_Customers_CustomerUserId",
                table: "TshirtDesignOrders",
                column: "CustomerUserId",
                principalTable: "Customers",
                principalColumn: "Id");
        }
    }
}
