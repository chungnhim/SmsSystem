using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Sms.Web.Entity;
using Sms.Web.Helpers;
using Sms.Web.Middleware.Filters;
using Sms.Web.Models;
using Sms.Web.Service;

namespace Sms.Web.Controllers
{
    [AllowAnonymous]
    [ServiceFilter(typeof(DevelopmentOnly))]
    public class DevSeedController : ControllerBase
    {
        private readonly SmsDataContext _smsDataContext;
        private readonly ISystemAlertService _systemAlertService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IUserService _userService;
        private readonly IAuthService _authService;
        private readonly IOrderComplaintService _orderComplaintService;
        private readonly IOrderService _orderService;
        private readonly IInternationalSimService _internationalSimService;
        public DevSeedController(SmsDataContext smsDataContext,
            ISystemAlertService systemAlertService,
            IDateTimeService dateTimeService,
            IUserService userService,
            IAuthService authService,
            IOrderService orderService,
            IInternationalSimService internationalSimService,
            IOrderComplaintService orderComplaintService)
        {
            _smsDataContext = smsDataContext;
            _systemAlertService = systemAlertService;
            _dateTimeService = dateTimeService;
            _userService = userService;
            _authService = authService;
            _orderComplaintService = orderComplaintService;
            _orderService = orderService;
            _internationalSimService = internationalSimService;
        }
        [HttpGet("calculate-all-order-complains-price")]
        public async Task<decimal> CalculateAllOrderComplains()
        {
            var complaints = await _smsDataContext.OrderComplaints
                .Where(r => r.OrderComplaintStatus == Helpers.OrderComplaintStatus.Floating || r.OrderComplaintStatus == Helpers.OrderComplaintStatus.Cancelled)
                .SumAsync(r => r.Order.Price);
            return complaints;
        }

        [HttpGet("refund-all-order-complains")]
        public async Task<bool> RefundAllOrderComplains()
        {
            var complaints = await _smsDataContext.OrderComplaints
                .Where(r => r.OrderComplaintStatus == Helpers.OrderComplaintStatus.Floating || r.OrderComplaintStatus == Helpers.OrderComplaintStatus.Cancelled)
                .ToListAsync();
            foreach (var complaint in complaints)
            {
                await _orderComplaintService.Update(new OrderComplaint()
                {
                    Id = complaint.Id,
                    AdminComment = complaint.AdminComment,
                    UserComment = complaint.UserComment,
                    OrderComplaintStatus = Helpers.OrderComplaintStatus.Refund
                });
            }

            return true;
        }

        [HttpGet("turn-all-referals")]
        public async Task<bool> TurnAllReferal()
        {
            var admin = await _smsDataContext.Users.Where(r => r.Username == "admin").Select(r => r.Id).FirstOrDefaultAsync();
            _authService.SetCurrentUserId(admin);
            var totalUsers = await _smsDataContext.Users.Where(r => r.ReferEnabled == false).Select(r => r.Id).ToListAsync();
            var i = 0;
            foreach (var user in totalUsers)
            {
                i++;
                await _userService.ToggleReferEnabled(user, true);
            }
            return true;
        }

        [HttpGet("raise-an-alert")]
        public async Task<bool> RaiseAnSystemAlert()
        {
            await _systemAlertService.RaiseAnAlert(new SystemAlert()
            {
                Topic = "Order",
                Thread = "FloatingOrderOverload",
                DetailJson = JsonConvert.SerializeObject(10),
            });
            return true;
        }
        [HttpGet("service-with-all-networks")]
        public async Task<bool> InitAllServiceProviderWithNetworks()
        {
            var services = await _smsDataContext.ServiceProviders.ToListAsync();
            foreach (var service in services)
            {
                _smsDataContext.ServiceNetworkProviders.Add(new ServiceNetworkProvider()
                {
                    NetworkProviderId = 1,
                    ServiceProviderId = service.Id,
                });
                _smsDataContext.ServiceNetworkProviders.Add(new ServiceNetworkProvider()
                {
                    NetworkProviderId = 2,
                    ServiceProviderId = service.Id,
                });
                _smsDataContext.ServiceNetworkProviders.Add(new ServiceNetworkProvider()
                {
                    NetworkProviderId = 3,
                    ServiceProviderId = service.Id,
                });
                _smsDataContext.ServiceNetworkProviders.Add(new ServiceNetworkProvider()
                {
                    NetworkProviderId = 4,
                    ServiceProviderId = service.Id,
                });
                _smsDataContext.ServiceNetworkProviders.Add(new ServiceNetworkProvider()
                {
                    NetworkProviderId = 5,
                    ServiceProviderId = service.Id,
                });
            }
            await _smsDataContext.SaveChangesAsync();
            return true;
        }

