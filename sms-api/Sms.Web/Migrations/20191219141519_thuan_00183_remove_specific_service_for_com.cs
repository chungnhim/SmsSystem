using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00183_remove_specific_service_for_com : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComServiceProviders");

            migrationBuilder.DropColumn(
                name: "SpecifiedService",
                table: "Coms");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SpecifiedService",
                table: "Coms",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ComServiceProviders",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ComId = table.Column<int>(nullable: false),
                    Created = table.Column<DateTime>(nullable: true),
                    CreatedBy = table.Column<int>(nullable: true),
                    Guid = table.Column<string>(nullable: true),
                    ServiceProviderId = table.Column<int>(nullable: false),
                    Updated = table.Column<DateTime>(nullable: true),
                    UpdatedBy = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComServiceProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComServiceProviders_Coms_ComId",
                        column: x => x.ComId,
                        principalTable: "Coms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComServiceProviders_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComServiceProviders_ComId",
                table: "ComServiceProviders",
                column: "ComId");

            migrationBuilder.CreateIndex(
                name: "IX_ComServiceProviders_ServiceProviderId",
                table: "ComServiceProviders",
                column: "ServiceProviderId");
        }
    }
}
