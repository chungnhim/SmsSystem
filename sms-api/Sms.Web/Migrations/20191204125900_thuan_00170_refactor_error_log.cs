using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00170_refactor_error_log : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ErrorPhoneLogs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Guid = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: true),
                    Updated = table.Column<DateTime>(nullable: true),
                    CreatedBy = table.Column<int>(nullable: true),
                    UpdatedBy = table.Column<int>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    IsActive = table.Column<bool>(nullable: false),
                    ServiceProviderId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorPhoneLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErrorPhoneLogUsers",
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
                    ErrorPhoneLogId = table.Column<int>(nullable: false),
                    IsIgnored = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorPhoneLogUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ErrorPhoneLogUsers_ErrorPhoneLogs_ErrorPhoneLogId",
                        column: x => x.ErrorPhoneLogId,
                        principalTable: "ErrorPhoneLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ErrorPhoneLogUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorPhoneLogs_PhoneNumber",
                table: "ErrorPhoneLogs",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorPhoneLogUsers_ErrorPhoneLogId",
                table: "ErrorPhoneLogUsers",
                column: "ErrorPhoneLogId");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorPhoneLogUsers_UserId",
                table: "ErrorPhoneLogUsers",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErrorPhoneLogUsers");

            migrationBuilder.DropTable(
                name: "ErrorPhoneLogs");
        }
    }
}
