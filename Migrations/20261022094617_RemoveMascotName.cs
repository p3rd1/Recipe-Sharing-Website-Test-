using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookingBaby.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMascotName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MascotName",
                table: "Categories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MascotName",
                table: "Categories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 1,
                column: "MascotName",
                value: "Bé Gái Khăn Cam");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 2,
                column: "MascotName",
                value: "Bé Trai Tạp Dề Vàng");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 3,
                column: "MascotName",
                value: "Bé Gái Khăn Hồng");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 4,
                column: "MascotName",
                value: "Bé Trai Khăn Xanh");
        }
    }
}
