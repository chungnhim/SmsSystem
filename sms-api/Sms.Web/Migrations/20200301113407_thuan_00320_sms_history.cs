using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00320_sms_history : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AutoCancelOrderDuration",
                table: "SystemConfigurations",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NotMappedOrderId",
                table: "SmsHistorys",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PermanentMessageRegex",
                table: "SmsHistorys",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PermanentServiceType",
                table: "SmsHistorys",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SmsHistorys_NotMappedOrderId",
                table: "SmsHistorys",
                column: "NotMappedOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_SmsHistorys_Orders_NotMappedOrderId",
                table: "SmsHistorys",
                column: "NotMappedOrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SmsHistorys_Orders_NotMappedOrderId",
                table: "SmsHistorys");

            migrationBuilder.DropIndex(
                name: "IX_SmsHistorys_NotMappedOrderId",
                table: "SmsHistorys");

            migrationBuilder.DropColumn(
                name: "AutoCancelOrderDuration",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "NotMappedOrderId",
                table: "SmsHistorys");

            migrationBuilder.DropColumn(
                name: "PermanentMessageRegex",
                table: "SmsHistorys");

            migrationBuilder.DropColumn(
                name: "PermanentServiceType",
                table: "SmsHistorys");
        }
    }
}
