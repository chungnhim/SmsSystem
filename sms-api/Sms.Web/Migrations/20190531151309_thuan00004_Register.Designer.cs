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
    [Migration("20190531151309_thuan00004_Register")]
    partial class thuan00004_Register
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.11-servicing-32099")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Sms.Web.Entity.GsmDevice", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Code");

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<bool>("Disabled");

                    b.Property<string>("Name");

                    b.Property<string>("Note");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.HasKey("Id");

                    b.ToTable("GsmDevices");
                });

            modelBuilder.Entity("Sms.Web.Entity.Sim", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ComName");

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<bool>("Disabled");

                    b.Property<int>("GsmDeviceId");

                    b.Property<string>("PhoneNumber");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.HasKey("Id");

                    b.HasIndex("GsmDeviceId");

                    b.ToTable("Sims");
                });

            modelBuilder.Entity("Sms.Web.Entity.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ApiKey");

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

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

            modelBuilder.Entity("Sms.Web.Entity.UserProfile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime?>("Created");

                    b.Property<int?>("CreatedBy");

                    b.Property<string>("Email");

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

                    b.Property<string>("Token");

                    b.Property<DateTime?>("Updated");

                    b.Property<int?>("UpdatedBy");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserTokens");
                });

            modelBuilder.Entity("Sms.Web.Entity.Sim", b =>
                {
                    b.HasOne("Sms.Web.Entity.GsmDevice", "GsmDevice")
                        .WithMany()
                        .HasForeignKey("GsmDeviceId")
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
#pragma warning restore 612, 618
        }
    }
}
