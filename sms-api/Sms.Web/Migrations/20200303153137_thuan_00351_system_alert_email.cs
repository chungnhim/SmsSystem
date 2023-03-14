using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00351_system_alert_email : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BccEmail",
                table: "SystemConfigurations",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSent",
                table: "SystemAlerts",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BccEmail",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "IsSent",
                table: "SystemAlerts");
        }
    }
}
