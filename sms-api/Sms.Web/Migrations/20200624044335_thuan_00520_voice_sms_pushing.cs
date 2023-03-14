using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00520_voice_sms_pushing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudioUrl",
                table: "OrderResults",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmsType",
                table: "OrderResults",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioUrl",
                table: "OrderResults");

            migrationBuilder.DropColumn(
                name: "SmsType",
                table: "OrderResults");
        }
    }
}
