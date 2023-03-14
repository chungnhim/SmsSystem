using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00410_transfer_fee : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExternalTransferFee",
                table: "SystemConfigurations",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InternalTransferFee",
                table: "SystemConfigurations",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalTransferFee",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "InternalTransferFee",
                table: "SystemConfigurations");
        }
    }
}
