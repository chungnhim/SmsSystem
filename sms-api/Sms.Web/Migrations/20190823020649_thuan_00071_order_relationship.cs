using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00071_order_relationship : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserTransactions_OrderId",
                table: "UserTransactions",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserTransactions_Orders_OrderId",
                table: "UserTransactions",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserTransactions_Orders_OrderId",
                table: "UserTransactions");

            migrationBuilder.DropIndex(
                name: "IX_UserTransactions_OrderId",
                table: "UserTransactions");
        }
    }
}
