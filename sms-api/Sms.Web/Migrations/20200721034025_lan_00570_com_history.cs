using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class lan_00570_com_history : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComHistorys",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Guid = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: true),
                    Updated = table.Column<DateTime>(nullable: true),
                    CreatedBy = table.Column<int>(nullable: true),
                    UpdatedBy = table.Column<int>(nullable: true),
                    GsmDeviceId = table.Column<int>(nullable: false),
                    ComName = table.Column<string>(maxLength: 10, nullable: true),
                    OldPhoneNumber = table.Column<string>(maxLength: 15, nullable: true),
                    NewPhoneNumber = table.Column<string>(maxLength: 15, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComHistorys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComHistorys_GsmDevices_GsmDeviceId",
                        column: x => x.GsmDeviceId,
                        principalTable: "GsmDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComHistorys_GsmDeviceId",
                table: "ComHistorys",
                column: "GsmDeviceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComHistorys");
        }
    }
}
