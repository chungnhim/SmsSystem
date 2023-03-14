using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00180_gsm_priority : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "GsmDevices",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "GsmDevices");
        }
    }
}
