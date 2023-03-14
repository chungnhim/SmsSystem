using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00366_refer_fee_i_table : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Day",
                table: "UserReferredFees");

            migrationBuilder.DropColumn(
                name: "Month",
                table: "UserReferredFees");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "UserReferredFees");

            migrationBuilder.DropColumn(
                name: "Day",
                table: "UserReferalFees");

            migrationBuilder.DropColumn(
                name: "Month",
                table: "UserReferalFees");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "UserReferalFees");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReportTime",
                table: "UserReferredFees",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ReportTime",
                table: "UserReferalFees",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportTime",
                table: "UserReferredFees");

            migrationBuilder.DropColumn(
                name: "ReportTime",
                table: "UserReferalFees");

            migrationBuilder.AddColumn<int>(
                name: "Day",
                table: "UserReferredFees",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Month",
                table: "UserReferredFees",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "UserReferredFees",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Day",
                table: "UserReferalFees",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Month",
                table: "UserReferalFees",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "UserReferalFees",
                nullable: false,
                defaultValue: 0);
        }
    }
}
