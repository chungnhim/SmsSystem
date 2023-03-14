using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00066_Solved_receipt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SolvedReceiptId",
                table: "OfflinePaymentNotices",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfflinePaymentNotices_SolvedReceiptId",
                table: "OfflinePaymentNotices",
                column: "SolvedReceiptId");

            migrationBuilder.AddForeignKey(
                name: "FK_OfflinePaymentNotices_UserOfflinePaymentReceipts_SolvedReceiptId",
                table: "OfflinePaymentNotices",
                column: "SolvedReceiptId",
                principalTable: "UserOfflinePaymentReceipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OfflinePaymentNotices_UserOfflinePaymentReceipts_SolvedReceiptId",
                table: "OfflinePaymentNotices");

            migrationBuilder.DropIndex(
                name: "IX_OfflinePaymentNotices_SolvedReceiptId",
                table: "OfflinePaymentNotices");

            migrationBuilder.DropColumn(
                name: "SolvedReceiptId",
                table: "OfflinePaymentNotices");
        }
    }
}
