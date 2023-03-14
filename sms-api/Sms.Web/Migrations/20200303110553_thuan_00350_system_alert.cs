using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00350_system_alert : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FloatingOrderWarning",
                table: "SystemConfigurations",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SystemAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Guid = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: true),
                    Updated = table.Column<DateTime>(nullable: true),
                    CreatedBy = table.Column<int>(nullable: true),
                    UpdatedBy = table.Column<int>(nullable: true),
                    Topic = table.Column<string>(maxLength: 20, nullable: true),
                    Thread = table.Column<string>(maxLength: 20, nullable: true),
                    DetailJson = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemAlerts", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemAlerts");

            migrationBuilder.DropColumn(
                name: "FloatingOrderWarning",
                table: "SystemConfigurations");
        }
    }
}
