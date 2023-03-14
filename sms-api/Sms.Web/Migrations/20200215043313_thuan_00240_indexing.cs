using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00240_indexing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ErrorPhoneLogs_IsActive_ServiceProviderId",
                table: "ErrorPhoneLogs",
                columns: new[] { "IsActive", "ServiceProviderId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ErrorPhoneLogs_IsActive_ServiceProviderId",
                table: "ErrorPhoneLogs");
        }
    }
}
