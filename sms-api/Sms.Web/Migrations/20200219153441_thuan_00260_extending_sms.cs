using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00260_extending_sms : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Price2",
                table: "ServiceProviders",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price3",
                table: "ServiceProviders",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price4",
                table: "ServiceProviders",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price5",
                table: "ServiceProviders",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaximunSms",
                table: "Orders",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemainingSms",
                table: "Orders",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price2",
                table: "ServiceProviders");

            migrationBuilder.DropColumn(
                name: "Price3",
                table: "ServiceProviders");

            migrationBuilder.DropColumn(
                name: "Price4",
                table: "ServiceProviders");

            migrationBuilder.DropColumn(
                name: "Price5",
                table: "ServiceProviders");

            migrationBuilder.DropColumn(
                name: "MaximunSms",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RemainingSms",
                table: "Orders");
        }
    }
}
