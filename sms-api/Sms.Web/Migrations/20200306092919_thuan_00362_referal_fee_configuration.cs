using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00362_referal_fee_configuration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PendingReferalCalculate",
                table: "Orders",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "OrderResults",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingReferalCalculate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "OrderResults");
        }
    }
}
