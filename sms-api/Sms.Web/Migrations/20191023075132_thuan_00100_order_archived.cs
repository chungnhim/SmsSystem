using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00100_order_archived : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderId1",
                table: "OrderResults",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderResults_OrderId1",
                table: "OrderResults",
                column: "OrderId1");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderResults_Orders_OrderId1",
                table: "OrderResults",
                column: "OrderId1",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderResults_Orders_OrderId1",
                table: "OrderResults");

            migrationBuilder.DropIndex(
                name: "IX_OrderResults_OrderId1",
                table: "OrderResults");

            migrationBuilder.DropColumn(
                name: "OrderId1",
                table: "OrderResults");
        }
    }
}
