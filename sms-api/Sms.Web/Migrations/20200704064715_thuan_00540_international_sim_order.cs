using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00540_international_sim_order : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaximumAvailableInternationSimOrder",
                table: "SystemConfigurations",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "ServiceProviderId",
                table: "Orders",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<bool>(
                name: "OnlyAcceptFreshOtp",
                table: "Orders",
                nullable: true,
                oldClrType: typeof(bool));

            migrationBuilder.AlterColumn<decimal>(
                name: "GsmDeviceProfit",
                table: "Orders",
                nullable: true,
                oldClrType: typeof(decimal));

            migrationBuilder.AlterColumn<bool>(
                name: "AllowVoiceSms",
                table: "Orders",
                nullable: true,
                oldClrType: typeof(bool));

            migrationBuilder.AddColumn<int>(
                name: "ConnectedForwarderId",
                table: "Orders",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SimCountryId",
                table: "Orders",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderType",
                table: "Orders",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SimCountries",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Guid = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: true),
                    Updated = table.Column<DateTime>(nullable: true),
                    CreatedBy = table.Column<int>(nullable: true),
                    UpdatedBy = table.Column<int>(nullable: true),
                    CountryName = table.Column<string>(nullable: false),
                    CountryCode = table.Column<string>(nullable: false),
                    PhonePrefix = table.Column<string>(nullable: false),
                    Price = table.Column<decimal>(nullable: false),
                    IsDisabled = table.Column<bool>(nullable: false),
                    LockTime = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimCountries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InternationalSims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Guid = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: true),
                    Updated = table.Column<DateTime>(nullable: true),
                    CreatedBy = table.Column<int>(nullable: true),
                    UpdatedBy = table.Column<int>(nullable: true),
                    SimCountryId = table.Column<int>(nullable: false),
                    PhoneNumber = table.Column<string>(nullable: false),
                    ForwarderId = table.Column<int>(nullable: false),
                    IsDisabled = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternationalSims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InternationalSims_Users_ForwarderId",
                        column: x => x.ForwarderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InternationalSims_SimCountries_SimCountryId",
                        column: x => x.SimCountryId,
                        principalTable: "SimCountries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SimCountryId",
                table: "Orders",
                column: "SimCountryId");

            migrationBuilder.CreateIndex(
                name: "IX_InternationalSims_ForwarderId",
                table: "InternationalSims",
                column: "ForwarderId");

            migrationBuilder.CreateIndex(
                name: "IX_InternationalSims_SimCountryId",
                table: "InternationalSims",
                column: "SimCountryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_SimCountries_SimCountryId",
                table: "Orders",
                column: "SimCountryId",
                principalTable: "SimCountries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_SimCountries_SimCountryId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "InternationalSims");

            migrationBuilder.DropTable(
                name: "SimCountries");

            migrationBuilder.DropIndex(
                name: "IX_Orders_SimCountryId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "MaximumAvailableInternationSimOrder",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "ConnectedForwarderId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SimCountryId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderType",
                table: "Orders");

            migrationBuilder.AlterColumn<int>(
                name: "ServiceProviderId",
                table: "Orders",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "OnlyAcceptFreshOtp",
                table: "Orders",
                nullable: false,
                oldClrType: typeof(bool),
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "GsmDeviceProfit",
                table: "Orders",
                nullable: false,
                oldClrType: typeof(decimal),
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "AllowVoiceSms",
                table: "Orders",
                nullable: false,
                oldClrType: typeof(bool),
                oldNullable: true);
        }
    }
}
