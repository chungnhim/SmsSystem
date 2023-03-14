using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00212_staff_checkout_request : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankAccountName",
                table: "CheckoutRequests",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "CheckoutRequests",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankCode",
                table: "CheckoutRequests",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountName",
                table: "CheckoutRequests");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "CheckoutRequests");

            migrationBuilder.DropColumn(
                name: "BankCode",
                table: "CheckoutRequests");
        }
    }
}
