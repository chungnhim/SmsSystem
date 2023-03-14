using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00012_simtocom : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sims");

            migrationBuilder.CreateTable(
                name: "Coms",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Created = table.Column<DateTime>(nullable: true),
                    Updated = table.Column<DateTime>(nullable: true),
                    CreatedBy = table.Column<int>(nullable: true),
                    UpdatedBy = table.Column<int>(nullable: true),
                    GsmDeviceId = table.Column<int>(nullable: false),
                    Disabled = table.Column<bool>(nullable: false),
                    ComName = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Coms_GsmDevices_GsmDeviceId",
                        column: x => x.GsmDeviceId,
                        principalTable: "GsmDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Coms_GsmDeviceId",
                table: "Coms",
                column: "GsmDeviceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Coms");

            migrationBuilder.CreateTable(
                name: "Sims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ComName = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: true),
                    CreatedBy = table.Column<int>(nullable: true),
                    Disabled = table.Column<bool>(nullable: false),
                    GsmDeviceId = table.Column<int>(nullable: false),
                    PhoneNumber = table.Column<string>(nullable: true),
                    Updated = table.Column<DateTime>(nullable: true),
                    UpdatedBy = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sims_GsmDevices_GsmDeviceId",
                        column: x => x.GsmDeviceId,
                        principalTable: "GsmDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sims_GsmDeviceId",
                table: "Sims",
                column: "GsmDeviceId");
        }
    }
}
