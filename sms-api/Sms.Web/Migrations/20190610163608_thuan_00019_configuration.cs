using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00019_configuration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BrandName",
                table: "SystemConfigurations",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "SystemConfigurations",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacebookUrl",
                table: "SystemConfigurations",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "SystemConfigurations",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "SystemConfigurations",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ThresholdsForWarning",
                table: "SystemConfigurations",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrandName",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "FacebookUrl",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "ThresholdsForWarning",
                table: "SystemConfigurations");
        }
    }
}
