using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00025_nganluong_configuration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NganLuongIsLiveEnvironment",
                table: "SystemConfigurations",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NganLuongLiveMerchantCode",
                table: "SystemConfigurations",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NganLuongLiveMerchantPassword",
                table: "SystemConfigurations",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NganLuongLiveReceiver",
                table: "SystemConfigurations",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NganLuongSandboxMerchantCode",
                table: "SystemConfigurations",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NganLuongSandboxMerchantPassword",
                table: "SystemConfigurations",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NganLuongSandboxReceiver",
                table: "SystemConfigurations",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NganLuongIsLiveEnvironment",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "NganLuongLiveMerchantCode",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "NganLuongLiveMerchantPassword",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "NganLuongLiveReceiver",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "NganLuongSandboxMerchantCode",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "NganLuongSandboxMerchantPassword",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "NganLuongSandboxReceiver",
                table: "SystemConfigurations");
        }
    }
}
