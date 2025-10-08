using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Mall_API.Migrations
{
    /// <inheritdoc />
    public partial class design : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CustomerImageUrl",
                table: "TshirtDesignOrders",
                newName: "TshirtRightImage");

            migrationBuilder.AlterColumn<string>(
                name: "FinalDesignUrl",
                table: "TshirtDesignOrders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<decimal>(
                name: "Length",
                table: "TshirtDesignOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TshirtBackImage",
                table: "TshirtDesignOrders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TshirtFrontImage",
                table: "TshirtDesignOrders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TshirtLeftImage",
                table: "TshirtDesignOrders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TshirtType",
                table: "TshirtDesignOrders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "TshirtDesignOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "TshirtDesignOrderImage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TshirtDesignOrderId = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TshirtDesignOrderImage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TshirtDesignOrderImage_TshirtDesignOrders_TshirtDesignOrderId",
                        column: x => x.TshirtDesignOrderId,
                        principalTable: "TshirtDesignOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TshirtOrderText",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TshirtDesignOrderId = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FontFamily = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FontColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FontSize = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FontStyle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TshirtOrderText", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TshirtOrderText_TshirtDesignOrders_TshirtDesignOrderId",
                        column: x => x.TshirtDesignOrderId,
                        principalTable: "TshirtDesignOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TshirtDesignOrderImage_TshirtDesignOrderId",
                table: "TshirtDesignOrderImage",
                column: "TshirtDesignOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TshirtOrderText_TshirtDesignOrderId",
                table: "TshirtOrderText",
                column: "TshirtDesignOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TshirtDesignOrderImage");

            migrationBuilder.DropTable(
                name: "TshirtOrderText");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "TshirtDesignOrders");

            migrationBuilder.DropColumn(
                name: "TshirtBackImage",
                table: "TshirtDesignOrders");

            migrationBuilder.DropColumn(
                name: "TshirtFrontImage",
                table: "TshirtDesignOrders");

            migrationBuilder.DropColumn(
                name: "TshirtLeftImage",
                table: "TshirtDesignOrders");

            migrationBuilder.DropColumn(
                name: "TshirtType",
                table: "TshirtDesignOrders");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "TshirtDesignOrders");

            migrationBuilder.RenameColumn(
                name: "TshirtRightImage",
                table: "TshirtDesignOrders",
                newName: "CustomerImageUrl");

            migrationBuilder.AlterColumn<string>(
                name: "FinalDesignUrl",
                table: "TshirtDesignOrders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
