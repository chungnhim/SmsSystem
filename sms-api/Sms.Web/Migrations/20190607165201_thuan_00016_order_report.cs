using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00016_order_report : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderResults",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Created = table.Column<DateTime>(nullable: true),
                    Updated = table.Column<DateTime>(nullable: true),
                    CreatedBy = table.Column<int>(nullable: true),
                    UpdatedBy = table.Column<int>(nullable: true),
                    OrderId = table.Column<int>(nullable: false),
                    Message = table.Column<string>(nullable: true),
                    SmsHistoryId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderResults_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderResults_SmsHistorys_SmsHistoryId",
                        column: x => x.SmsHistoryId,
                        principalTable: "SmsHistorys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderResultReports",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Created = table.Column<DateTime>(nullable: true),
                    Updated = table.Column<DateTime>(nullable: true),
                    CreatedBy = table.Column<int>(nullable: true),
                    UpdatedBy = table.Column<int>(nullable: true),
                    OrderResultReportStatus = table.Column<int>(nullable: false),
                    OrderResultId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderResultReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderResultReports_OrderResults_OrderResultId",
                        column: x => x.OrderResultId,
                        principalTable: "OrderResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderResultReports_OrderResultId",
                table: "OrderResultReports",
                column: "OrderResultId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderResults_OrderId",
                table: "OrderResults",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderResults_SmsHistoryId",
                table: "OrderResults",
                column: "SmsHistoryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderResultReports");

            migrationBuilder.DropTable(
                name: "OrderResults");
        }
    }
}
