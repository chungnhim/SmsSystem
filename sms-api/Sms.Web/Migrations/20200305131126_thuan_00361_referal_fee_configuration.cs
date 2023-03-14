using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00361_referal_fee_configuration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReferalFee",
                table: "SystemConfigurations",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReferredUserFee",
                table: "SystemConfigurations",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferalFee",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "ReferredUserFee",
                table: "SystemConfigurations");
        }
    }
}
