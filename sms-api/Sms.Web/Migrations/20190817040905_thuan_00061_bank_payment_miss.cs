using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sms.Web.Migrations
{
    public partial class thuan_00061_bank_payment_miss : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentMethodConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Guid = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: true),
                    Updated = table.Column<DateTime>(nullable: true),
                    CreatedBy = table.Column<int>(nullable: true),
                    UpdatedBy = table.Column<int>(nullable: true),
                    BankCode = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    IsDisabled = table.Column<bool>(nullable: false),
                    MessageFromAdmin = table.Column<string>(nullable: true),
                    Sender = table.Column<string>(nullable: true),
                    BankAccount = table.Column<string>(nullable: true),
                    OwnerName = table.Column<string>(nullable: true),
                    BankName = table.Column<string>(nullable: true),
                    PaymentMethodType = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethodConfigurations", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentMethodConfigurations");
        }
    }
}
