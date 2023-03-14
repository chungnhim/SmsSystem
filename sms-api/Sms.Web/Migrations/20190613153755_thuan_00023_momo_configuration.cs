using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00023_momo_configuration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MomoApiEndPoint",
                table: "SystemConfigurations",
                nullable: true,
                oldClrType: typeof(bool));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "MomoApiEndPoint",
                table: "SystemConfigurations",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}
