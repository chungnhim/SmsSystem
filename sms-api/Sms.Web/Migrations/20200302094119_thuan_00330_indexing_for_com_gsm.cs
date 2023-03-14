using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00330_indexing_for_com_gsm : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "GsmDevices",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ComName",
                table: "Coms",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GsmDevices_Code",
                table: "GsmDevices",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Coms_ComName",
                table: "Coms",
                column: "ComName");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GsmDevices_Code",
                table: "GsmDevices");

            migrationBuilder.DropIndex(
                name: "IX_Coms_ComName",
                table: "Coms");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "GsmDevices",
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ComName",
                table: "Coms",
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 10,
                oldNullable: true);
        }
    }
}