        [HttpGet("basic")]
        public async Task<bool> BasicSeeding()
        {
            await SeedSystemConfiguration();
            await SeedUsers();
            await SeedServiceProvider();
            await SeedGsmDevices();
            return true;
        }
        [HttpGet("international-sim")]
        public async Task<bool> SeedInternationalSim()
        {
            await SeedSomeSimCountries();
            await SeedSomeInternationalSims();
            return true;
        }

        [HttpGet("init-referal-code")]
        public async Task<bool> InitReferalCode()
        {
            var user = await _smsDataContext.Users.FirstOrDefaultAsync(r => r.Username == "user");
            if (user != null)
            {
                user.ReferEnabled = true;
                await _smsDataContext.SaveChangesAsync();
            }
            return true;
        }

        [HttpGet("seeds-user-referal-fee")]
        public async Task<bool> SeedUserReferalFee()
        {
            var user = await _smsDataContext.Users.FirstOrDefaultAsync(r => r.Username == "user");
            var user2 = await _smsDataContext.Users.FirstOrDefaultAsync(r => r.Username == "user2");
            var user3 = await _smsDataContext.Users.FirstOrDefaultAsync(r => r.Username == "user3");
            var user4 = await _smsDataContext.Users.FirstOrDefaultAsync(r => r.Username == "user4");
            var staff = await _smsDataContext.Users.FirstOrDefaultAsync(r => r.Username == "staff");
            var admin = await _smsDataContext.Users.FirstOrDefaultAsync(r => r.Username == "admin");
            if (user == null || user2 == null || user3 == null || user4 == null || staff == null || admin == null)
                throw new Exception("Please run basic dev seeds firt!");
            _authService.SetCurrentUserId(admin.Id);
            for (var i = 0; i < 30; i++)
            {
                var cost = 12891000 + i * 300000;
                var referPercent = i < 10 ? 5 : 3;
                var reportTime = _dateTimeService.UtcNow().AddDays(-i);
                if (_smsDataContext.UserReferalFees
                    .Any(r => r.ReportTime >= reportTime
                                && r.ReportTime < reportTime.AddDays(1)
                                && r.UserId == user.Id
                                && r.ReferredUserId == user2.Id
                                ))
                {
                    continue;
                }
                _smsDataContext.UserReferalFees.Add(new UserReferalFee()
                {
                    FeeAmount = cost * referPercent / 100,
                    ReferFeePercent = referPercent,
                    ReportTime = reportTime,
                    ReferredUserId = user2.Id,
                    TotalCost = cost,
                    TotalOrderCount = 12891 + i * 300,
                    UserId = user.Id
                });
                _smsDataContext.UserReferredFees.Add(new UserReferredFee()
                {
                    FeeAmount = cost * referPercent / 100,
                    ReferFeePercent = referPercent,
                    ReportTime = reportTime,
                    TotalCost = cost,
                    TotalOrderCount = 12891 + i * 300,
                    UserId = user2.Id
                });
            }
            for (var i = 0; i < 30; i++)
            {
                var cost = 13897000 + i * 317000;
                var referPercent = i < 10 ? 5 : 3;
                var reportTime = _dateTimeService.UtcNow().AddDays(-i);
                if (_smsDataContext.UserReferalFees
                    .Any(r => r.ReportTime >= reportTime
                                && r.ReportTime < reportTime.AddDays(1)
                                && r.UserId == user.Id
                                && r.ReferredUserId == user3.Id
                                ))
                {
                    continue;
                }
                _smsDataContext.UserReferalFees.Add(new UserReferalFee()
                {
                    FeeAmount = cost * referPercent / 100,
                    ReferFeePercent = referPercent,
                    ReportTime = reportTime,
                    ReferredUserId = user3.Id,
                    TotalCost = cost,
                    TotalOrderCount = 13897 + i * 320,
                    UserId = user.Id
                });
                _smsDataContext.UserReferredFees.Add(new UserReferredFee()
                {
                    FeeAmount = cost * referPercent / 100,
                    ReferFeePercent = referPercent,
                    ReportTime = reportTime,
                    TotalCost = cost,
                    TotalOrderCount = 13897 + i * 320,
                    UserId = user3.Id
                });
            }
            for (var i = 0; i < 30; i++)
            {
                var cost = 11781000 + i * 287000;
                var referPercent = i < 10 ? 5 : 3;
                var reportTime = _dateTimeService.UtcNow().AddDays(-i);
                if (_smsDataContext.UserReferalFees
                    .Any(r => r.ReportTime >= reportTime
                                && r.ReportTime < reportTime.AddDays(1)
                                && r.UserId == user3.Id
                                && r.ReferredUserId == user4.Id
                                ))
                {
                    continue;
                }
                _smsDataContext.UserReferalFees.Add(new UserReferalFee()
                {
                    FeeAmount = cost * referPercent / 100,
                    ReferFeePercent = referPercent,
                    ReportTime = reportTime,
                    ReferredUserId = user4.Id,
                    TotalCost = cost,
                    TotalOrderCount = 11781 + i * 281,
                    UserId = user3.Id
                });
                _smsDataContext.UserReferredFees.Add(new UserReferredFee()
                {
                    FeeAmount = cost * referPercent / 100,
                    ReferFeePercent = referPercent,
                    ReportTime = reportTime,
                    TotalCost = cost,
                    TotalOrderCount = 11781 + i * 281,
                    UserId = user4.Id
                });
            }
            for (var i = 0; i < 30; i++)
            {
                var cost = 13091000 + i * 251000;
                var referPercent = i < 10 ? 5 : 3;
                var reportTime = _dateTimeService.UtcNow().AddDays(-i);
                if (_smsDataContext.UserReferalFees
                    .Any(r => r.ReportTime >= reportTime
                                && r.ReportTime < reportTime.AddDays(1)
                                && r.UserId == staff.Id
                                && r.ReferredUserId == user.Id
                                ))
                {
                    continue;
                }
                _smsDataContext.UserReferalFees.Add(new UserReferalFee()
                {
                    FeeAmount = cost * referPercent / 100,
                    ReferFeePercent = referPercent,
                    ReportTime = reportTime,
                    ReferredUserId = user.Id,
                    TotalCost = cost,
                    TotalOrderCount = 13090 + i * 246,
                    UserId = staff.Id
                });
                _smsDataContext.UserReferredFees.Add(new UserReferredFee()
                {
                    FeeAmount = cost * referPercent / 100,
                    ReferFeePercent = referPercent,
                    ReportTime = reportTime,
                    TotalCost = cost,
                    TotalOrderCount = 13090 + i * 246,
                    UserId = user.Id
                });
            }
            user.ReferalId = staff.Id;
            user2.ReferalId = user.Id;
            user3.ReferalId = user.Id;
            user4.ReferalId = user3.Id;
            await _userService.ToggleReferEnabled(user.Id, true);
            await _userService.ToggleReferEnabled(user2.Id, true);
            await _userService.ToggleReferEnabled(user3.Id, true);
            await _userService.ToggleReferEnabled(staff.Id, true);
            await _smsDataContext.SaveChangesAsync();
            return true;
        }
        [HttpGet("seed-request-a-order")]
        public async Task<bool> SeedAOrder()
        {
            await SeedRequestAOrder();
            return true;
        }

