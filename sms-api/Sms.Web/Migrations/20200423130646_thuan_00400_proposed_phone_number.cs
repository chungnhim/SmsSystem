using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00400_proposed_phone_number : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Idle",
                table: "Coms");

            migrationBuilder.AddColumn<string>(
                name: "ProposedGsm512RangeName",
                table: "Orders",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposedPhoneNumber",
                table: "Orders",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProposedGsm512RangeName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ProposedPhoneNumber",
                table: "Orders");

            migrationBuilder.AddColumn<bool>(
                name: "Idle",
                table: "Coms",
                nullable: false,
                defaultValue: false);
        }
    }
}
