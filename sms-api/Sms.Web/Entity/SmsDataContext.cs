using Microsoft.EntityFrameworkCore;
using Sms.Web.Service;
using System;
using Sms.Web.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class SmsDataContext : DbContext
    {
        private readonly IAuthService _authService;
        private readonly IDateTimeService _dateTimeService;
        public SmsDataContext(DbContextOptions<SmsDataContext> options, IAuthService authService, IDateTimeService dateTimeService)
            : base(options)
        {
            this._authService = authService;
            _dateTimeService = dateTimeService;
        }
        public override int SaveChanges()
        {
            var userId = _authService.CurrentUserId();
            AddTimestamps(userId == null ? null : new User() { Id = userId.GetValueOrDefault() });
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var userId = _authService.CurrentUserId();
            AddTimestamps(userId == null ? null : new User() { Id = userId.GetValueOrDefault() });
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            var userId = _authService.CurrentUserId();
            AddTimestamps(userId == null ? null : new User() { Id = userId.GetValueOrDefault() });
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasMany(x => x.UserTokens)
                .WithOne(x => x.User)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<User>()
                .HasIndex(r => r.ApiKey);
            modelBuilder.Entity<UserToken>()
                .HasIndex(x => x.Token);

            modelBuilder.Entity<User>()
                .HasOne(x => x.UserProfile)
                .WithOne(x => x.User)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<User>()
                .HasAlternateKey(c => c.Username)
                .HasName("AlternateKey_Username");

            modelBuilder.Entity<SmsHistory>()
                .HasMany<OrderResult>()
                .WithOne(x => x.SmsHistory)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>()
                .HasMany<ForgotPasswordToken>()
                .WithOne(x => x.User)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<GsmDevice>()
                .HasMany(x => x.Coms)
                .WithOne(x => x.GsmDevice)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PhoneFailedCount>()
                .HasAlternateKey(c => c.GsmDeviceId)
                .HasName("AlternateKey_PhoneFailedCount_PhoneNumber");

            modelBuilder.Entity<User>()
                .HasMany<UserTransaction>()
                .WithOne(r => r.User)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<User>()
                .HasMany<Order>()
                .WithOne(r => r.User)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GsmDevice>()
                .HasMany(r => r.GsmDeviceServiceProviders)
                .WithOne(r => r.GsmDevice)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany<UserGsmDevice>(r => r.UserGsmDevices)
                .WithOne(r => r.User)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GsmDevice>()
                .HasMany<UserGsmDevice>(r => r.UserGsmDevices)
                .WithOne(r => r.GsmDevice)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ErrorPhoneLog>()
                .HasIndex(r => r.PhoneNumber);
            modelBuilder.Entity<ErrorPhoneLog>()
                .HasMany(r => r.ErrorPhoneLogOrders)
                .WithOne(r => r.ErrorPhoneLog)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<GsmDevice>()
                .HasMany<Discount>()
                .WithOne(r => r.GsmDevice)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ServiceProvider>()
                .HasMany<Discount>()
                .WithOne(r => r.ServiceProvider)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ErrorPhoneLog>()
                .HasIndex(r => new { r.IsActive, r.ServiceProviderId });

            modelBuilder.Entity<ServiceProvider>()
                .HasMany(r => r.ServiceNetworkProviders)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Com>()
                .HasIndex(r => r.ComName);

            modelBuilder.Entity<GsmDevice>()
                .HasIndex(r => r.Code);

            modelBuilder.Entity<User>()
                .HasMany<UserReferredFee>()
                .WithOne(r => r.User)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany<UserReferalFee>()
                .WithOne(r => r.User)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany<UserReferalFee>()
                .WithOne(r => r.ReferredUser)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany<NewServiceSuggestion>()
                .WithOne(r => r.User)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order>()
              .HasMany(r => r.OrderResults)
              .WithOne(r => r.Order)
              .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order>()
              .HasMany<OrderComplaint>()
              .WithOne(r => r.Order)
              .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Order>()
              .HasMany<UserTransaction>()
              .WithOne(r => r.Order)
              .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Order>()
              .HasIndex(r => r.PhoneNumber);
            modelBuilder.Entity<Com>()
              .HasIndex(r => r.PhoneNumber);
            modelBuilder.Entity<Order>()
              .HasMany<SmsHistory>()
              .WithOne(r => r.Order)
              .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<GsmDevice>()
                .HasMany(x => x.ComHistorys)
                .WithOne(x => x.GsmDevice)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<GsmDevice>()
                .HasMany(x => x.ServiceProviderPhoneNumberLiveChecks)
                .WithOne(x => x.GsmDevice)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<Order>()
                    .HasDiscriminator(r => r.OrderType)
                    .HasValue<RentCodeOrder>(OrderType.RentCode)
                    .HasValue<InternationalSimOrder>(OrderType.InternationalSim);

            modelBuilder.Entity<User>()
                .HasMany<UserBalanceSnapshot>()
                .WithOne(r => r.User)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasIndex(r => r.Status);

            modelBuilder.Entity<User>()
                .HasMany<OrderExportJob>()
                .WithOne(r => r.User)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserBalanceSnapshot>(r =>
            {
                r.HasIndex(e => new { e.UserId, e.Year, e.Month, e.Date }).IsUnique();
            });
            modelBuilder.Entity<ServiceProviderPhoneNumberLiveCheck>()
                .HasIndex(r => new { r.ServiceProviderId, r.PhoneNumber })
                .IsUnique();

            modelBuilder.Entity<GsmDevice>()
                .Property(r=>r.LastActivedAt)
                .HasDefaultValue(new DateTime(2020, 8, 1));

            base.OnModelCreating(modelBuilder);
        }

        private void AddTimestamps(User currentUser)
        {
            var entities = ChangeTracker.Entries().Where(x => x.Entity is BaseEntity && (x.State == EntityState.Added || x.State == EntityState.Modified));

            foreach (var entity in entities)
            {
                if (entity.State == EntityState.Added)
                {
                    ((BaseEntity)entity.Entity).Created = _dateTimeService.UtcNow();
                    ((BaseEntity)entity.Entity).CreatedBy = currentUser?.Id;
                    ((BaseEntity)entity.Entity).Guid = Guid.NewGuid().ToString().Split("-")[0];
                }

                ((BaseEntity)entity.Entity).Updated = _dateTimeService.UtcNow();
                ((BaseEntity)entity.Entity).UpdatedBy = currentUser?.Id;
            }
        }
        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<UserToken> UserTokens { get; set; }
        public DbSet<GsmDevice> GsmDevices { get; set; }
        public DbSet<Com> Coms { get; set; }
        public DbSet<SmsHistory> SmsHistorys { get; set; }
        public DbSet<ServiceProvider> ServiceProviders { get; set; }
        public DbSet<ServiceNetworkProvider> ServiceNetworkProviders { get; set; }
        public DbSet<NewServiceSuggestion> NewServiceSuggestions { get; set; }

        public DbSet<ForgotPasswordToken> ForgotPasswordTokens { get; set; }

        public DbSet<Order> Orders { get; set; }
        public DbSet<RentCodeOrder> RentCodeOrders { get; set; }
        public DbSet<InternationalSimOrder> InternationalSimOrders { get; set; }

        public DbSet<ProviderHistory> ProviderHistories { get; set; }

        public DbSet<UserTransaction> UserTransactions { get; set; }
        public DbSet<OrderResult> OrderResults { get; set; }
        public DbSet<OrderResultReport> OrderResultReports { get; set; }
        public DbSet<SystemConfiguration> SystemConfigurations { get; set; }
        public DbSet<OrderComplaint> OrderComplaints { get; set; }
        public DbSet<PhoneFailedCount> PhoneFailedCounts { get; set; }
        public DbSet<PhoneFailedNotification> PhoneFailedNotifications { get; set; }
        public DbSet<UserPaymentTransaction> UserPaymentTransactions { get; set; }
        public DbSet<PaymentMethodConfiguration> PaymentMethodConfigurations { get; set; }
        public DbSet<UserOfflinePaymentReceipt> UserOfflinePaymentReceipts { get; set; }
        public DbSet<OfflinePaymentNotice> OfflinePaymentNotices { get; set; }
        public DbSet<GsmDeviceServiceProvider> GsmDeviceServiceProviders { get; set; }
        public DbSet<DailyReport> DailyReports { get; set; }
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<UserGsmDevice> UserGsmDevices { get; set; }
        public DbSet<GsmReport> GsmReports { get; set; }
        public DbSet<ErrorPhoneLog> ErrorPhoneLogs { get; set; }
        public DbSet<ErrorPhoneLogUser> ErrorPhoneLogUsers { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<CheckoutRequest> CheckoutRequests { get; set; }
        public DbSet<SystemAlert> SystemAlerts { get; set; }
        public DbSet<UserReferalFee> UserReferalFees { get; set; }
        public DbSet<UserReferredFee> UserReferredFees { get; set; }
        public DbSet<UserBalanceSnapshot> UserBalanceSnapshots { get; set; }
        public DbSet<SimCountry> SimCountries { get; set; }
        public DbSet<InternationalSim> InternationalSims { get; set; }
        public DbSet<ServiceProviderPhoneNumberLiveCheck> ServiceProviderPhoneNumberLiveChecks { get; set; }
        public DbSet<ComHistory> ComHistorys { get; set; }
        public DbSet<OrderExportJob> OrderExportJobs { get; set; }
    }
}
