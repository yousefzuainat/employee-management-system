using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YousefZuaianatAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSalaryAndDeductions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Deductions",
                table: "Users",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Salary",
                table: "Users",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deductions",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Salary",
                table: "Users");
        }
    }
}
