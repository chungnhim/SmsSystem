using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00490_sms_history_with_audio : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudioUrl",
                table: "SmsHistorys",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmsType",
                table: "SmsHistorys",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioUrl",
                table: "SmsHistorys");

            migrationBuilder.DropColumn(
                name: "SmsType",
                table: "SmsHistorys");
        }
    }
}
