﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sms.Web.Entity;

namespace Sms.Web.Migrations
{
    [DbContext(typeof(SmsDataContext))]
    [Migration("20190823020350_thuan_00070_order_id_null_able")]
    partial class thuan_00070_order_id_null_able
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.11-servicing-32099")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Sms.Web.Entity.Com", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ComName");

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<bool>("Disabled");

                    b.Property<int>("GsmDeviceId");

                    b.Property<string>("Guid");

                    b.Property<int?>("NetworkProvider");

                    b.Property<string>("PhoneNumber");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.HasKey("Id");

                    b.HasIndex("GsmDeviceId");

                    b.ToTable("Coms");
                });

            modelBuilder.Entity("Sms.Web.Entity.ForgotPasswordToken", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<string>("Guid");

                    b.Property<string>("Token");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.Property<bool>("Used");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("ForgotPasswordTokens");
                });

            modelBuilder.Entity("Sms.Web.Entity.GsmDevice", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Code");

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<bool>("Disabled");

                    b.Property<string>("Guid");

                    b.Property<string>("Name");

                    b.Property<string>("Note");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.HasKey("Id");

                    b.ToTable("GsmDevices");
                });

            modelBuilder.Entity("Sms.Web.Entity.NewServiceSuggestion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<string>("Description");

                    b.Property<string>("Guid");

                    b.Property<string>("Name");

                    b.Property<string>("Sender");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.HasKey("Id");

                    b.ToTable("NewServiceSuggestions");
                });

            modelBuilder.Entity("Sms.Web.Entity.OfflinePaymentNotice", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal>("Amount");

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<int>("ErrorReason");

                    b.Property<string>("Guid");

                    b.Property<string>("OptionalMessage");

                    b.Property<string>("ReceiptCode");

                    b.Property<string>("ServiceProvider");

                    b.Property<int?>("SolvedReceiptId");

                    b.Property<int>("Status");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.HasKey("Id");

                    b.HasIndex("SolvedReceiptId");

                    b.ToTable("OfflinePaymentNotices");
                });

            modelBuilder.Entity("Sms.Web.Entity.Order", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<DateTime?>("Expired");

                    b.Property<string>("Guid");

                    b.Property<int>("LockTime");

                    b.Property<int?>("NetworkProvider");

                    b.Property<string>("PhoneNumber");

                    b.Property<decimal>("Price");

                    b.Property<string>("RequestPhoneNumber");

                    b.Property<int>("ServiceProviderId");

                    b.Property<int>("Status");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("ServiceProviderId");

                    b.HasIndex("UserId");

                    b.ToTable("Orders");
                });

            modelBuilder.Entity("Sms.Web.Entity.OrderComplaint", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("AdminComment");

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<string>("Guid");

                    b.Property<int>("OrderComplaintStatus");

                    b.Property<int>("OrderId");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.Property<string>("UserComment");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.ToTable("OrderComplaints");
                });

            modelBuilder.Entity("Sms.Web.Entity.OrderResult", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<string>("Guid");

                    b.Property<string>("Message");

                    b.Property<int>("OrderId");

                    b.Property<string>("PhoneNumber");

                    b.Property<string>("Sender");

                    b.Property<int?>("SmsHistoryId");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("SmsHistoryId");

                    b.ToTable("OrderResults");
                });

            modelBuilder.Entity("Sms.Web.Entity.OrderResultReport", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<string>("Guid");

                    b.Property<int>("OrderResultId");

                    b.Property<int>("OrderResultReportStatus");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.HasKey("Id");

                    b.HasIndex("OrderResultId");

                    b.ToTable("OrderResultReports");
                });

            modelBuilder.Entity("Sms.Web.Entity.PaymentMethodConfiguration", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("BankAccount");

                    b.Property<string>("BankCode");

                    b.Property<string>("BankName");

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<string>("Guid");

                    b.Property<bool>("IsDisabled");

                    b.Property<string>("MessageFromAdmin");

                    b.Property<string>("Name");

                    b.Property<string>("OwnerName");

                    b.Property<int>("PaymentMethodType");

                    b.Property<string>("Sender");

                    b.Property<string>("Thumbnail");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.HasKey("Id");

                    b.ToTable("PaymentMethodConfigurations");
                });

            modelBuilder.Entity("Sms.Web.Entity.PhoneFailedCount", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("ContinuousFailed");

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<int>("GsmDeviceId");

                    b.Property<string>("Guid");

                    b.Property<int>("TotalFailed");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.HasKey("Id");

                    b.HasAlternateKey("GsmDeviceId")
                        .HasName("AlternateKey_PhoneFailedCount_PhoneNumber");

                    b.ToTable("PhoneFailedCounts");
                });

            modelBuilder.Entity("Sms.Web.Entity.PhoneFailedNotification", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("ContinuosFailed");

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<string>("GsmDeviceCode");

                    b.Property<int>("GsmDeviceId");

                    b.Property<string>("GsmDeviceName");

                    b.Property<string>("Guid");

                    b.Property<bool>("IsRead");

                    b.Property<int>("TotalFailed");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.ToTable("PhoneFailedNotifications");
                });

            modelBuilder.Entity("Sms.Web.Entity.ProviderHistory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<string>("Guid");

                    b.Property<string>("PhoneNumber");

                    b.Property<int?>("ServiceProviderId");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.HasKey("Id");

                    b.HasIndex("ServiceProviderId");

                    b.ToTable("ProviderHistories");
                });

            modelBuilder.Entity("Sms.Web.Entity.ServiceProvider", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<bool>("Disabled");

                    b.Property<string>("Guid");

                    b.Property<int>("LockTime");

                    b.Property<string>("MessageRegex");

                    b.Property<string>("Name");

                    b.Property<decimal>("Price");

                    b.Property<int>("ReceivingThreshold");

                    b.Property<int>("ServiceType");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.HasKey("Id");

                    b.ToTable("ServiceProviders");
                });

            modelBuilder.Entity("Sms.Web.Entity.SmsHistory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Content");

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<string>("Guid");

                    b.Property<string>("PhoneNumber");

                    b.Property<DateTime>("ReceivedDate");

                    b.Property<string>("Sender");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.HasKey("Id");

                    b.ToTable("SmsHistorys");
                });

            modelBuilder.Entity("Sms.Web.Entity.SystemConfiguration", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("AdminNotification");

                    b.Property<string>("BrandName");

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<string>("Email");

                    b.Property<string>("FacebookUrl");

                    b.Property<string>("Guid");

                    b.Property<string>("LogoUrl");

                    b.Property<string>("MomoAccessKey");

                    b.Property<string>("MomoApiEndPoint");

                    b.Property<string>("MomoPartnerCode");

                    b.Property<string>("MomoSecretKey");

                    b.Property<bool>("NganLuongIsLiveEnvironment");

                    b.Property<string>("NganLuongLiveMerchantCode");

                    b.Property<string>("NganLuongLiveMerchantPassword");

                    b.Property<string>("NganLuongLiveReceiver");

                    b.Property<string>("NganLuongSandboxMerchantCode");

                    b.Property<string>("NganLuongSandboxMerchantPassword");

                    b.Property<string>("NganLuongSandboxReceiver");

                    b.Property<string>("PhoneNumber");

                    b.Property<string>("TelegramUrl");

                    b.Property<int?>("ThresholdsForAutoSuspend");

                    b.Property<int?>("ThresholdsForWarning");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.Property<string>("YoutubeUrl");

                    b.HasKey("Id");

                    b.ToTable("SystemConfigurations");
                });

            modelBuilder.Entity("Sms.Web.Entity.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ApiKey");

                    b.Property<decimal>("Ballance");

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<string>("Guid");

                    b.Property<bool>("IsBanned");

                    b.Property<string>("Password");

                    b.Property<int>("Role");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.Property<string>("Username")
                        .IsRequired();

                    b.HasKey("Id");

                    b.HasAlternateKey("Username")
                        .HasName("AlternateKey_Username");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Sms.Web.Entity.UserOfflinePaymentReceipt", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal>("Amount");

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<string>("Guid");

                    b.Property<bool>("IsExpired");

                    b.Property<string>("ReceiptCode");

                    b.Property<string>("ReceiptResult");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserOfflinePaymentReceipts");
                });

            modelBuilder.Entity("Sms.Web.Entity.UserPaymentTransaction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<string>("Guid");

                    b.Property<bool>("IsExpired");

                    b.Property<long>("Money");

                    b.Property<string>("RequestId");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.ToTable("UserPaymentTransactions");
                });

            modelBuilder.Entity("Sms.Web.Entity.UserProfile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<string>("Email");

                    b.Property<string>("Guid");

                    b.Property<string>("Name");

                    b.Property<string>("PhoneNumber");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("UserProfiles");
                });

            modelBuilder.Entity("Sms.Web.Entity.UserToken", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<DateTime>("Expired");

                    b.Property<string>("Guid");

                    b.Property<string>("Token");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserTokens");
                });

            modelBuilder.Entity("Sms.Web.Entity.UserTransaction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal>("Amount");

                    b.Property<string>("Comment");

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<string>("Guid");

                    b.Property<bool>("IsImport");

                    b.Property<int?>("OrderId");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserTransactions");
                });

            modelBuilder.Entity("Sms.Web.Entity.Com", b =>
                {
                    b.HasOne("Sms.Web.Entity.GsmDevice", "GsmDevice")
                        .WithMany("Coms")
                        .HasForeignKey("GsmDeviceId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Sms.Web.Entity.ForgotPasswordToken", b =>
                {
                    b.HasOne("Sms.Web.Entity.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Sms.Web.Entity.OfflinePaymentNotice", b =>
                {
                    b.HasOne("Sms.Web.Entity.UserOfflinePaymentReceipt", "Receipt")
                        .WithMany()
                        .HasForeignKey("SolvedReceiptId");
                });

            modelBuilder.Entity("Sms.Web.Entity.Order", b =>
                {
                    b.HasOne("Sms.Web.Entity.ServiceProvider", "ServiceProvider")
                        .WithMany()
                        .HasForeignKey("ServiceProviderId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Sms.Web.Entity.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Sms.Web.Entity.OrderComplaint", b =>
                {
                    b.HasOne("Sms.Web.Entity.Order", "Order")
                        .WithMany()
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Sms.Web.Entity.OrderResult", b =>
                {
                    b.HasOne("Sms.Web.Entity.Order", "Order")
                        .WithMany("OrderResults")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Sms.Web.Entity.SmsHistory", "SmsHistory")
                        .WithMany()
                        .HasForeignKey("SmsHistoryId")
                        .OnDelete(DeleteBehavior.SetNull);
                });

            modelBuilder.Entity("Sms.Web.Entity.OrderResultReport", b =>
                {
                    b.HasOne("Sms.Web.Entity.OrderResult", "OrderResult")
                        .WithMany()
                        .HasForeignKey("OrderResultId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Sms.Web.Entity.ProviderHistory", b =>
                {
                    b.HasOne("Sms.Web.Entity.ServiceProvider", "ServiceProvider")
                        .WithMany()
                        .HasForeignKey("ServiceProviderId");
                });

            modelBuilder.Entity("Sms.Web.Entity.UserOfflinePaymentReceipt", b =>
                {
                    b.HasOne("Sms.Web.Entity.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Sms.Web.Entity.UserProfile", b =>
                {
                    b.HasOne("Sms.Web.Entity.User", "User")
                        .WithOne("UserProfile")
                        .HasForeignKey("Sms.Web.Entity.UserProfile", "UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Sms.Web.Entity.UserToken", b =>
                {
                    b.HasOne("Sms.Web.Entity.User", "User")
                        .WithMany("UserTokens")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Sms.Web.Entity.UserTransaction", b =>
                {
                    b.HasOne("Sms.Web.Entity.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
