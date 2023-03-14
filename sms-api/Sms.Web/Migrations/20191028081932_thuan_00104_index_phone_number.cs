using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00104_index_phone_number : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Orders",
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Coms",
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PhoneNumber",
                table: "Orders",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Coms_PhoneNumber",
                table: "Coms",
                column: "PhoneNumber");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_PhoneNumber",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Coms_PhoneNumber",
                table: "Coms");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Orders",
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Coms",
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 15,
                oldNullable: true);
        }
    }
}
