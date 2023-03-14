using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00067_more_relation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserOfflinePaymentReceipts_UserId",
                table: "UserOfflinePaymentReceipts",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserOfflinePaymentReceipts_Users_UserId",
                table: "UserOfflinePaymentReceipts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserOfflinePaymentReceipts_Users_UserId",
                table: "UserOfflinePaymentReceipts");

            migrationBuilder.DropIndex(
                name: "IX_UserOfflinePaymentReceipts_UserId",
                table: "UserOfflinePaymentReceipts");
        }
    }
}
