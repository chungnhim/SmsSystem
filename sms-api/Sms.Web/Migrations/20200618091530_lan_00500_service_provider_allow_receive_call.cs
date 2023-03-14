using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class lan_00500_service_provider_allow_receive_call : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowReceiveCall",
                table: "ServiceProviders",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceReceiveCall",
                table: "ServiceProviders",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowReceiveCall",
                table: "ServiceProviders");

            migrationBuilder.DropColumn(
                name: "PriceReceiveCall",
                table: "ServiceProviders");
        }
    }
}
