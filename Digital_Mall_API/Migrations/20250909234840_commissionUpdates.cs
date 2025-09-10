using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Mall_API.Migrations
{
    /// <inheritdoc />
    public partial class commissionUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommissionRate",
                table: "FashionModels");

            migrationBuilder.DropColumn(
                name: "CommissionRate",
                table: "Brands");

            migrationBuilder.AlterColumn<string>(
                name: "BankAccountNumber",
                table: "Payouts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(34)",
                oldMaxLength: 34);

            migrationBuilder.AddColumn<decimal>(
                name: "SpecificCommissionRate",
                table: "FashionModels",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SpecificCommissionRate",
                table: "Brands",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GlobalCommission",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommissionRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalCommission", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GlobalCommission");

            migrationBuilder.DropColumn(
                name: "SpecificCommissionRate",
                table: "FashionModels");

            migrationBuilder.DropColumn(
                name: "SpecificCommissionRate",
                table: "Brands");

            migrationBuilder.AlterColumn<string>(
                name: "BankAccountNumber",
                table: "Payouts",
                type: "nvarchar(34)",
                maxLength: 34,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionRate",
                table: "FashionModels",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionRate",
                table: "Brands",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
