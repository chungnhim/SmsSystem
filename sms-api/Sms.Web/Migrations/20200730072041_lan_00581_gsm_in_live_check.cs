using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class lan_00581_gsm_in_live_check : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GsmDeviceId",
                table: "ServiceProviderPhoneNumberLiveChecks",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviderPhoneNumberLiveChecks_GsmDeviceId",
                table: "ServiceProviderPhoneNumberLiveChecks",
                column: "GsmDeviceId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceProviderPhoneNumberLiveChecks_GsmDevices_GsmDeviceId",
                table: "ServiceProviderPhoneNumberLiveChecks",
                column: "GsmDeviceId",
                principalTable: "GsmDevices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceProviderPhoneNumberLiveChecks_GsmDevices_GsmDeviceId",
                table: "ServiceProviderPhoneNumberLiveChecks");

            migrationBuilder.DropIndex(
                name: "IX_ServiceProviderPhoneNumberLiveChecks_GsmDeviceId",
                table: "ServiceProviderPhoneNumberLiveChecks");

            migrationBuilder.DropColumn(
                name: "GsmDeviceId",
                table: "ServiceProviderPhoneNumberLiveChecks");
        }
    }
}