        [HttpGet("seed-auth-2fa")]
        public async Task<AspNetCore.Totp.Interface.Models.TotpSetup> Seed2FaAuthCode()
        {
            var admin = await _smsDataContext.Users.Where(u => u.Username == "admin").FirstOrDefaultAsync();

            var a = new AspNetCore.Totp.TotpSetupGenerator();
            var r = a.Generate("Rentcode", admin.Username, $"{admin.Username}X{admin.Guid}");
            return r;
        }

        [HttpGet("seed-verify-2fa")]
        public async Task<bool> Seed2FaAuthVerify(int totp)
        {
            var admin = await _smsDataContext.Users.Where(u => u.Username == "admin").FirstOrDefaultAsync();
            var x = new AspNetCore.Totp.TotpValidator(new AspNetCore.Totp.TotpGenerator());
            return x.Validate($"{admin.Username}X{admin.Guid}", totp, 0);
        }
        private async Task SeedSystemConfiguration()
        {
            if (!await _smsDataContext.SystemConfigurations.AnyAsync(r => r.BrandName != null))
            {
                var all = await _smsDataContext.SystemConfigurations.ToListAsync();
                foreach (var item in all)
                {
                    _smsDataContext.SystemConfigurations.Remove(item);
                }
                _smsDataContext.SystemConfigurations.Add(new SystemConfiguration()
                {
                    ThresholdsForAutoSuspend = 15,
                    BrandName = "RentCode",
                    Email = "rentcode.co@gmail.com",
                    FacebookUrl = "https://www.facebook.com/loi.chuoc.3",
                    LogoUrl = "https://s3-ap-southeast-1.amazonaws.com/maxmarket/myad/img/img_38fc39ea-4cff-4ba9-9933-d9f1569955bc.png",
                    PhoneNumber = "0988 918 919",
                    ThresholdsForWarning = 10,
                    AdminNotification = "Chào mừng bạn đến với hệ thống RentCode",
                    TelegramUrl = "@rentcode (telegram)",
                    YoutubeUrl = "",
                    MaximumAvailableOrder = 100,
                    UsdRate = 23190
                });
            }
        }

