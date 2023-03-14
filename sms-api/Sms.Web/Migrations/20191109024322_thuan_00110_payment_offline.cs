using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00110_payment_offline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentMethodConfigurationId",
                table: "UserOfflinePaymentReceipts",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserOfflinePaymentReceipts_PaymentMethodConfigurationId",
                table: "UserOfflinePaymentReceipts",
                column: "PaymentMethodConfigurationId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserOfflinePaymentReceipts_PaymentMethodConfigurations_PaymentMethodConfigurationId",
                table: "UserOfflinePaymentReceipts",
                column: "PaymentMethodConfigurationId",
                principalTable: "PaymentMethodConfigurations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserOfflinePaymentReceipts_PaymentMethodConfigurations_PaymentMethodConfigurationId",
                table: "UserOfflinePaymentReceipts");

            migrationBuilder.DropIndex(
                name: "IX_UserOfflinePaymentReceipts_PaymentMethodConfigurationId",
                table: "UserOfflinePaymentReceipts");

            migrationBuilder.DropColumn(
                name: "PaymentMethodConfigurationId",
                table: "UserOfflinePaymentReceipts");
        }
    }
}
