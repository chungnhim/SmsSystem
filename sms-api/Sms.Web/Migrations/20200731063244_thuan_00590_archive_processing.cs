using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00590_archive_processing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowArchiveOrder",
                table: "SystemConfigurations",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowArchiveUserTransaction",
                table: "SystemConfigurations",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowArchiveOrder",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "AllowArchiveUserTransaction",
                table: "SystemConfigurations");
        }
    }
}
