using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00560_live_check_service : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NeedLiveCheckBeforeUse",
                table: "ServiceProviders",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ServiceProviderPhoneNumberLiveChecks",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Guid = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: true),
                    Updated = table.Column<DateTime>(nullable: true),
                    CreatedBy = table.Column<int>(nullable: true),
                    UpdatedBy = table.Column<int>(nullable: true),
                    ServiceProviderId = table.Column<int>(nullable: false),
                    LiveCheckStatus = table.Column<int>(nullable: false),
                    PhoneNumber = table.Column<string>(nullable: true),
                    PostedAt = table.Column<DateTime>(nullable: true),
                    ReturnedAt = table.Column<DateTime>(nullable: true),
                    CheckBy = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceProviderPhoneNumberLiveChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceProviderPhoneNumberLiveChecks_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviderPhoneNumberLiveChecks_ServiceProviderId_PhoneNumber",
                table: "ServiceProviderPhoneNumberLiveChecks",
                columns: new[] { "ServiceProviderId", "PhoneNumber" },
                unique: true,
                filter: "[PhoneNumber] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceProviderPhoneNumberLiveChecks");

            migrationBuilder.DropColumn(
                name: "NeedLiveCheckBeforeUse",
                table: "ServiceProviders");
        }
    }
}
