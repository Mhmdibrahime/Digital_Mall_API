using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Mall_API.Migrations
{
    /// <inheritdoc />
    public partial class designerUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TShirtSizes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TShirtSizes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TShirtStyles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TShirtStyles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TshirtTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SizeChartUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FrontImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BackImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LeftImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RightImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TshirtTemplates", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TShirtSizes");

            migrationBuilder.DropTable(
                name: "TShirtStyles");

            migrationBuilder.DropTable(
                name: "TshirtTemplates");
        }
    }
}
