using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00040_some_update : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Guid",
                table: "UserTransactions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Guid",
                table: "UserTokens",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Guid",
                table: "Users",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Guid",
                table: "UserProfiles",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Guid",
                table: "UserPaymentTransactions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Guid",
                table: "SystemConfigurations",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Guid",
                table: "SmsHistorys",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Guid",
                table: "ServiceProviders",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Guid",
                table: "ProviderHistories",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Guid",
                table: "PhoneFailedCounts",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Guid",
                table: "Orders",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Guid",
                table: "OrderResults",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Guid",
                table: "OrderResultReports",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Guid",
                table: "OrderComplaints",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Guid",
                table: "NewServiceSuggestions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Guid",
                table: "GsmDevices",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Guid",
                table: "ForgotPasswordTokens",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Guid",
                table: "Coms",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Guid",
                table: "UserTransactions");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "UserTokens");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "UserPaymentTransactions");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "SmsHistorys");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "ServiceProviders");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "ProviderHistories");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "PhoneFailedCounts");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "OrderResults");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "OrderResultReports");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "OrderComplaints");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "NewServiceSuggestions");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "GsmDevices");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "ForgotPasswordTokens");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "Coms");
        }
    }
}
