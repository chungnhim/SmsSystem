using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00365_refer_fee_i_table : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReferFeePercent",
                table: "UserReferredFees",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReferFeePercent",
                table: "UserReferalFees",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "ReferredUserFee",
                table: "SystemConfigurations",
                nullable: false,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<decimal>(
                name: "ReferalFee",
                table: "SystemConfigurations",
                nullable: false,
                oldClrType: typeof(int));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferFeePercent",
                table: "UserReferredFees");

            migrationBuilder.DropColumn(
                name: "ReferFeePercent",
                table: "UserReferalFees");

            migrationBuilder.AlterColumn<int>(
                name: "ReferredUserFee",
                table: "SystemConfigurations",
                nullable: false,
                oldClrType: typeof(decimal));

            migrationBuilder.AlterColumn<int>(
                name: "ReferalFee",
                table: "SystemConfigurations",
                nullable: false,
                oldClrType: typeof(decimal));
        }
    }
}
