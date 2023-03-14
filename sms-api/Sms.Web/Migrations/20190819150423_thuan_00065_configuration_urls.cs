using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00065_configuration_urls : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TelegramUrl",
                table: "SystemConfigurations",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YoutubeUrl",
                table: "SystemConfigurations",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TelegramUrl",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "YoutubeUrl",
                table: "SystemConfigurations");
        }
    }
}
