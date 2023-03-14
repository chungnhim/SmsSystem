using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00230_perfect_money : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayeeAccount",
                table: "SystemConfigurations",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayeeName",
                table: "SystemConfigurations",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayeeSecretKey",
                table: "SystemConfigurations",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayeeAccount",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "PayeeName",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "PayeeSecretKey",
                table: "SystemConfigurations");
        }
    }
}
