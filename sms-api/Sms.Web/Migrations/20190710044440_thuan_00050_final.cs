using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00050_final : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AlternateKey_PhoneFailedCount_PhoneNumber",
                table: "PhoneFailedCounts");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "PhoneFailedCounts");

            migrationBuilder.AddColumn<int>(
                name: "GsmDeviceId",
                table: "PhoneFailedCounts",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddUniqueConstraint(
                name: "AlternateKey_PhoneFailedCount_PhoneNumber",
                table: "PhoneFailedCounts",
                column: "GsmDeviceId");

            migrationBuilder.CreateTable(
                name: "PhoneFailedNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Guid = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: true),
                    Updated = table.Column<DateTime>(nullable: true),
                    CreatedBy = table.Column<int>(nullable: true),
                    UpdatedBy = table.Column<int>(nullable: true),
                    UserId = table.Column<int>(nullable: false),
                    IsRead = table.Column<bool>(nullable: false),
                    TotalFailed = table.Column<int>(nullable: false),
                    ContinuosFailed = table.Column<int>(nullable: false),
                    GsmDeviceCode = table.Column<string>(nullable: true),
                    GsmDeviceName = table.Column<string>(nullable: true),
                    GsmDeviceId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneFailedNotifications", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhoneFailedNotifications");

            migrationBuilder.DropUniqueConstraint(
                name: "AlternateKey_PhoneFailedCount_PhoneNumber",
                table: "PhoneFailedCounts");

            migrationBuilder.DropColumn(
                name: "GsmDeviceId",
                table: "PhoneFailedCounts");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "PhoneFailedCounts",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddUniqueConstraint(
                name: "AlternateKey_PhoneFailedCount_PhoneNumber",
                table: "PhoneFailedCounts",
                column: "PhoneNumber");
        }
    }
}
