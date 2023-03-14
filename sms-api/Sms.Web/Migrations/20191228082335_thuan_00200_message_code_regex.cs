using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00200_message_code_regex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MessageCodeRegex",
                table: "ServiceProviders",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageCodeRegex",
                table: "ServiceProviders");
        }
    }
}