        private async Task SeedUsers()
        {
            if (!await _smsDataContext.Users.AnyAsync(r => r.Username == "admin"))
            {
                _smsDataContext.Users.Add(new Entity.User()
                {
                    ApiKey = Helpers.Helpers.GenerateApiKey(),
                    Username = "admin",
                    Ballance = 0,
                    Password = "123456789",
                    Role = Helpers.RoleType.Administrator,
                    UserProfile = new UserProfile()
                    {
                        Name = "Admin",
                        PhoneNumber = "0777423918",
                        Email = "admin@rentcode.com",
                    },
                });
            }
            if (!await _smsDataContext.Users.AnyAsync(r => r.Username == "user"))
            {
                _smsDataContext.Users.Add(new Entity.User()
                {
                    ApiKey = Helpers.Helpers.GenerateApiKey(),
                    Username = "user",
                    Ballance = 200000000,
                    Password = "123456789",
                    Role = Helpers.RoleType.User,
                    UserProfile = new UserProfile()
                    {
                        Name = "User",
                        PhoneNumber = "0777423916",
                        Email = "user@rentcode.com",
                    },
                });
            }
            if (!await _smsDataContext.Users.AnyAsync(r => r.Username == "user2"))
            {
                _smsDataContext.Users.Add(new Entity.User()
                {
                    ApiKey = Helpers.Helpers.GenerateApiKey(),
                    Username = "user2",
                    Ballance = 180000000,
                    Password = "123456789",
                    Role = Helpers.RoleType.User,
                    UserProfile = new UserProfile()
                    {
                        Name = "User2",
                        PhoneNumber = "0777423916",
                        Email = "user2@rentcode.com",
                    },
                });
            }
            if (!await _smsDataContext.Users.AnyAsync(r => r.Username == "user3"))
            {
                _smsDataContext.Users.Add(new Entity.User()
                {
                    ApiKey = Helpers.Helpers.GenerateApiKey(),
                    Username = "user3",
                    Ballance = 180000000,
                    Password = "123456789",
                    Role = Helpers.RoleType.User,
                    UserProfile = new UserProfile()
                    {
                        Name = "User3",
                        PhoneNumber = "0777423911",
                        Email = "user3@rentcode.com",
                    },
                });
            }
            if (!await _smsDataContext.Users.AnyAsync(r => r.Username == "user4"))
            {
                _smsDataContext.Users.Add(new Entity.User()
                {
                    ApiKey = Helpers.Helpers.GenerateApiKey(),
                    Username = "user4",
                    Ballance = 180000000,
                    Password = "123456789",
                    Role = Helpers.RoleType.User,
                    UserProfile = new UserProfile()
                    {
                        Name = "User4",
                        PhoneNumber = "0777423910",
                        Email = "user4@rentcode.com",
                    },
                });
            }
            if (!await _smsDataContext.Users.AnyAsync(r => r.Username == "staff"))
            {
                _smsDataContext.Users.Add(new Entity.User()
                {
                    ApiKey = Helpers.Helpers.GenerateApiKey(),
                    Username = "staff",
                    Ballance = 170000000,
                    Password = "123456789",
                    Role = Helpers.RoleType.Staff,
                    UserProfile = new UserProfile()
                    {
                        Name = "Staff",
                        PhoneNumber = "0777423915",
                        Email = "staff@rentcode.com",
                    },
                });
            }

            if (!await _smsDataContext.Users.AnyAsync(r => r.Username == "forwarder"))
            {
                _smsDataContext.Users.Add(new Entity.User()
                {
                    ApiKey = Helpers.Helpers.GenerateApiKey(),
                    Username = "forwarder",
                    Ballance = 201000000,
                    Password = "123456789",
                    Role = Helpers.RoleType.Forwarder,
                    UserProfile = new UserProfile()
                    {
                        Name = "Forwarder",
                        PhoneNumber = "0777423911",
                        Email = "forwarder@rentcode.com",
                    },
                });
            }
            if (!await _smsDataContext.Users.AnyAsync(r => r.Username == "forwarder2"))
            {
                _smsDataContext.Users.Add(new Entity.User()
                {
                    ApiKey = Helpers.Helpers.GenerateApiKey(),
                    Username = "forwarder2",
                    Ballance = 199000000,
                    Password = "123456789",
                    Role = Helpers.RoleType.Forwarder,
                    UserProfile = new UserProfile()
                    {
                        Name = "Forwarder2",
                        PhoneNumber = "0777423910",
                        Email = "forwarder2@rentcode.com",
                    },
                });
            }
            if (!await _smsDataContext.Users.AnyAsync(r => r.Username == "forwarder3"))
            {
                _smsDataContext.Users.Add(new Entity.User()
                {
                    ApiKey = Helpers.Helpers.GenerateApiKey(),
                    Username = "forwarder3",
                    Ballance = 198000000,
                    Password = "123456789",
                    Role = Helpers.RoleType.Forwarder,
                    UserProfile = new UserProfile()
                    {
                        Name = "Forwarder3",
                        PhoneNumber = "0777423901",
                        Email = "forwarder3@rentcode.com",
                    },
                });
            }
            await _smsDataContext.SaveChangesAsync();
        }

