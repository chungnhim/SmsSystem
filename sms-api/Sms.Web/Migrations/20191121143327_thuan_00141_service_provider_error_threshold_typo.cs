using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00141_service_provider_error_threshold_typo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ErrorThreashold",
                table: "ServiceProviders",
                newName: "ErrorThreshold");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ErrorThreshold",
                table: "ServiceProviders",
                newName: "ErrorThreashold");
        }
    }
}
