using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sms.Web.Entity;
using Sms.Web.Helpers;
using Sms.Web.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Sms.Web.Service
{
    public interface IStatisticService
    {
        Task<ApiResponseBaseModel<List<DailyReport>>> GenerateDashboard(int userId);
        Task<ApiResponseBaseModel<List<DailyReport>>> GenerateGsmReport(StatisticRequest request);
        Task<ApiResponseBaseModel<List<GsmReportModel>>> GsmPerformanceReport(GsmPerformanceReportRequest request);
        Task<ApiResponseBaseModel<List<ServiceAvailableReportModel>>> ServiceAvailableReport();
        Task GenerateServiceProviderAvailableReport();
    }
    public class StatisticService : IStatisticService
    {
        private readonly IOrderService _orderService;
        private readonly SmsDataContext _smsDataContext;
        private readonly IUserTransactionService _userTransactionService;
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IServiceProviderService _serviceProviderService;
        private readonly IDiscountService _discountService;
        private readonly IMemoryCache _memoryCache;
        private readonly ICacheService _cacheService;
        public StatisticService(SmsDataContext smsDataContext, IOrderService orderService,
            IUserTransactionService userTransactionService, IAuthService authService, IUserService userService,
            IDateTimeService dateTimeService, IServiceProviderService serviceProviderService,
            IDiscountService discountService, IMemoryCache memoryCache, ICacheService cacheService)
        {
            _smsDataContext = smsDataContext;
            _orderService = orderService;
            _userTransactionService = userTransactionService;
            _authService = authService;
            _userService = userService;
            _dateTimeService = dateTimeService;
            _serviceProviderService = serviceProviderService;
            _discountService = discountService;
            _memoryCache = memoryCache;
            _cacheService = cacheService;
        }
        public async Task<ApiResponseBaseModel<List<DailyReport>>> GenerateDashboard(int userId)
        {
            return await _memoryCache.GetOrCreateAsync($"DASHBOARD_DATA_CACHE_KEY_{userId}", async settings =>
            {
                settings.SetSlidingExpiration(TimeSpan.FromHours(2));
                settings.SetAbsoluteExpiration(_dateTimeService.GMT7Now().Date.AddDays(1));
                var toDate = _dateTimeService.GMT7Now().Date;
                var fromDate = toDate.AddMonths(-1).AddDays(1);
                var dayList = new List<DateTime>();
                var existingDailyReports = await GetExistingDailyReports(fromDate, toDate, userId);
                while (fromDate < toDate)
                {
                    if (!existingDailyReports.Any(r => r.Date.Date == fromDate))
                    {
                        dayList.Add(fromDate);
                    }
                    fromDate = fromDate.AddDays(1);
                }
                var reports = new List<DailyReport>();
                foreach (var day in dayList)
                {
                    var orderQuery = _smsDataContext.Orders.Where(r => r.Created >= day && r.Created < day.AddDays(1));
                    var transactionQuery = _smsDataContext.UserTransactions.Where(r => r.Created >= day && r.Created < day.AddDays(1));
                    if (userId != 0)
                    {
                        orderQuery = orderQuery.Where(r => r.UserId == userId);
                        transactionQuery = transactionQuery.Where(r => r.UserId == userId);
                    }
                    var canceledOrderCount = await orderQuery.CountAsync(r => r.Status == OrderStatus.Cancelled);
                    var createdOrderCount = await orderQuery.CountAsync();
                    var finishedCount = await orderQuery.CountAsync(r => r.Status == OrderStatus.Success);
                    var errorCount = await orderQuery.CountAsync(r => r.Status == OrderStatus.Error);
                    var spentCredits = await transactionQuery.Where(r => !r.IsImport && r.UserTransactionType == UserTransactionType.PaidForService).SumAsync(r => r.Amount);
                    var rechargedCredits = await transactionQuery.Where(r => r.IsImport && r.UserTransactionType == UserTransactionType.UserRecharge).SumAsync(r => r.Amount);
                    var referalFee = await transactionQuery.Where(r => r.IsImport && r.UserTransactionType == UserTransactionType.ReferalFee).SumAsync(r => r.Amount);
                    var referedFee = await transactionQuery.Where(r => r.IsImport && r.UserTransactionType == UserTransactionType.ReferedFee).SumAsync(r => r.Amount);
                    var agentDiscount = await transactionQuery.Where(r => r.IsImport && r.UserTransactionType == UserTransactionType.AgentDiscount).SumAsync(r => r.Amount);
                    var agentCheckout = await transactionQuery.Where(r => !r.IsImport && r.UserTransactionType == UserTransactionType.AgentCheckout).SumAsync(r => r.Amount);
                    var dailyReport = new DailyReport()
                    {
                        CanceledOrderCount = canceledOrderCount,
                        CreatedOrderCount = createdOrderCount,
                        FinishedOrderCount = finishedCount,
                        ErrorOrderCount = errorCount,
                        SpentCredits = spentCredits,
                        RechargedCredits = rechargedCredits,
                        ReferalFee = referalFee,
                        ReferedFee = referedFee,
                        AgentDiscount = agentDiscount,
                        AgentCheckout = agentCheckout,
                        UserId = userId,
                        Date = day
                    };
                    reports.Add(dailyReport);
                    if (day < _dateTimeService.GMT7Now().Date)
                    {
                        _smsDataContext.DailyReports.Add(dailyReport);
                    }
                }
                await _smsDataContext.SaveChangesAsync();
                return new ApiResponseBaseModel<List<DailyReport>>() { Success = true, Results = existingDailyReports.Concat(reports).ToList() };
            });
        }
        private async Task<List<DailyReport>> GetExistingDailyReports(DateTime fromDate, DateTime toDate, int userId)
        {
            return await _smsDataContext.DailyReports.Where(r => fromDate <= r.Date && toDate > r.Date && r.UserId == userId).ToListAsync();
        }
        private async Task<List<UserTransaction>> GetAllTransactions(int userId, DateTime fromDate, DateTime toDate)
        {
            var pageIndex = 0;
            var pageSize = 1000;
            var list = new List<UserTransaction>();
            FilterRequest pagingRequest;
            FilterResponse<UserTransaction> paging;
            int totals;
            do
            {
                pagingRequest = new FilterRequest()
                {
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    SearchObject = new Dictionary<string, object>() {
                    {"CreatedFrom", fromDate },
                    {"CreatedTo", toDate }
                }
                };
                if (userId != 0)
                {
                    pagingRequest.SearchObject.Add("UserId", userId);
                }
                paging = await _userTransactionService.Paging(pagingRequest);
                totals = paging.Total;
                list.AddRange(paging.Results);
                pageIndex++;
            }
            while (totals > list.Count && paging.Results.Count > 0);
            return list;
        }

        public async Task<ApiResponseBaseModel<List<DailyReport>>> GenerateGsmReport(StatisticRequest request)
        {
            var userId = _authService.CurrentUserId();
            var user = await _userService.GetUser(userId.GetValueOrDefault());
            if (user == null || !(new List<RoleType>() { RoleType.Administrator, RoleType.Staff }).Contains(user.Role)) return new ApiResponseBaseModel<List<DailyReport>>()
            {
                Success = false,
                Message = "Unauthorized"
            };
            var reports = new List<DailyReport>();
            if (user.Role == RoleType.Staff)
            {
                var authorizedGsmIds = await _smsDataContext.UserGsmDevices.Where(r => r.UserId == userId).Select(r => r.GsmDeviceId).ToListAsync();
                request.GsmDeviceIds = request.GsmDeviceIds?.Where(r => authorizedGsmIds.Contains(r))?.ToList() ?? authorizedGsmIds;
            }

            var toDate = (request.EndDate ?? _dateTimeService.UtcNow()).AddHours(7);
            var fromDate = (request.StartDate ?? _dateTimeService.UtcNow()).AddHours(7);
            var allReportQuery = _smsDataContext.GsmReports.Where(r => r.ReportedDate >= fromDate & r.ReportedDate < toDate);

            if (request.GsmDeviceIds != null)
            {
                allReportQuery = allReportQuery.Where(r => request.GsmDeviceIds.Contains(r.GsmId));
            }

            if (request.ServiceProviderIds != null)
            {
                allReportQuery = allReportQuery.Where(r => request.ServiceProviderIds.Contains(r.ServiceProviderId));
            }

            var allReports = await allReportQuery.OrderByDescending(r => r.Created).Skip(0).Take(100000).ToListAsync();

            while (fromDate < toDate)
            {
                var reportInThisDate = allReports.Where(r => r.ReportedDate >= fromDate && r.ReportedDate < fromDate.AddDays(1)).ToList();

                reports.Add(new DailyReport()
                {
                    CanceledOrderCount = 0,
                    CreatedOrderCount = 0,
                    FinishedOrderCount = reportInThisDate.Count(r => r.OrderStatus == OrderStatus.Success),
                    ErrorOrderCount = reportInThisDate.Count(r => r.OrderStatus == OrderStatus.Error),
                    RechargedCredits = 0,
                    SpentCredits = 0,
                    Date = fromDate
                });
                fromDate = fromDate.AddDays(1);
            }
            return new ApiResponseBaseModel<List<DailyReport>>()
            {
                Results = reports
            };
        }

        public async Task<ApiResponseBaseModel<List<GsmReportModel>>> GsmPerformanceReport(GsmPerformanceReportRequest request)
        {
            var userId = _authService.CurrentUserId();
            if (!userId.HasValue) return new ApiResponseBaseModel<List<GsmReportModel>>()
            {
                Success = false,
                Message = "Unauthorized"
            };
            var user = await _smsDataContext.Users.Where(r => r.Id == userId).FirstOrDefaultAsync();
            if (user == null || user.Role == RoleType.User)
            {
                if (!userId.HasValue) return new ApiResponseBaseModel<List<GsmReportModel>>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            List<int> authorizedGsmIds = null;
            if (user.Role == RoleType.Staff)
            {
                authorizedGsmIds = await _smsDataContext.UserGsmDevices.Where(r => r.UserId == userId).Select(r => r.GsmDeviceId).ToListAsync();
                if (authorizedGsmIds.Count == 0)
                {
                    return new ApiResponseBaseModel<List<GsmReportModel>>()
                    {
                        Results = new List<GsmReportModel>()
                    };
                }
            }

            if (!string.IsNullOrEmpty(request.StaffName) && user.Role == RoleType.Administrator)
            {
                authorizedGsmIds = await _smsDataContext.UserGsmDevices.Where(r => r.User.Username.Contains(request.StaffName.ToLower())
                                                                                && r.User.Role == RoleType.Staff)
                                                                       .Select(r => r.GsmDeviceId).ToListAsync();
            }

            if (request.GsmDeviveIds != null)
            {
                if (authorizedGsmIds == null)
                {
                    authorizedGsmIds = request.GsmDeviveIds;
                }
                else
                {
                    authorizedGsmIds = authorizedGsmIds.Where(r => request.GsmDeviveIds.Contains(r)).ToList();
                }
            }

            var query = await (from order in _smsDataContext.RentCodeOrders
                               where order.Updated >= request.StartDate && order.Updated < request.EndDate
                               && order.ConnectedGsmId.HasValue
                               && (order.Status == OrderStatus.Success || order.Status == OrderStatus.Error)
                               && (authorizedGsmIds == null || authorizedGsmIds.Contains(order.ConnectedGsmId.Value))
                               group order by new { order.ServiceProviderId, GsmDeviceId = order.ConnectedGsmId, order.Status } into g
                               select new { g.Key.ServiceProviderId, g.Key.GsmDeviceId, g.Key.Status, Count = g.Count(), Profit = g.Sum(x => x.GsmDeviceProfit) }).ToListAsync();

            var result = new List<GsmReportModel>();
            var queryGroup = (from q in query
                              group q by new { q.ServiceProviderId, q.GsmDeviceId } into g
                              select new GsmReportModel()
                              {
                                  ServiceProviderId = g.Key.ServiceProviderId,
                                  GsmId = g.Key.GsmDeviceId.GetValueOrDefault(),
                                  ErrorCount = (g.FirstOrDefault(r => r.Status == OrderStatus.Error)?.Count).GetValueOrDefault(),
                                  FinishedCount = (g.FirstOrDefault(r => r.Status == OrderStatus.Success)?.Count).GetValueOrDefault(),
                                  Profit = (g.FirstOrDefault(r => r.Status == OrderStatus.Success)?.Profit).GetValueOrDefault(),
                              }).ToList();
            return new ApiResponseBaseModel<List<GsmReportModel>>()
            {
                Success = true,
                Results = queryGroup
            };
        }

        public async Task GenerateServiceProviderAvailableReport()
        {
            var serviceProviders = (await _cacheService.GetAllServiceProviders()).Where(r => !r.Disabled).ToList();
            var allComs = await (from com in _smsDataContext.Coms.Include(r => r.GsmDevice)
                                 where com.Disabled != true && !string.IsNullOrEmpty(com.PhoneNumber) && com.GsmDevice.Disabled != true
                                 && com.GsmDevice.IsInMaintenance != true
                                 select com).AsNoTracking().ToListAsync();
            foreach (var serviceProvider in serviceProviders)
            {
                var gsmDeviceServiceProviders = await _smsDataContext.GsmDeviceServiceProviders
                    .Where(r => r.ServiceProviderId == serviceProvider.Id)
                    .AsNoTracking()
                    .ToListAsync();

                var successPhoneNumbers = await (from com in _smsDataContext.Coms
                                                 join order in _smsDataContext.RentCodeOrders on com.PhoneNumber equals order.PhoneNumber
                                                 where com.Disabled != true && !string.IsNullOrEmpty(com.PhoneNumber) && com.GsmDevice.Disabled != true
                                                 && com.GsmDevice.IsInMaintenance != true && (order.Status == OrderStatus.Success)
                                                 && order.ServiceProviderId == serviceProvider.Id
                                                 select order.PhoneNumber
                                                    ).ToListAsync();

                var coms = (from com in allComs
                            where (serviceProvider.ServiceType != ServiceType.ByTime || com.GsmDevice.IsServingForThirdService == true)
                            select com).ToList();

                var specifiedServiceGsmIds = coms.Where(r => r.GsmDevice.SpecifiedService == true).Select(r => r.GsmDeviceId).ToList();
                var gsmDeviceServiceProviderIds = gsmDeviceServiceProviders
                    .Where(r => specifiedServiceGsmIds.Contains(r.GsmDeviceId)).Select(r => r.GsmDeviceId).ToList();
                coms = coms.Where(com => com.GsmDevice.SpecifiedService != true || gsmDeviceServiceProviderIds.Contains(com.GsmDeviceId)).ToList();

                var allCount = coms.Count;

                if (serviceProvider.ServiceType == ServiceType.Any || serviceProvider.ServiceType == ServiceType.Basic)
                {
                    var checkingPhoneNumbers = allComs.Select(r => r.PhoneNumber).Distinct().ToList();
                    var successedPhoneCount = successPhoneNumbers
                        .Where(r => checkingPhoneNumbers.Contains(r))
                        .GroupBy(r => r)
                        .Where(gr => gr.Count() >= serviceProvider.ReceivingThreshold)
                        .Select(gr => gr.Key)
                        .ToList();

                    coms = coms.Where(r => !successedPhoneCount.Contains(r.PhoneNumber)).ToList();
                }
                var successedCount = allCount - coms.Count;
                var phoneHasDied = await (from r in _smsDataContext.ErrorPhoneLogs
                                          join com in _smsDataContext.Coms on r.PhoneNumber equals com.PhoneNumber
                                          where r.ServiceProviderId == serviceProvider.Id && r.IsActive == true
                                              && com.Disabled != true
                                              && !string.IsNullOrEmpty(com.PhoneNumber)
                                              && com.GsmDevice.Disabled != true
                                              && com.GsmDevice.IsInMaintenance != true
                                          select r.PhoneNumber).ToListAsync();

                coms = coms.Where(r => !phoneHasDied.Contains(r.PhoneNumber)).ToList();
                var availableCount = coms.Count;

                await _cacheService.SetServiceProviderAvailableCache(new ServiceProviderAvailableCacheModel()
                {
                    AllCount = allCount,
                    AvailableCount = coms.Count,
                    SuccessCount = successedCount,
                    ServiceProviderId = serviceProvider.Id,
                    ErrorCount = allCount - successedCount - availableCount
                });
            }
        }

        public async Task<ApiResponseBaseModel<List<ServiceAvailableReportModel>>> ServiceAvailableReport()
        {
            var serviceProviders = (await _cacheService.GetAllServiceProviders()).Where(r => !r.Disabled).ToList();
            var cache = await _cacheService.GetServiceProviderAvailableCache(serviceProviders.Select(r => r.Id).ToList());
            if (cache == null)
            {
                cache = new List<ServiceProviderAvailableCacheModel>();
            }
            return new ApiResponseBaseModel<List<ServiceAvailableReportModel>>()
            {
                Results = serviceProviders.Select(r =>
                {
                    var ca = cache.FirstOrDefault(x => x.ServiceProviderId == r.Id);
                    return new ServiceAvailableReportModel()
                    {
                        AvailableCount = ca?.AvailableCount ?? 0,
                        ErrorCount = ca?.ErrorCount ?? 0,
                        ReceivingThreshold = r.ReceivingThreshold,
                        ServiceProviderId = r.Id,
                        ServiceType = r.ServiceType,
                        UsedCount = ca?.SuccessCount ?? 0,
                        WaitingCount = 0,
                        TotalCount = ca?.AllCount ?? 0
                    };
                }).ToList()
            };
        }
    }
}
