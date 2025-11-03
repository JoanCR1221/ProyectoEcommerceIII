using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoEcommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddBuyPaidAndEmployeeNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Buys_Employees_EmployeeId",
                table: "Buys");

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "Buys",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "Paid",
                table: "Buys",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Buys_Employees_EmployeeId",
                table: "Buys",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Buys_Employees_EmployeeId",
                table: "Buys");

            migrationBuilder.DropColumn(
                name: "Paid",
                table: "Buys");

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "Buys",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Buys_Employees_EmployeeId",
                table: "Buys",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
