using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00363_referal_fee_management : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserReferalFees",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Guid = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: true),
                    Updated = table.Column<DateTime>(nullable: true),
                    CreatedBy = table.Column<int>(nullable: true),
                    UpdatedBy = table.Column<int>(nullable: true),
                    Day = table.Column<int>(nullable: false),
                    Month = table.Column<int>(nullable: false),
                    Year = table.Column<int>(nullable: false),
                    TotalOrderCount = table.Column<int>(nullable: false),
                    TotalCost = table.Column<decimal>(nullable: false),
                    FeeAmount = table.Column<decimal>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    ReferredUserId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReferalFees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserReferalFees_Users_ReferredUserId",
                        column: x => x.ReferredUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserReferalFees_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserReferredFees",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Guid = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: true),
                    Updated = table.Column<DateTime>(nullable: true),
                    CreatedBy = table.Column<int>(nullable: true),
                    UpdatedBy = table.Column<int>(nullable: true),
                    Day = table.Column<int>(nullable: false),
                    Month = table.Column<int>(nullable: false),
                    Year = table.Column<int>(nullable: false),
                    TotalOrderCount = table.Column<int>(nullable: false),
                    TotalCost = table.Column<decimal>(nullable: false),
                    FeeAmount = table.Column<decimal>(nullable: false),
                    UserId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReferredFees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserReferredFees_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserReferalFees_ReferredUserId",
                table: "UserReferalFees",
                column: "ReferredUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReferalFees_UserId",
                table: "UserReferalFees",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReferredFees_UserId",
                table: "UserReferredFees",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserReferalFees");

            migrationBuilder.DropTable(
                name: "UserReferredFees");
        }
    }
}
