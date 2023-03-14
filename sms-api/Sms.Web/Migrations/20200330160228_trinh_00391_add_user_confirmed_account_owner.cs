using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class trinh_00391_add_user_confirmed_account_owner : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserConfirmedAccountOwner",
                table: "UserOfflinePaymentReceipts",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserConfirmedAccountOwner",
                table: "UserOfflinePaymentReceipts");
        }
    }
}
