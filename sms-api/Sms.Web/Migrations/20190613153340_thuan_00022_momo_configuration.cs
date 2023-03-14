using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00022_momo_configuration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MomoAccessKey",
                table: "SystemConfigurations",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MomoApiEndPoint",
                table: "SystemConfigurations",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MomoPartnerCode",
                table: "SystemConfigurations",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MomoSecretKey",
                table: "SystemConfigurations",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MomoAccessKey",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "MomoApiEndPoint",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "MomoPartnerCode",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "MomoSecretKey",
                table: "SystemConfigurations");
        }
    }
}
