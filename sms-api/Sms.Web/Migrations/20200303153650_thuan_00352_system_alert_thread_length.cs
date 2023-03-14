using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00352_system_alert_thread_length : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Thread",
                table: "SystemAlerts",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 20,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Thread",
                table: "SystemAlerts",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 40,
                oldNullable: true);
        }
    }
}
