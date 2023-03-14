using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00331_indexing_user : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ApiKey",
                table: "Users",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ApiKey",
                table: "Users",
                column: "ApiKey");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_ApiKey",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "ApiKey",
                table: "Users",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}
