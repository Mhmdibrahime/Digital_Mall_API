using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Mall_API.Migrations
{
    /// <inheritdoc />
    public partial class designer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RequestDate",
                table: "TshirtDesignOrders",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "TshirtDesignSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    DesignName = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TshirtDesignSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TshirtDesignSubmissions_TshirtDesignOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "TshirtDesignOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TshirtDesignSubmissionImage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TshirtDesignSubmissionImage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TshirtDesignSubmissionImage_TshirtDesignSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "TshirtDesignSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TshirtDesignSubmissionImage_SubmissionId",
                table: "TshirtDesignSubmissionImage",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_TshirtDesignSubmissions_OrderId",
                table: "TshirtDesignSubmissions",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TshirtDesignSubmissionImage");

            migrationBuilder.DropTable(
                name: "TshirtDesignSubmissions");

            migrationBuilder.DropColumn(
                name: "RequestDate",
                table: "TshirtDesignOrders");
        }
    }
}
