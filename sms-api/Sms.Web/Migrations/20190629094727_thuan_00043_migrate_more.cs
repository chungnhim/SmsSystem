using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00043_migrate_more : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderResults_SmsHistorys_SmsHistoryId",
                table: "OrderResults");

            migrationBuilder.AlterColumn<int>(
                name: "SmsHistoryId",
                table: "OrderResults",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "OrderResults",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderResults_SmsHistorys_SmsHistoryId",
                table: "OrderResults",
                column: "SmsHistoryId",
                principalTable: "SmsHistorys",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderResults_SmsHistorys_SmsHistoryId",
                table: "OrderResults");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "OrderResults");

            migrationBuilder.AlterColumn<int>(
                name: "SmsHistoryId",
                table: "OrderResults",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderResults_SmsHistorys_SmsHistoryId",
                table: "OrderResults",
                column: "SmsHistoryId",
                principalTable: "SmsHistorys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
