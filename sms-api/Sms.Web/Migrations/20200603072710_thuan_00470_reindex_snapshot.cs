using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00470_reindex_snapshot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserBalanceSnapshots_UserId",
                table: "UserBalanceSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_UserBalanceSnapshots_Year_Month_Date",
                table: "UserBalanceSnapshots");

            migrationBuilder.CreateIndex(
                name: "IX_UserBalanceSnapshots_UserId_Year_Month_Date",
                table: "UserBalanceSnapshots",
                columns: new[] { "UserId", "Year", "Month", "Date" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserBalanceSnapshots_UserId_Year_Month_Date",
                table: "UserBalanceSnapshots");

            migrationBuilder.CreateIndex(
                name: "IX_UserBalanceSnapshots_UserId",
                table: "UserBalanceSnapshots",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBalanceSnapshots_Year_Month_Date",
                table: "UserBalanceSnapshots",
                columns: new[] { "Year", "Month", "Date" },
                unique: true);
        }
    }
}
