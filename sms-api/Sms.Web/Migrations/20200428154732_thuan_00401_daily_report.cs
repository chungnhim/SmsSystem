using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00401_daily_report : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AgentCheckout",
                table: "DailyReports",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AgentDiscount",
                table: "DailyReports",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReferalFee",
                table: "DailyReports",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReferedFee",
                table: "DailyReports",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "DailyReports",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgentCheckout",
                table: "DailyReports");

            migrationBuilder.DropColumn(
                name: "AgentDiscount",
                table: "DailyReports");

            migrationBuilder.DropColumn(
                name: "ReferalFee",
                table: "DailyReports");

            migrationBuilder.DropColumn(
                name: "ReferedFee",
                table: "DailyReports");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "DailyReports");
        }
    }
}