        private async Task SeedServiceProvider()
        {
            if (!await _smsDataContext.ServiceProviders.AnyAsync(r => r.Name == "Facebook"))
            {
                _smsDataContext.ServiceProviders.Add(new ServiceProvider()
                {
                    LockTime = 5,
                    Name = "Facebook",
                    MessageRegex = "facebook",
                    MessageCodeRegex = "(\\d+)",
                    Price = 800,
                    ReceivingThreshold = 1,
                    ServiceType = Helpers.ServiceType.Basic,
                });
            }
            if (!await _smsDataContext.ServiceProviders.AnyAsync(r => r.Name == "Google+Gmail"))
            {
                _smsDataContext.ServiceProviders.Add(new ServiceProvider()
                {
                    LockTime = 5,
                    Name = "Google",
                    MessageRegex = "g-",
                    MessageCodeRegex = "(\\d+)",
                    Price = 1000,
                    ReceivingThreshold = 1,
                    ServiceType = Helpers.ServiceType.Any,
                });
            }
            if (!await _smsDataContext.ServiceProviders.AnyAsync(r => r.Name == "Shopee"))
            {
                _smsDataContext.ServiceProviders.Add(new ServiceProvider()
                {
                    LockTime = 5,
                    Name = "Shopee",
                    MessageRegex = "shopee",
                    MessageCodeRegex = "(\\d+)",
                    Price = 1000,
                    ReceivingThreshold = 1,
                    ServiceType = Helpers.ServiceType.Basic,
                });
            }
            if (!await _smsDataContext.ServiceProviders.AnyAsync(r => r.Name == "Thuê SIM theo ngày"))
            {

                _smsDataContext.ServiceProviders.Add(new ServiceProvider()
                {
                    Name = "Thuê SIM theo ngày",
                    MessageRegex = "shopee",
                    MessageCodeRegex = "|\\D*",
                    Price = 7000,
                    AdditionalPrice = 3000,
                    ServiceType = Helpers.ServiceType.ByTime,
                });
            }
            if (!await _smsDataContext.ServiceProviders.AnyAsync(r => r.Name == "Thuê lại sim đã dùng"))
            {
                _smsDataContext.ServiceProviders.Add(new ServiceProvider()
                {
                    Name = "Thuê lại sim đã dùng",
                    MessageRegex = "shopee",
                    MessageCodeRegex = "|\\D*",
                    Price = 1100,
                    ServiceType = Helpers.ServiceType.Callback,
                });
            }
            await _smsDataContext.SaveChangesAsync();
        }

