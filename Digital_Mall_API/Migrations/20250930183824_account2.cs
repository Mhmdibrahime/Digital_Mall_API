using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Mall_API.Migrations
{
    /// <inheritdoc />
    public partial class account2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Facebook",
                table: "Brands",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Instgram",
                table: "Brands",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Ofline",
                table: "Brands",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Online",
                table: "Brands",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Facebook",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "Instgram",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "Ofline",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "Online",
                table: "Brands");
        }
    }
}
