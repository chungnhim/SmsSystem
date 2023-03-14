using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00360_referal_code : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ReferEnabled",
                table: "Users",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReferalCode",
                table: "Users",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReferalId",
                table: "Users",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ReferalCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ReferalId",
                table: "Users");
        }
    }
}
