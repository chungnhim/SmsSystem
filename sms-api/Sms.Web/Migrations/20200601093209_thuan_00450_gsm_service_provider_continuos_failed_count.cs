using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00450_gsm_service_provider_continuos_failed_count : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GsmServiceProviderContinuosFailed",
                table: "SystemConfigurations",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GsmServiceProviderContinuosFailed",
                table: "SystemConfigurations");
        }
    }
}