        private async Task SeedGsmDevices()
        {
            if (!await _smsDataContext.GsmDevices.AnyAsync(r => r.Name == "GsmTest01"))
            {
                _smsDataContext.GsmDevices.Add(new GsmDevice()
                {
                    Name = "GsmTest01",
                    Code = "CodeTest01",
                    Coms = new List<Com>() {
                        new Com(){
                            PhoneNumber = "0905000001"
                        },
                        new Com(){
                            PhoneNumber = "0905000002"
                        },
                        new Com(){
                            PhoneNumber = "0905000003"
                        },
                        new Com(){
                            PhoneNumber = "0905000004"
                        },
                        new Com(){
                            PhoneNumber = "0905000005"
                        },
                        new Com(){
                            PhoneNumber = "0905000006s"
                        },
                    }

                });
            }

            await _smsDataContext.SaveChangesAsync();
        }

        private async Task SeedRequestAOrder()
        {
            var user = await _smsDataContext.Users.Where(r => r.Role == RoleType.User).FirstOrDefaultAsync();
            if (user == null) throw new Exception("Run basic dev seed first");
            var serviceProvider = await _smsDataContext.ServiceProviders.OrderBy(r => Guid.NewGuid()).FirstOrDefaultAsync();
            if (serviceProvider == null) throw new Exception("Run basic dev seed first");
            await _orderService.RequestAOrder(user.Id, serviceProvider.Id, null, null, AppSourceType.Web, false, false);
        }

        private async Task SeedSomeSimCountries()
        {
            if (!await _smsDataContext.SimCountries.AnyAsync(r => r.CountryCode == "us"))
            {
                _smsDataContext.SimCountries.Add(new SimCountry()
                {
                    CountryCode = "us",
                    CountryName = "USA",
                    PhonePrefix = "+1",
                    Price = 20000m
                });
            }
            if (!await _smsDataContext.SimCountries.AnyAsync(r => r.CountryCode == "uk"))
            {
                _smsDataContext.SimCountries.Add(new SimCountry()
                {
                    CountryCode = "uk",
                    CountryName = "United Kingdom",
                    PhonePrefix = "+44",
                    Price = 22000m
                });
            }
            if (!await _smsDataContext.SimCountries.AnyAsync(r => r.CountryCode == "au"))
            {
                _smsDataContext.SimCountries.Add(new SimCountry()
                {
                    CountryCode = "au",
                    CountryName = "Australia",
                    PhonePrefix = "+61",
                    Price = 19000m
                });
            }
            if (!await _smsDataContext.SimCountries.AnyAsync(r => r.CountryCode == "th"))
            {
                _smsDataContext.SimCountries.Add(new SimCountry()
                {
                    CountryCode = "th",
                    CountryName = "Thailand",
                    PhonePrefix = "+66",
                    Price = 15000m
                });
            }
            await _smsDataContext.SaveChangesAsync();
        }

        private async Task SeedSomeInternationalSims()
        {
            await SeedSomeInternationalSimsByForwarderAndCountry("forwarder", "th", "98");

            await SeedSomeInternationalSimsByForwarderAndCountry("forwarder2", "uk", "53");
            await SeedSomeInternationalSimsByForwarderAndCountry("forwarder3", "au", "21");
        }
        private async Task SeedSomeInternationalSimsByForwarderAndCountry(string forwarderUserName, string countryCode, string randomPhone)
        {
            var forwarder = await _smsDataContext.Users.FirstOrDefaultAsync(r => r.Username == forwarderUserName);
            if (forwarder == null)
            {
                throw new Exception("Please run basic dev seed first");
            }
            var country = await _smsDataContext.SimCountries.FirstOrDefaultAsync(r => r.CountryCode == countryCode);
            if (country == null)
            {
                throw new Exception("Not found th country");
            }
            for (int i = 0; i < 10; i++)
            {
                await CreateInternationalSim($"0891{randomPhone}817{i}", country.Id, forwarder.Id);
            }
        }

        private async Task CreateInternationalSim(string phoneNumber, int simCountryId, int forwarderId)
        {
            if (!await _smsDataContext.InternationalSims.AnyAsync(r => r.PhoneNumber == phoneNumber && r.SimCountryId == simCountryId && r.ForwarderId == forwarderId))
            {
                await _internationalSimService.Create(new InternationalSim()
                {
                    PhoneNumber = phoneNumber,
                    ForwarderId = forwarderId,
                    SimCountryId = simCountryId
                });
            }
        }
    }
}
