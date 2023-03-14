using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
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
    public interface IOrderService : IServiceBase<RentCodeOrder>
    {
        Task<ApiResponseBaseModel<RentCodeOrder>> RequestAOrder(int userId, int serviceProviderId, int? networkProvider, int? maximumSms, AppSourceType appSourceType, bool OnlyAcceptFreshOtp, bool AllowVoiceSms);
        Task BackgroundProcessOrder();
        Task BackgroundProcessProposedPhoneNumber();
        Task BackgroundExpiredOrder();
        Task BackgroundProposedOrder();
        Task BackgroundPreloadServiceProvider();
        Task<ApiResponseBaseModel> CloseOrder(int id);
        Task<ApiResponseBaseModel<RentCodeOrder>> AssignResult(string phoneNumber, string message, string sender, int smsHistoryId);
        Task<ApiResponseBaseModel<RentCodeOrder>> AssignAudioResult(string phoneNumber, string audioUrl, string sender, int smsHistoryId);
        Task<bool> CheckOrderIsAvailableForUser(int orderId, int? userId = null);
        Task<ApiResponseBaseModel<RentCodeOrder>> RequestThirdOrder(int id, int duration, HoldSimDurationUnit unit, int? networkProvider, AppSourceType appSourceType);
        Task<ApiResponseBaseModel<RentCodeOrder>> RequestOrderCallback(int id, string requestPhoneNumber, AppSourceType appSourceType);
        Task<ApiResponseBaseModel<List<string>>> GetAllUserHistoryPhoneNumbers();
        Task<ApiResponseBaseModel<int?>> CheckMessageIsMatchWithService(ServiceProvider serviceProvider, string message, string sender);
        Task ArchiveOldOrders(ILogger logger);
    }
    public class OrderService : ServiceBase<RentCodeOrder>, IOrderService
    {
        private readonly string ORDER_PENDING_PHONE_CACHE_KEY = "PENDING_PHONE_{0}";
        private string SERVICE_PROVIDER_PENDING_PHONE_CACHE_KEY(int servieProviderId) => $"SERVICE_PROVIDER_PENDING_PHONE_CACHE_KEY_{servieProviderId}";
        private readonly IUserService _userService;
        private readonly IServiceProviderService _serviceProviderService;
        private readonly IOrderResultService _orderResultService;
        private readonly ILogger _logger;
        private readonly IAuthService _authService;
        private readonly IEmailSender _emailSender;
        private readonly IDateTimeService _dateTimeService;
        private readonly ISystemConfigurationService _systemConfigurationService;
        private readonly IMemoryCache _memoryCache;
        private readonly ISystemAlertService _systemAlertService;
        private readonly ICacheService _cacheService;
        private readonly IPreloadOrderServiceProviderQueue _preloadOrderServiceProviderQueue;
        private readonly TimeSpan PreloadCacheTimeOut = TimeSpan.FromSeconds(60 * 60 * 1000);
        public OrderService(SmsDataContext smsDataContext, IUserService userService, IServiceProviderService serviceProviderService,
            IOrderResultService orderResultService,
            ILogger<OrderService> logger,
            IAuthService authService,
            IEmailSender emailSender,
            IDateTimeService dateTimeService,
            ISystemConfigurationService systemConfigurationService,
            IMemoryCache memoryCache,
            ISystemAlertService systemAlertService,
            IPreloadOrderServiceProviderQueue preloadOrderServiceProviderQueue,
            ICacheService cacheService) : base(smsDataContext)
        {
            _userService = userService;
            _serviceProviderService = serviceProviderService;
            _orderResultService = orderResultService;
            _logger = logger;
            _authService = authService;
            _emailSender = emailSender;
            _dateTimeService = dateTimeService;
            _systemConfigurationService = systemConfigurationService;
            _memoryCache = memoryCache;
            _systemAlertService = systemAlertService;
            _cacheService = cacheService;
            _preloadOrderServiceProviderQueue = preloadOrderServiceProviderQueue;
        }

        public async Task BackgroundProcessOrder()
        {
            await AssignPhoneNumberToOrder();
        }

        public async Task BackgroundProposedOrder()
        {
            await ProcessProposedStatus();
        }

        public async Task BackgroundExpiredOrder()
        {
            await ProcessExpiredStatus();
        }
        public async Task BackgroundProcessProposedPhoneNumber()
        {
            await ProcessProposedPhoneNumber();
        }

        public async Task BackgroundPreloadServiceProvider()
        {
            await ProcessPreloadServiceProvider();
        }

        private async Task ProcessExpiredStatus()
        {
            var now = _dateTimeService.UtcNow();
            var expiredOrders = await (from o in _smsDataContext.RentCodeOrders
                                       where o.Status == OrderStatus.Waiting
                                       && o.Expired.HasValue
                                       && o.Expired < now
                                       select o).ToListAsync();

            var orderIds = expiredOrders.Select(r => r.Id).ToList();
            var resultCounts = await (from r in _smsDataContext.OrderResults
                                      where orderIds.Contains(r.OrderId)
                                      group r by r.OrderId into gr
                                      select new { orderId = gr.Key, Count = gr.Count() }).ToListAsync();

            foreach (var order in expiredOrders)
            {
                var count = resultCounts.Where(r => r.orderId == order.Id).Select(r => r.Count).FirstOrDefault();
                order.Status = count > 0 ? OrderStatus.Success : OrderStatus.Error;
                order.NeedProposedProcessing = true;
            }
            await _smsDataContext.SaveChangesAsync();
        }

        private async Task ProcessProposedStatus()
        {
            var now = _dateTimeService.UtcNow();
            var expiredOrders = await (from o in _smsDataContext.RentCodeOrders
                                       where o.NeedProposedProcessing
                                       orderby o.Id ascending
                                       select o).Take(100).ToListAsync();
            var serviceProviderIds = expiredOrders.Select(r => r.ServiceProviderId).ToList();
            var serviceProviders = await _smsDataContext.ServiceProviders.Where(r => serviceProviderIds.Contains(r.Id))
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation("Expired order: {0}", string.Join(", ", expiredOrders.Select(r => r.Id).ToArray()));
            var orderIds = expiredOrders.Select(r => r.Id).ToList();
            var configuration = await _systemConfigurationService.GetSystemConfiguration();
            var thresholdForWarning = 0;
            var thresholdForSuppend = 0;
            string adminEmail = "";
            if (configuration != null)
            {
                thresholdForSuppend = configuration.ThresholdsForAutoSuspend.GetValueOrDefault();
                thresholdForWarning = configuration.ThresholdsForWarning.GetValueOrDefault();
                adminEmail = configuration.Email;
            }
            var orderPhoneNumbers = expiredOrders.Where(r => !string.IsNullOrEmpty(r.PhoneNumber)).Select(r => r.PhoneNumber).ToList();
            var gsm512IdLists = await _cacheService.Gsm512IdList();

            foreach (var order in expiredOrders)
            {
                order.NeedProposedProcessing = false;
                var serviceProvider = serviceProviders.FirstOrDefault(r => r.Id == order.ServiceProviderId);
                var user = await _smsDataContext.Users.FirstOrDefaultAsync(r => r.Id == order.UserId);
                await ReportGsmValue(order);
                _logger.LogInformation("Stop coms Idle", order.ConnectedGsmId, order.PhoneNumber);
                if (order.ConnectedGsmId.HasValue && gsm512IdLists.Contains(order.ConnectedGsmId.Value))
                {
                    await StopComsRangeIdleAsync(order.ConnectedGsmId.Value, order.ProposedGsm512RangeName);
                }
                _logger.LogInformation("Update order {0} with status {1}", order.Id, order.Status);
                if (order.Status == OrderStatus.Error && !string.IsNullOrEmpty(order.PhoneNumber))
                {
                    await ReportServiceProviderContinuosFailedCount(order.ServiceProviderId);
                    await ReportUserContinuosFailedCount(order.UserId);
                    if (order.ConnectedGsmId.HasValue)
                    {
                        await ReportGsmServiceProviderContinuosFailedCount(order.ConnectedGsmId.Value, order.ServiceProviderId);
                    }
                    if (order.ConnectedGsmId.HasValue)
                    {
                        _logger.LogInformation("Start Add failed for phone {0}", order.PhoneNumber);
                        var gsmDeviceId = order.ConnectedGsmId;
                        var failed = await _smsDataContext.PhoneFailedCounts.FirstOrDefaultAsync(r => r.GsmDeviceId == gsmDeviceId);
                        if (failed == null)
                        {
                            failed = new PhoneFailedCount()
                            {
                                GsmDeviceId = gsmDeviceId.Value
                            };
                            _smsDataContext.PhoneFailedCounts.Add(failed);
                        }
                        failed.ContinuousFailed++;
                        failed.TotalFailed++;
                        if (failed.ContinuousFailed == thresholdForWarning)
                        {
                            await WarningForAdmin(adminEmail, failed.GsmDeviceId, failed);
                        }
                        if (failed.ContinuousFailed == thresholdForSuppend)
                        {
                            var com = await _smsDataContext.GsmDevices.FirstOrDefaultAsync(r => r.Id == gsmDeviceId);
                            if (com != null)
                            {
                                com.Disabled = true;
                            }
                        }
                        _logger.LogInformation("Start add ErrorPhoneLog");
                        await AddEditPhoneLog(order.PhoneNumber, order.UserId, serviceProvider);
                    }
                    _logger.LogInformation("Start get UserTransaction");
                    var transaction = await _smsDataContext.UserTransactions.FirstOrDefaultAsync(r => r.OrderId == order.Id);
                    if (transaction != null)
                    {
                        _logger.LogInformation("Start remove UserTransaction");
                        _smsDataContext.UserTransactions.Remove(transaction);
                    }
                }
                _logger.LogInformation("Start save change all");
                await _smsDataContext.SaveChangesAsync();
                if (order.Status == OrderStatus.Error && !string.IsNullOrEmpty(order.PhoneNumber))
                {
                    _logger.LogInformation("Start execute query increase balance");
                    var sql = @"UPDATE Users set Ballance = Ballance + {0} where Id = {1}";
                    await _smsDataContext.Database.ExecuteSqlCommandAsync(
                        sql,
                        order.Price,
                        user.Id
                        );
                }
                else if (order.Status == OrderStatus.Success && serviceProvider.ServiceType == ServiceType.ByTime)
                {
                    try
                    {
                        var discount = await _smsDataContext.Discounts.FirstOrDefaultAsync(r => r.Month == order.Created.Value.Month - 1
                    && r.Year == order.Created.Value.Year && r.GsmDeviceId == order.ConnectedGsmId && r.ServiceProviderId == order.ServiceProviderId);
                        order.GsmDeviceProfit = order.Price * (decimal)discount.Percent / 100;
                        var gsmOwnerIds = await _smsDataContext.UserGsmDevices.Where(r => r.GsmDeviceId == order.ConnectedGsmId).Select(r => r.UserId).ToListAsync();
                        var gsmOwnersCount = gsmOwnerIds.Count;
                        if (gsmOwnersCount > 0)
                        {
                            var eachProfit = order.GsmDeviceProfit / gsmOwnersCount;
                            var transactions = gsmOwnerIds.Select(r => new UserTransaction()
                            {
                                Amount = eachProfit,
                                Comment = $"Chiec Khau GSM<{order.ConnectedGsmId}>",
                                IsImport = true,
                                UserId = r,
                                OrderId = order.Id,
                                UserTransactionType = UserTransactionType.AgentDiscount
                            }).ToList();
                            _smsDataContext.UserTransactions.AddRange(transactions);
                        }
                        await _smsDataContext.SaveChangesAsync();
                        if (gsmOwnersCount > 0)
                        {
                            var eachProfit = order.GsmDeviceProfit / gsmOwnersCount;
                            foreach (var ownerId in gsmOwnerIds)
                            {
                                await UpdateUserBallance(ownerId, eachProfit);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Profit failed 2");
                    }
                }
            }
            _logger.LogInformation("End ProcessOrderStatus");
        }

        private async Task ReportUserContinuosFailedCount(int userId)
        {
            try
            {
                var systemConfiguration = await _systemConfigurationService.GetSystemConfiguration();
                var failedCount = systemConfiguration.UserContinuosFailed;
                if (failedCount == 0)
                {
                    failedCount = 10;
                }
                var current = await _cacheService.IncreaseUserContinuosFailedCount(userId);
                if (current > failedCount)
                {
                    await _systemAlertService.RaiseAnAlert(new SystemAlert()
                    {
                        Topic = "RentCodeOrder",
                        Thread = "UserContinuosFailed",
                        DetailJson = JsonConvert.SerializeObject(new UserContinuosFailedAlertPayload()
                        {
                            ContinuosFailedCount = current,
                            UserId = userId
                        })
                    });
                    await _cacheService.ResetUserContinuosFailedCount(userId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Report user continuos failed count error");
            }
        }

        private async Task ReportGsmServiceProviderContinuosFailedCount(int gsmId, int serviceProviderId)
        {
            try
            {
                var systemConfiguration = await _systemConfigurationService.GetSystemConfiguration();
                var failedCount = systemConfiguration.GsmServiceProviderContinuosFailed;
                if (failedCount == 0)
                {
                    failedCount = 10;
                }
                var current = await _cacheService.IncreaseGsmServiceProviderContinuosFailedCount(gsmId, serviceProviderId);
                if (current > failedCount)
                {
                    await _systemAlertService.RaiseAnAlert(new SystemAlert()
                    {
                        Topic = "RentCodeOrder",
                        Thread = "GsmServiceProviderContinuosFailed",
                        DetailJson = JsonConvert.SerializeObject(new GsmServiceProviderContinuosFailedAlertPayload()
                        {
                            ContinuosFailedCount = current,
                            GsmId = gsmId,
                            ServiceProviderId = serviceProviderId
                        })
                    });
                    await _cacheService.ResetGsmServiceProviderContinuosFailedCount(gsmId, serviceProviderId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Report gsm service provider continuos failed count error");
            }
        }

        private async Task ReportServiceProviderContinuosFailedCount(int serviceProviderId)
        {
            try
            {
                var systemConfiguration = await _systemConfigurationService.GetSystemConfiguration();
                var failedCount = systemConfiguration.ServiceProviderContinuosFailed;
                if (failedCount == 0)
                {
                    failedCount = 10;
                }
                var current = _cacheService.IncreaseServiceProviderContinuosFailedCount(serviceProviderId);
                if (current > failedCount)
                {
                    await _systemAlertService.RaiseAnAlert(new SystemAlert()
                    {
                        Topic = "RentCodeOrder",
                        Thread = "ServiceProviderContinuosFailed",
                        DetailJson = JsonConvert.SerializeObject(new ServiceProviderContinuosFailedAlertPayload()
                        {
                            ContinuosFailedCount = current,
                            ServiceProviderId = serviceProviderId
                        })
                    });
                    _cacheService.ResetServiceProviderContinuosFailedCount(serviceProviderId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Report Service provider continuos failed count error");
            }
        }

        private async Task AddEditPhoneLog(string phoneNumber, int userId, ServiceProvider serviceProvider)
        {
            if (serviceProvider == null) return;
            var totalErrorOnUser = await _smsDataContext.RentCodeOrders.CountAsync(r =>
                r.PhoneNumber == phoneNumber
                && r.UserId == userId
                && r.ServiceProviderId == serviceProvider.Id
                && r.Status == OrderStatus.Error);

            if (totalErrorOnUser < serviceProvider.ErrorThreshold) return;
            var errorLog = await _smsDataContext.ErrorPhoneLogs.Include(r => r.ErrorPhoneLogOrders)
                .FirstOrDefaultAsync(r => r.PhoneNumber == phoneNumber && r.ServiceProviderId == serviceProvider.Id);
            if (errorLog == null)
            {
                errorLog = new ErrorPhoneLog()
                {
                    PhoneNumber = phoneNumber,
                    ServiceProviderId = serviceProvider.Id,
                    ErrorPhoneLogOrders = new List<ErrorPhoneLogUser>()
                };
            }
            errorLog.ErrorPhoneLogOrders.Add(new ErrorPhoneLogUser()
            {
                UserId = userId
            });
            if (errorLog.ErrorPhoneLogOrders.Count(r => r.IsIgnored != true) >= serviceProvider.TotalErrorThreshold)
            {
                errorLog.IsActive = true;
            }
            if (errorLog.Id == 0) _smsDataContext.ErrorPhoneLogs.Add(errorLog);
        }

        private async Task StopComsRangeIdleAsync(int gsmDeviceId, string rangeName)
        {
            await _cacheService.RemoveGsm512BlockedRangeName(gsmDeviceId, rangeName);
        }

        private async Task ReportGsmValue(RentCodeOrder order)
        {
            if (!order.ConnectedGsmId.HasValue) return;
            try
            {
                var date = _dateTimeService.GMT7Now().Date;
                var gsmReport = await _smsDataContext.GsmReports.FirstOrDefaultAsync(r => r.GsmId == order.ConnectedGsmId.Value
                && r.OrderStatus == order.Status && r.ReportedDate == date && r.ServiceProviderId == order.ServiceProviderId);
                if (gsmReport != null)
                {
                    gsmReport.Count++;
                }
                else
                {
                    gsmReport = new GsmReport()
                    {
                        Count = 1,
                        OrderStatus = order.Status,
                        ReportedDate = date,
                        GsmId = order.ConnectedGsmId.Value,
                        ServiceProviderId = order.ServiceProviderId
                    };
                    _smsDataContext.GsmReports.Add(gsmReport);
                }
                await _smsDataContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogInformation("gsmReport failed {0}", e.Message);

            }
        }
        private async Task WarningForAdmin(string adminEmail, int gsmDeviceId, PhoneFailedCount failedCount)
        {
            // temporary disable warning/alert feature
            try
            {
                var gsmDevice = await _smsDataContext.GsmDevices.FirstOrDefaultAsync(r => r.Id == gsmDeviceId);
                if (gsmDevice == null) return;
                var staffEmail = await _smsDataContext.UserGsmDevices.Where(r => r.GsmDeviceId == gsmDeviceId).Select(r => r.User.UserProfile.Email).FirstOrDefaultAsync();

                await _systemAlertService.RaiseAnAlert(new SystemAlert()
                {
                    Topic = "GsmDevice",
                    Thread = "ErrorGsmWarning",
                    DetailJson = JsonConvert.SerializeObject(new GsmWarningPayload()
                    {
                        ContinuosErrors = failedCount.ContinuousFailed,
                        GsmCode = gsmDevice.Code,
                        GsmName = gsmDevice.Name,
                        TotalErrors = failedCount.TotalFailed,
                        StaffEmail = staffEmail
                    }),
                });

                //var adminIds = _smsDataContext.Users.Where(r => r.Role == RoleType.Administrator && r.IsBanned != true).Select(r => r.Id).ToList();
                //foreach (var adminId in adminIds)
                //{
                //    var notice = new PhoneFailedNotification()
                //    {
                //        ContinuosFailed = failedCount.ContinuousFailed,
                //        GsmDeviceCode = gsmDevice.Code,
                //        GsmDeviceId = gsmDevice.Id,
                //        GsmDeviceName = gsmDevice.Name,
                //        IsRead = false,
                //        TotalFailed = failedCount.TotalFailed,
                //        UserId = adminId
                //    };
                //    _smsDataContext.PhoneFailedNotifications.Add(notice);
                //}
            }
            catch (Exception e)
            {
                _logger.LogError(e, "WarningForAdmin");
            }
        }

        private async Task AssignPhoneNumberToOrder()
        {
            _logger.LogInformation("Start AssignPhoneNumberToOrder");
            var floatingOrders = (await _smsDataContext.RentCodeOrders
                .Include(r => r.ServiceProvider)
                .Where(r => r.Status == OrderStatus.Floating)
                .ToListAsync()).OrderBy(r => r.ServiceProviderId).ToList();
            floatingOrders = floatingOrders.Where(r => string.IsNullOrEmpty(r.ProposedPhoneNumber)).ToList();
            if (floatingOrders.Count == 0) return;

            _logger.LogInformation("Floating orders: {0}", string.Join(", ", floatingOrders.Select(r => r.Id).ToArray()));
            var floatingServiceProviderIds = floatingOrders.Where(r => GetCachePendingPhone(r.Id) == null).Select(r => r.ServiceProviderId).Distinct().ToList();
            var serviceNetworkProvidersPreloaded = await LoadServiceNetworkProviders(floatingServiceProviderIds);
            _logger.LogInformation("Preloaded serviceNetworkProvidersPreloaded: {0}", serviceNetworkProvidersPreloaded.Count);

            var gsm512IdLists = await _cacheService.Gsm512IdList();

            var allComsNoSortable = await LoadComs();
            var allComs = allComsNoSortable.Select(r => new SortableCom()
            {
                SuccessedCount = 0,
                Com = r
            }).ToList();
            _logger.LogInformation("Preloaded allComs: {0}", allComs.Count);

            var activePhoneNumbers = await _smsDataContext
                .RentCodeOrders
                .Where(r => r.Status == OrderStatus.Waiting)
                .Select(r => r.PhoneNumber).ToListAsync();
            _logger.LogInformation("Preloaded activePhoneNumbers: {0}", activePhoneNumbers.Count);

            var userIds = floatingOrders.Select(r => r.UserId).Distinct().ToList();
            var users = await _smsDataContext.Users.Where(r => userIds.Contains(r.Id)).AsNoTracking().ToListAsync();
            _logger.LogInformation("Preloaded users: {0}", users.Count);

            var vietnamNetworks = new List<int>() { 1, 2, 3, 4 };
            var configuration = await _systemConfigurationService.GetSystemConfiguration();
            var autoCancelOrderDuration = configuration.AutoCancelOrderDuration ?? 2;

            var serviceProviderErrorPhoneLogs = await LoadServiceProviderErrorPhoneLogs(floatingServiceProviderIds);
            _logger.LogInformation("Preloaded serviceProviderErrorPhoneLogs: {0}", serviceProviderErrorPhoneLogs.Count);

            var gsmDeviceServiceProvidersPreloaded = await LoadGsmDeviceServiceProviders(floatingServiceProviderIds);
            _logger.LogInformation("Preloaded gsmDeviceServiceProvidersPreloaded: {0}", gsmDeviceServiceProvidersPreloaded.Count);

            var errorPhoneLogUsersPreloaded = await LoadErrorPhoneLogUsers(floatingServiceProviderIds);
            _logger.LogInformation("Preloaded errorPhoneLogUsersPreloaded: {0}", errorPhoneLogUsersPreloaded.Count);

            var successPhoneNumberByServicesPreloaded = await LoadSuccessedPhoneNumberByServiceProviders(floatingServiceProviderIds);
            _logger.LogInformation("Preloaded successPhoneNumberByServicesPreloaded: {0}", successPhoneNumberByServicesPreloaded.Count);

            foreach (var order in floatingOrders)
            {
                if (order.Created < _dateTimeService.UtcNow().AddMinutes(-autoCancelOrderDuration))
                {
                    order.Status = OrderStatus.OutOfService;
                    await _smsDataContext.SaveChangesAsync();
                    continue;
                }
                _logger.LogInformation("Assigning phone for order {0}", order.Id);
                var user = users.FirstOrDefault(r => r.Id == order.UserId);
                if (user.Ballance < order.Price)
                {
                    _logger.LogInformation("Not enough credits");
                    order.Status = OrderStatus.Error;
                    await _smsDataContext.SaveChangesAsync();
                    continue;
                }
                var serviceProviderErrorThreshold = order.ServiceProvider.ErrorThreshold ?? 2;
                var serviceProviderTotalErrorThreshold = order.ServiceProvider.TotalErrorThreshold ?? 2;
                var serviceProviderReceivingThreshold = order.ServiceProvider.ReceivingThreshold;
                _preloadOrderServiceProviderQueue.QueuePreloadService(order.ServiceProvider.Id);
                var coms = new List<SortableCom>();
                var pendingCache = GetCachePendingPhone(order.Id);
                if (pendingCache != null)
                {
                    _logger.LogInformation("Start with cache");

                    coms = allComs.Where(r => pendingCache.Contains(r.Com.PhoneNumber)).ToList();
                    _logger.LogInformation("Cache coms: " + coms.Count);
                    if (coms.Count == 0)
                    {
                        continue;
                    }

                    coms = coms.Where(r => !activePhoneNumbers.Contains(r.Com.PhoneNumber)).ToList();
                    _logger.LogInformation("Cache coms with active: " + coms.Count);
                    if (coms.Count == 0)
                    {
                        continue;
                    }
                }
                else
                {
                    #region filter callback & by time service
                    coms = (from com in allComs
                            where (order.ServiceProvider.ServiceType != ServiceType.Callback || com.Com.PhoneNumber == order.RequestPhoneNumber)
                            && (order.ServiceProvider.ServiceType != ServiceType.ByTime || com.Com.GsmDevice.IsServingForThirdService == true)
                            select com).ToList();
                    _logger.LogInformation("Total coms: " + coms.Count);
                    if (coms.Count == 0)
                    {
                        order.Status = OrderStatus.OutOfService;
                        await _smsDataContext.SaveChangesAsync();
                        continue;
                    }
                    #endregion

                    #region filter allow voice order
                    if (order.AllowVoiceSms)
                    {
                        coms = (from com in coms
                                where com.Com.GsmDevice.AllowVoiceOrder
                                select com).ToList();
                    }
                    #endregion

                    var servicePendingCachePhones = GetCachePendingPhoneForServiceProvider(order.ServiceProviderId);
                    if (servicePendingCachePhones != null && !order.OnlyAcceptFreshOtp)
                    {
                        coms = coms.Where(r => servicePendingCachePhones.Contains(r.Com.PhoneNumber)).ToList();
                        _logger.LogInformation("Coms from servicePendingCachePhones: " + coms.Count);
                        if (coms.Count == 0)
                        {
                            CachePendingPhoneForServiceProvider(order.ServiceProviderId, null);
                            order.Status = OrderStatus.OutOfService;
                            await _smsDataContext.SaveChangesAsync();
                            continue;
                        }
                    }
                    else
                    {
                        #region Filter by is network allow in service
                        var orderServiceNetworkProviders = serviceNetworkProvidersPreloaded.Where(r => r.ServiceProviderId == order.ServiceProviderId).ToList();
                        var networks = orderServiceNetworkProviders.Count == 0 ? vietnamNetworks
                            : orderServiceNetworkProviders.Select(r => r.NetworkProviderId).ToList();
                        coms = coms.Where(r => r.Com.NetworkProvider == null || networks.Contains(r.Com.NetworkProvider.Value)).ToList();
                        _logger.LogInformation("Network match: " + coms.Count);
                        if (coms.Count == 0)
                        {
                            order.Status = OrderStatus.OutOfService;
                            await _smsDataContext.SaveChangesAsync();
                            continue;
                        }
                        #endregion

                        #region Filter by is gsm allow service
                        var specifiedServiceGsmIds = coms.Where(r => r.Com.GsmDevice.SpecifiedService == true).Select(r => r.Com.GsmDeviceId).ToList();
                        var gsmDeviceServiceProviders = gsmDeviceServiceProvidersPreloaded
                            .Where(r => r.ServiceProviderId == order.ServiceProviderId && specifiedServiceGsmIds.Contains(r.GsmDeviceId)).Select(r => r.GsmDeviceId).ToList();

                        coms = coms.Where(com => com.Com.GsmDevice.SpecifiedService != true || gsmDeviceServiceProviders.Contains(com.Com.GsmDeviceId)).ToList();

                        _logger.LogInformation("SpecifiedService coms: " + coms.Count);
                        if (coms.Count == 0)
                        {
                            order.Status = OrderStatus.OutOfService;
                            await _smsDataContext.SaveChangesAsync();
                            continue;
                        }
                        #endregion
                        #region Filter by is phone got error
                        var phoneHasDied = (from t in serviceProviderErrorPhoneLogs
                                            where t.ServiceProviderId == order.ServiceProviderId && t.IsActive == true
                                            select t.PhoneNumber).ToList();

                        coms = coms.Where(r => !phoneHasDied.Contains(r.Com.PhoneNumber)).ToList();
                        _logger.LogInformation("Coms without toal error: " + coms.Count);
                        if (coms.Count == 0)
                        {
                            order.Status = OrderStatus.OutOfService;
                            await _smsDataContext.SaveChangesAsync();
                            continue;
                        }
                        #endregion
                        #region Filter done service
                        if (order.ServiceProvider.ServiceType == ServiceType.Any || order.ServiceProvider.ServiceType == ServiceType.Basic)
                        {
                            var availablePhoneFromCache = await _cacheService.GetAvailablePhoneNumberForServiceProvider(order.ServiceProviderId);
                            if (availablePhoneFromCache != null && !order.OnlyAcceptFreshOtp)
                            {
                                coms = coms.Where(r => availablePhoneFromCache.Contains(r.Com.PhoneNumber)).ToList();
                                _logger.LogInformation("Coms is not used cache: " + coms.Count);
                                if (coms.Count == 0)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                var successPhoneNumberByService = successPhoneNumberByServicesPreloaded.Where(r => r.ServiceProviderId == order.ServiceProviderId).ToList();
                                var checkingPhoneNumbers = allComs.Select(r => r.Com.PhoneNumber).Distinct().ToList();
                                var receivingThresholdComputeWithFreshOtp = order.OnlyAcceptFreshOtp ? 1 : serviceProviderReceivingThreshold;
                                var successedPhoneCount = successPhoneNumberByService
                                    .Where(o => order.ServiceProviderId == o.ServiceProviderId)
                                    .Where(r => checkingPhoneNumbers.Contains(r.PhoneNumber))
                                    .GroupBy(r => r.PhoneNumber)
                                    .Where(gr => gr.Count() >= receivingThresholdComputeWithFreshOtp)
                                    .Select(gr => gr.Key)
                                    .ToList();
                                coms = coms.Where(r => !successedPhoneCount.Contains(r.Com.PhoneNumber)).ToList();
                                await _cacheService.SetAvailablePhoneNumberForServiceProvider(order.ServiceProviderId, coms.Select(r => r.Com.PhoneNumber).ToList());

                                _logger.LogInformation("Coms is not used: " + coms.Count);
                                if (coms.Count == 0)
                                {
                                    order.Status = OrderStatus.OutOfService;
                                    await _smsDataContext.SaveChangesAsync();
                                    continue;
                                }
                                if (order.OnlyAcceptFreshOtp)
                                {
                                    var phoneListSortBySuccessedCount = successPhoneNumberByService
                                        .Where(o => order.ServiceProviderId == o.ServiceProviderId)
                                        .Where(r => checkingPhoneNumbers.Contains(r.PhoneNumber))
                                        .GroupBy(r => r.PhoneNumber)
                                        .Where(gr => gr.Count() < receivingThresholdComputeWithFreshOtp)
                                        .Select(gr => new { gr.Key, Count = gr.Count() })
                                        .ToList();
                                    foreach (var c in coms)
                                    {
                                        c.SuccessedCount = (phoneListSortBySuccessedCount.FirstOrDefault(r => r.Key == c.Com.PhoneNumber)?.Count).GetValueOrDefault();
                                    }
                                }
                            }
                        }
                        #endregion
                        CachePendingPhoneForServiceProvider(order.ServiceProviderId,
                            coms.Select(r => r.Com.PhoneNumber).Where(r => !activePhoneNumbers.Contains(r)).ToList());
                    }
                    #region Filter by requested network in order
                    coms = coms.Where(com => order.NetworkProvider == null || order.NetworkProvider == com.Com.NetworkProvider).ToList();
                    _logger.LogInformation("Coms is matched order network: " + coms.Count);
                    if (coms.Count == 0)
                    {
                        order.Status = OrderStatus.OutOfService;
                        await _smsDataContext.SaveChangesAsync();
                        continue;
                    }
                    #endregion

                    #region Filter by app source type
                    if (order.AppSourceType == AppSourceType.Api)
                    {
                        coms = coms.Where(r => r.Com.GsmDevice.OnlyWebOrder == false).ToList();
                        _logger.LogInformation("Coms for api only: " + coms.Count);
                        if (coms.Count == 0)
                        {
                            order.Status = OrderStatus.OutOfService;
                            await _smsDataContext.SaveChangesAsync();
                            continue;
                        }
                    }
                    #endregion

                    #region Filter by user error
                    var phoneHasDiedOnUser = (from eu in errorPhoneLogUsersPreloaded
                                              where eu.UserId == order.UserId
                                                    && eu.ServiceProviderId == order.ServiceProviderId
                                              select eu.PhoneNumber).ToList();

                    coms = coms.Where(r => !phoneHasDiedOnUser.Contains(r.Com.PhoneNumber)).ToList();

                    _logger.LogInformation("Final coms: " + coms.Count);
                    if (coms.Count == 0)
                    {
                        order.Status = OrderStatus.OutOfService;
                        await _smsDataContext.SaveChangesAsync();
                        continue;
                    }
                    #endregion

                    CachePendingPhone(order.Id, coms.Select(r => r.Com.PhoneNumber).ToList(), autoCancelOrderDuration);
                    #region Filter by active phone numbers
                    coms = coms.Where(r => !activePhoneNumbers.Contains(r.Com.PhoneNumber)).ToList();
                    _logger.LogInformation("Coms is not active: " + coms.Count);
                    if (coms.Count == 0)
                    {
                        continue;
                    }
                    #endregion
                }

                #region Filter by idle for GSM512
                var gsm512Ids = coms.Where(r => gsm512IdLists.Contains(r.Com.GsmDeviceId)).Select(r => r.Com.GsmDeviceId).ToList();
                if (gsm512Ids.Count > 0)
                {
                    var blockedRangeNamesByGsmIdsTasks = gsm512Ids.Distinct().Select(async r =>
                    {
                        return new
                        {
                            Id = r,
                            BlockedRanges = await _cacheService.GetGsm512BlockedRangeNames(r)
                        };
                    });
                    var blockedRangeNamesByGsmIds = (await Task.WhenAll(blockedRangeNamesByGsmIdsTasks)).ToList();
                    coms = coms.Where(com =>
                    {
                        if (!gsm512IdLists.Contains(com.Com.GsmDeviceId)) return true;
                        var rangeNames = blockedRangeNamesByGsmIds.FirstOrDefault(r => r.Id == com.Com.GsmDeviceId)?.BlockedRanges ?? new List<string>();
                        return !rangeNames.Any(x => com.Com.ComName.StartsWith(x));
                    }).ToList();

                    _logger.LogInformation("Coms is not idle: " + coms.Count);
                    if (coms.Count == 0)
                    {
                        continue;
                    }
                }
                #endregion

                SortableCom sortableCom = null;
                if (order.OnlyAcceptFreshOtp)
                {
                    sortableCom = coms.OrderByDescending(r => r.SuccessedCount).ThenByDescending(r => r.Com.GsmDevice.Priority).ThenBy(r => Guid.NewGuid()).FirstOrDefault();
                }
                else
                {
                    sortableCom = coms.OrderByDescending(r => r.Com.GsmDevice.Priority).ThenBy(r => Guid.NewGuid()).FirstOrDefault();
                }
                var comObject = sortableCom?.Com;

                var phoneNumber = comObject?.PhoneNumber;
                var cachePendingPhonesForService = GetCachePendingPhoneForServiceProvider(order.ServiceProviderId);
                if (cachePendingPhonesForService != null)
                {
                    cachePendingPhonesForService = cachePendingPhonesForService.Where(r => r != phoneNumber).ToList();
                    CachePendingPhoneForServiceProvider(order.ServiceProviderId, cachePendingPhonesForService);
                }
                _logger.LogInformation("Available phone number {0}", phoneNumber);
                if (string.IsNullOrEmpty(phoneNumber)) continue;
                activePhoneNumbers.Add(phoneNumber);
                order.ProposedPhoneNumber = phoneNumber;
                order.ConnectedGsmId = comObject.GsmDeviceId;
                if (gsm512IdLists.Contains(comObject.GsmDeviceId))
                {
                    var prefix = GetPrefixOfComName(comObject.ComName);
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        await _cacheService.AddGsm512BlockedRangeName(comObject.GsmDeviceId, prefix);
                        order.ProposedGsm512RangeName = prefix;
                    }
                }
                await _smsDataContext.SaveChangesAsync();
            }
            _logger.LogInformation("End AssignPhoneNumberToOrder");
        }

        private async Task ProcessPreloadServiceProvider()
        {
            var floatingServiceProviderIds = _preloadOrderServiceProviderQueue.DequeueAllPreloadService().ToList();
            if (floatingServiceProviderIds == null || floatingServiceProviderIds.Count == 0) return;
            _logger.LogInformation(string.Format("Preload service providers {0} - {1}", floatingServiceProviderIds.Count, string.Join(", ", floatingServiceProviderIds)));
            await LoadComs(true);
            await LoadServiceNetworkProviders(floatingServiceProviderIds, true);
            await LoadServiceProviderErrorPhoneLogs(floatingServiceProviderIds, true);
            await LoadGsmDeviceServiceProviders(floatingServiceProviderIds, true);
            await LoadErrorPhoneLogUsers(floatingServiceProviderIds, true);
            await LoadSuccessedPhoneNumberByServiceProviders(floatingServiceProviderIds, true);
        }
        private string GetPrefixOfComName(string comName)
        {
            if (string.IsNullOrEmpty(comName)) return null;
            var prefix = comName.Split(".").FirstOrDefault();
            if (string.IsNullOrEmpty(prefix)) return null;
            return prefix + ".";
        }

        private async Task<List<SuccessedPhoneNumberByServiceProviderPreloadedModel>> LoadSuccessedPhoneNumberByServiceProviders(List<int> serviceProviderIds, bool isPreloading = false)
        {
            if (serviceProviderIds.Count == 0) return new List<SuccessedPhoneNumberByServiceProviderPreloadedModel>();
            Func<int, Task<List<SuccessedPhoneNumberByServiceProviderPreloadedModel>>> fetchFunc
            = async serviceProviderId => await (from com in _smsDataContext.Coms
                                                join o in _smsDataContext.RentCodeOrders on com.PhoneNumber equals o.PhoneNumber
                                                where com.Disabled != true && !string.IsNullOrEmpty(com.PhoneNumber) && com.GsmDevice.Disabled != true
                                                  && com.GsmDevice.IsInMaintenance != true && (o.Status == OrderStatus.Success)
                                                  && o.ServiceProviderId == serviceProviderId
                                                select new SuccessedPhoneNumberByServiceProviderPreloadedModel()
                                                { PhoneNumber = o.PhoneNumber, ServiceProviderId = o.ServiceProviderId }
                            ).ToListAsync();
            var tasks = serviceProviderIds
            .Select(async serviceProviderId =>
            {
                var cacheKey = $"SUCCESSED_PHONE_NUMBER_BY_SERVICE_PROVIDER_CACHE_KEY_{serviceProviderId}";
                List<SuccessedPhoneNumberByServiceProviderPreloadedModel> values = null;
                if (isPreloading)
                {
                    values = await fetchFunc(serviceProviderId);
                    _memoryCache.Remove(cacheKey);
                }
                return await _memoryCache.GetOrCreateAsync(cacheKey, async settings =>
          {
              settings.SetAbsoluteExpiration(PreloadCacheTimeOut);
              return values ?? await fetchFunc(serviceProviderId);
          });
            }).ToList();
            var dist = await Task.WhenAll(tasks);
            return dist.SelectMany(r => r).ToList();
        }

        private async Task<List<ErrorPhoneLogUserPreloadedModel>> LoadErrorPhoneLogUsers(List<int> serviceProviderIds, bool isPreloading = false)
        {
            if (serviceProviderIds.Count == 0) return new List<ErrorPhoneLogUserPreloadedModel>();
            Func<int, Task<List<ErrorPhoneLogUserPreloadedModel>>> fetchFunc = async serviceProviderId =>
            await (from eu in _smsDataContext.ErrorPhoneLogUsers
                   join com in _smsDataContext.Coms on eu.ErrorPhoneLog.PhoneNumber equals com.PhoneNumber
                   where eu.IsIgnored != true
                       && eu.ErrorPhoneLog.ServiceProviderId == serviceProviderId
                         && com.Disabled != true
                         && !string.IsNullOrEmpty(com.PhoneNumber)
                         && com.GsmDevice.Disabled != true
                         && com.GsmDevice.IsInMaintenance != true
                   select new ErrorPhoneLogUserPreloadedModel
                   {
                       UserId = eu.UserId,
                       PhoneNumber = eu.ErrorPhoneLog.PhoneNumber,
                       ServiceProviderId = eu.ErrorPhoneLog.ServiceProviderId
                   })
                              .ToListAsync();
            var tasks = serviceProviderIds
            .Select(async x =>
            {
                var serviceProviderId = x;
                var cacheKey = $"ERROR_PHONE_LOG_USER_CACHE_KEY_{serviceProviderId}";
                List<ErrorPhoneLogUserPreloadedModel> values = null;
                if (isPreloading)
                {
                    values = await fetchFunc(serviceProviderId);
                    _memoryCache.Remove(cacheKey);
                }
                return await _memoryCache.GetOrCreateAsync(cacheKey, async settings =>
          {
              settings.SetAbsoluteExpiration(PreloadCacheTimeOut);
              return values ?? await fetchFunc(serviceProviderId);
          });
            })
            .ToList();
            var dist = await Task.WhenAll(tasks);
            return dist.SelectMany(r => r).ToList();
        }

        private async Task<List<Com>> LoadComs(bool isPreloading = false)
        {
            var cacheKey = "COMS_FOR_SERVICE_CACHE_KEY";
            Func<Task<List<Com>>> fetchFunc = async () =>
            await (from com in _smsDataContext.Coms.Include(r => r.GsmDevice)
                   where com.Disabled != true && !string.IsNullOrEmpty(com.PhoneNumber) && com.GsmDevice.Disabled != true
                         && com.GsmDevice.IsInMaintenance != true
                   select com).AsNoTracking().ToListAsync();
            List<Com> values = null;
            if (isPreloading)
            {
                values = await fetchFunc();
                _memoryCache.Remove(cacheKey);
            }
            return await _memoryCache.GetOrCreateAsync(cacheKey, async settings =>
            {
                settings.SetAbsoluteExpiration(PreloadCacheTimeOut);
                return values ?? await fetchFunc();
            });
        }

        private async Task<List<ServiceNetworkProvider>> LoadServiceNetworkProviders(List<int> floatingServiceProviderIds, bool isPreloading = false)
        {
            if (floatingServiceProviderIds.Count == 0) return new List<ServiceNetworkProvider>();
            Func<int, Task<List<ServiceNetworkProvider>>> fetchFunc = async serviceProviderId =>
            await _smsDataContext
                                .ServiceNetworkProviders
                                .Where(r => r.ServiceProviderId == serviceProviderId)
                                .AsNoTracking()
                                .ToListAsync();
            var tasks = floatingServiceProviderIds.Select(async serviceProviderId =>
            {
                var cacheKey = $"SERVICE_NETWORK_PROVIDERS_CACHE_KEY_{serviceProviderId}";
                List<ServiceNetworkProvider> values = null;
                if (isPreloading)
                {
                    values = await fetchFunc(serviceProviderId);
                    _memoryCache.Remove(cacheKey);
                }
                return await _memoryCache.GetOrCreateAsync(cacheKey, async settings =>
          {
              settings.SetAbsoluteExpiration(PreloadCacheTimeOut);
              return values ?? await fetchFunc(serviceProviderId);
          });
            }).ToList();
            var dist = await Task.WhenAll(tasks);
            return dist.Aggregate(new List<ServiceNetworkProvider>(), (current, next) => current.Union(next).ToList());
        }

        private async Task<List<ErrorPhoneLog>> LoadServiceProviderErrorPhoneLogs(List<int> floatingServiceProviderIds, bool isPreloading = false)
        {
            if (floatingServiceProviderIds.Count == 0) return new List<ErrorPhoneLog>();
            Func<int, Task<List<ErrorPhoneLog>>> fetchFunc = async serviceProviderId =>
            await (from r in _smsDataContext.ErrorPhoneLogs
                   join com in _smsDataContext.Coms on r.PhoneNumber equals com.PhoneNumber
                   where r.ServiceProviderId == serviceProviderId && r.IsActive == true
                       && com.Disabled != true
                       && !string.IsNullOrEmpty(com.PhoneNumber)
                       && com.GsmDevice.Disabled != true
                       && com.GsmDevice.IsInMaintenance != true
                   select r).AsNoTracking().ToListAsync();
            var tasks = floatingServiceProviderIds.Select(async serviceProviderId =>
            {
                var cacheKey = $"SERVICE_PROVIDER_ERROR_PHONE_LOGS_CACHE_KEY_{serviceProviderId}";
                List<ErrorPhoneLog> values = null;
                if (isPreloading)
                {
                    values = await fetchFunc(serviceProviderId);
                    _memoryCache.Remove(cacheKey);
                }
                return await _memoryCache.GetOrCreateAsync(cacheKey, async settings =>
          {
              settings.SetAbsoluteExpiration(PreloadCacheTimeOut);
              return values ?? await fetchFunc(serviceProviderId);
          });
            });
            var dist = await Task.WhenAll(tasks);
            return dist.Aggregate(new List<ErrorPhoneLog>(), (current, next) => current.Union(next).ToList());
        }

        private async Task<List<GsmDeviceServiceProvider>> LoadGsmDeviceServiceProviders(List<int> floatingServiceProviderIds, bool isPreloading = false)
        {
            if (floatingServiceProviderIds.Count == 0) return new List<GsmDeviceServiceProvider>();
            Func<int, Task<List<GsmDeviceServiceProvider>>> fetchFunc = async serviceProviderId =>
            await _smsDataContext.GsmDeviceServiceProviders
                .Where(r => r.ServiceProviderId == serviceProviderId)
                .AsNoTracking()
                .ToListAsync();

            var tasks = floatingServiceProviderIds.Select(async serviceProviderId =>
            {
                var cacheKey = $"GSM_DEVICE_SERVICE_PROVIDERS_CACHE_KEY_{serviceProviderId}";
                List<GsmDeviceServiceProvider> values = null;
                if (isPreloading)
                {
                    values = await fetchFunc(serviceProviderId);
                    _memoryCache.Remove(cacheKey);
                }
                return await _memoryCache.GetOrCreateAsync(cacheKey, async settings =>
          {
              settings.SetAbsoluteExpiration(PreloadCacheTimeOut);
              return values ?? await fetchFunc(serviceProviderId);
          });
            });
            var dist = await Task.WhenAll(tasks);
            return dist.Aggregate(new List<GsmDeviceServiceProvider>(), (current, next) => current.Union(next).ToList());
        }

        private async Task ProcessProposedPhoneNumber()
        {
            var floatingOrders = (await _smsDataContext.RentCodeOrders
                .Where(r => r.Status == OrderStatus.Floating)
                .ToListAsync()).OrderBy(r => r.ServiceProviderId).ToList();
            floatingOrders = floatingOrders.Where(r => !string.IsNullOrEmpty(r.ProposedPhoneNumber)).ToList();
            if (floatingOrders.Count == 0) return;

            var activePhoneNumbers = await _smsDataContext
                .RentCodeOrders
                .Where(r => r.Status == OrderStatus.Waiting)
                .Select(r => r.PhoneNumber).ToListAsync();

            var userIds = floatingOrders.Select(r => r.UserId).Distinct().ToList();
            var users = await _smsDataContext.Users.Where(r => userIds.Contains(r.Id)).AsNoTracking().ToListAsync();

            var serviceProviderIds = floatingOrders.Select(r => r.ServiceProviderId).ToList();
            var serviceProviders = (await _cacheService.GetAllServiceProviders()).Where(r => serviceProviderIds.Contains(r.Id)).ToList();

            foreach (var order in floatingOrders)
            {
                var proposedPhoneNumber = order.ProposedPhoneNumber;
                var serviceProvider = serviceProviders.FirstOrDefault(r => r.Id == order.ServiceProviderId);
                var user = users.FirstOrDefault(r => r.Id == order.UserId);
                if (serviceProvider == null)
                {
                    continue;
                }
                if (activePhoneNumbers.Contains(proposedPhoneNumber))
                {
                    continue;
                }
                if (serviceProvider.ServiceType == ServiceType.Basic || serviceProvider.ServiceType == ServiceType.Any)
                {
                    var receivingThresholdComputeWithFreshOtp = order.OnlyAcceptFreshOtp ? 1 : serviceProvider.ReceivingThreshold;
                    if (await _smsDataContext.RentCodeOrders.CountAsync(r =>
                         r.Status == OrderStatus.Success &&
                         r.PhoneNumber == proposedPhoneNumber &&
                         r.ServiceProviderId == serviceProvider.Id
                    ) >= receivingThresholdComputeWithFreshOtp)
                    {
                        continue;
                    }
                }
                if (!string.IsNullOrEmpty(order.ProposedGsm512RangeName))
                {
                    if (await _smsDataContext.RentCodeOrders.AnyAsync(r => r.ConnectedGsmId == order.ConnectedGsmId && r.ProposedGsm512RangeName == order.ProposedGsm512RangeName && r.Status == OrderStatus.Waiting))
                    {
                        order.ProposedGsm512RangeName = null;
                        continue;
                    }
                }
                order.Status = OrderStatus.Waiting;
                order.PhoneNumber = proposedPhoneNumber;
                order.Expired = _dateTimeService.UtcNow().AddMinutes(order.LockTime);
                activePhoneNumbers.Add(proposedPhoneNumber);
                var transaction = new UserTransaction()
                {
                    Amount = order.Price,
                    Comment = $"Paid for <{serviceProvider.Name}>",
                    IsImport = false,
                    UserId = order.UserId,
                    OrderId = order.Id,
                    UserTransactionType = UserTransactionType.PaidForService,
                    Balance = user.Ballance
                };
                _smsDataContext.UserTransactions.Add(transaction);
                await _smsDataContext.SaveChangesAsync();
                _logger.LogInformation("RentCodeOrder {0}, Phone {1} -> Assigning successed", order.Id, proposedPhoneNumber);
            }
            foreach (var user in users)
            {
                var sum = floatingOrders.Where(r => r.UserId == user.Id && r.Status == OrderStatus.Waiting).Sum(r => r.Price);
                if (sum > 0)
                {
                    _logger.LogInformation("User {0}, Sum {1} -> Fee", user.Id, sum);
                    await UpdateUserBallance(user.Id, -1 * sum);
                }
            }
            var unfinalizedOrders = floatingOrders.Where(r => r.Status == OrderStatus.Floating).ToList();
            if (unfinalizedOrders.Count > 0)
            {
                foreach (var unfinalized in unfinalizedOrders)
                {
                    if (!string.IsNullOrEmpty(unfinalized.ProposedGsm512RangeName))
                    {
                        await _cacheService.RemoveGsm512BlockedRangeName(unfinalized.ConnectedGsmId.Value, unfinalized.ProposedGsm512RangeName);
                    }
                    unfinalized.ProposedPhoneNumber = null;
                    unfinalized.ConnectedGsmId = null;
                    unfinalized.ProposedGsm512RangeName = null;
                }
                await _smsDataContext.SaveChangesAsync();
            }
        }

        private void CachePendingPhone(int orderId, List<string> phoneNumbers, int autoCancelOrderDuration)
        {
            var cacheKey = string.Format(ORDER_PENDING_PHONE_CACHE_KEY, orderId);
            _memoryCache.CreateEntry(cacheKey);
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(autoCancelOrderDuration * 60 + 10));
            _memoryCache.Set(cacheKey, phoneNumbers, cacheEntryOptions);
        }

        private void CachePendingPhoneForServiceProvider(int serviceProviderId, List<string> phoneNumbers)
        {
            var expired = _dateTimeService.UtcNow().AddSeconds(10);
            if (_memoryCache.TryGetValue(SERVICE_PROVIDER_PENDING_PHONE_CACHE_KEY(serviceProviderId), out CacheObject<List<string>> results))
            {
                expired = results.Expired;
            }
            var cacheEntryOptions = new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = PreloadCacheTimeOut
            };
            _memoryCache.Set(SERVICE_PROVIDER_PENDING_PHONE_CACHE_KEY(serviceProviderId), new CacheObject<List<string>>()
            {
                Expired = expired,
                Object = phoneNumbers
            }, cacheEntryOptions);
        }

        private List<string> GetCachePendingPhone(int orderId)
        {
            var cacheKey = string.Format(ORDER_PENDING_PHONE_CACHE_KEY, orderId);
            if (_memoryCache.TryGetValue(cacheKey, out List<string> results))
            {
                return results;
            }
            return null;
        }

        private List<string> GetCachePendingPhoneForServiceProvider(int serviceProviderId)
        {
            var cacheKey = SERVICE_PROVIDER_PENDING_PHONE_CACHE_KEY(serviceProviderId);
            if (_memoryCache.TryGetValue(cacheKey, out CacheObject<List<string>> results))
            {
                if (results.Expired < _dateTimeService.UtcNow())
                {
                    return null;
                }
                return results.Object;
            }
            return null;
        }

        public override void Map(RentCodeOrder entity, RentCodeOrder model)
        {
            entity.Status = model.Status;
        }

        public async Task<ApiResponseBaseModel<RentCodeOrder>> RequestAOrder(int userId, int serviceProviderId, int? networkProvider, int? maximumSms, AppSourceType appSourceType, bool onlyAcceptFreshOtp, bool AllowVoiceSms)
        {
            var user = await _userService.Get(userId);
            if (user == null) return new ApiResponseBaseModel<RentCodeOrder>
            {
                Success = false,
                Message = "UserNotFound"
            };
            if (await _smsDataContext.RentCodeOrders.CountAsync(r => r.Status == OrderStatus.Floating) > 150)
            {
                return new ApiResponseBaseModel<RentCodeOrder>()
                {
                    Success = false,
                    Message = "SystemOverload"
                };
            }
            if (await _smsDataContext.RentCodeOrders.CountAsync(r => r.ServiceProviderId == serviceProviderId && r.Status == OrderStatus.Floating) > 100)
            {
                return new ApiResponseBaseModel<RentCodeOrder>()
                {
                    Success = false,
                    Message = "ServiceOverload"
                };
            }
            var serviceProvider = await _serviceProviderService.GetInCache(serviceProviderId);
            if (serviceProvider == null)
            {
                return new ApiResponseBaseModel<RentCodeOrder>()
                {
                    Success = false,
                    Message = "ServiceProviderNotFound"
                };
            }
            if (serviceProvider.Disabled)
            {
                return new ApiResponseBaseModel<RentCodeOrder>()
                {
                    Success = false,
                    Message = "ServiceProviderIsDisabled"
                };
            }
            if (serviceProvider.ReceivingThreshold <= 1)
            {
                onlyAcceptFreshOtp = false;
            }
            if (user.Ballance < serviceProvider.Price)
            {
                return new ApiResponseBaseModel<RentCodeOrder>()
                {
                    Success = false,
                    Message = "NotEnoughCredit"
                };
            }
            var systemConfiguration = await _systemConfigurationService.GetSystemConfiguration();
            if (systemConfiguration != null && systemConfiguration.MaximumAvailableOrder != 0)
            {
                var userOrderCount = await _smsDataContext.RentCodeOrders
                    .CountAsync(r => r.UserId == userId && (r.Status == OrderStatus.Floating || r.Status == OrderStatus.Waiting));

                if (userOrderCount >= systemConfiguration.MaximumAvailableOrder)
                {
                    return new ApiResponseBaseModel<RentCodeOrder>()
                    {
                        Success = false,
                        Message = "MaximumOrder"
                    };
                }
            }
            if (networkProvider == 0) networkProvider = null;
            var lockTime = serviceProvider.LockTime;
            // increase lock time by 60% if maximumSms is more than 1
            // ex: if lock time is 5 minutes, it will be 8 if user request more than 1 SMS
            lockTime += (int)Math.Ceiling((maximumSms ?? 1) > 1 ? 0.6 * lockTime : 0);
            var price = serviceProvider.Price;
            if (onlyAcceptFreshOtp)
            {
                price = price * 1.2m;
            }
            var order = new RentCodeOrder()
            {
                Price = price,
                ServiceProviderId = serviceProvider.Id,
                Status = OrderStatus.Floating,
                UserId = user.Id,
                LockTime = lockTime,
                NetworkProvider = networkProvider,
                MaximunSms = maximumSms,
                RemainingSms = maximumSms,
                AppSourceType = appSourceType,
                OnlyAcceptFreshOtp = onlyAcceptFreshOtp,
                AllowVoiceSms = AllowVoiceSms,
                VoiceSmsPrice = serviceProvider.PriceReceiveCall
            };
            await Create(order);
            return new ApiResponseBaseModel<RentCodeOrder>()
            {
                Success = true,
                Results = order
            };
        }

        protected override async Task<List<RentCodeOrder>> PagingResultsMap(List<RentCodeOrder> entities)
        {
            var gsm512IdLists = await _cacheService.Gsm512IdList();
            foreach (var entity in entities)
            {
                if (entity.Status == OrderStatus.Waiting
                    && entity.ConnectedGsmId.HasValue
                    && entity.Updated > _dateTimeService.UtcNow().AddSeconds(-16)
                    && gsm512IdLists.Contains(entity.ConnectedGsmId.Value))
                {
                    entity.PhoneNumber = "";
                }
            }
            return entities;
        }

        protected override IQueryable<RentCodeOrder> GenerateQuery(FilterRequest filterRequest = null)
        {
            var query = base.GenerateQuery(filterRequest).AsNoTracking();
            query = query.Include(r => r.ServiceProvider).Include("User.UserProfile").Include(r => r.OrderResults);
            bool ignoreDateTime = false;
            if (filterRequest != null)
            {
                var userId = 0;
                {
                    if (filterRequest.SearchObject.TryGetValue("UserId", out object obj))
                    {
                        userId = int.Parse(obj.ToString());
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("Guid", out object obj))
                    {
                        ignoreDateTime = true;
                        query = query.Where(r => r.Guid == (string)obj);
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("username", out object obj))
                    {
                        var username = ((string)obj);
                        if (!string.IsNullOrEmpty(username))
                        {
                            username = username.ToLower();
                            query = query.Where(r => r.User != null && r.User.Username.Contains(username));
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("GsmDeviceIds", out object obj))
                    {
                        var ids = obj as List<int>;
                        if (ids != null && ids.Count > 0)
                        {
                            query = query.Where(r => ids.Any(k => k == r.ConnectedGsmId));
                        }
                    }
                }
                DateTime? fromDate = null;
                DateTime? toDate = null;
                {
                    if (filterRequest.SearchObject.TryGetValue("CreatedFrom", out object obj))
                    {
                        fromDate = (DateTime)obj;
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("CreatedTo", out object obj))
                    {
                        toDate = (DateTime)obj;
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("ServiceType", out object obj))
                    {
                        var ts = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<ServiceType>>();
                        if (ts.Count > 0)
                        {
                            query = query.Where(r => ts.Contains(r.ServiceProvider.ServiceType));
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("Status", out object obj))
                    {
                        var ts = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<OrderStatus>>();
                        if (ts.Count > 0)
                        {
                            query = query.Where(r => ts.Contains(r.Status));
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("AppSourceTypes", out object obj))
                    {
                        var ts = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<AppSourceType>>();
                        if (ts.Count > 0)
                        {
                            query = query.Where(r => ts.Contains(r.AppSourceType));
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("NetworkProviders", out object obj))
                    {
                        var ts = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<int?>>();
                        if (ts.Count > 0)
                        {
                            query = query.Where(r => ts.Contains(r.NetworkProvider));
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("allowVoiceSms", out object obj))
                    {
                        if (obj != null)
                        {
                            query = query.Where(r => r.AllowVoiceSms == (bool)obj);
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("serviceProviderIds", out object obj))
                    {
                        var ts = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<int>>();
                        if (ts.Count > 0)
                        {
                            query = query.Where(r => ts.Contains(r.ServiceProviderId));
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("phoneNumber", out object obj))
                    {
                        var phoneNumber = obj.ToString();
                        if (!string.IsNullOrWhiteSpace(phoneNumber))
                        {
                            phoneNumber = phoneNumber.ToLower();
                            query = query.Where(r => r.PhoneNumber.Contains(phoneNumber));
                        }
                    }
                }

                {
                    if (filterRequest.SearchObject.TryGetValue("GsmIds", out object obj))
                    {
                        var gsmIds = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<int?>>();

                        if (gsmIds != null && gsmIds.Count > 0)
                        {
                            query = query.Where(r => gsmIds.Contains(r.ConnectedGsmId));
                        }
                    }
                }

                if (userId != 0)
                {
                    query = query.Where(r => r.UserId == userId);
                }
                if (!ignoreDateTime)
                {
                    if (fromDate != null)
                    {
                        query = query.Where(r => r.Created >= fromDate);
                    }
                    if (toDate != null)
                    {
                        query = query.Where(r => r.Created < toDate);
                    }
                }
            }
            return query;
        }
        public async Task<ApiResponseBaseModel> CloseOrder(int orderId)
        {
            var order = await Get(orderId);
            if (order == null) return new ApiResponseBaseModel<OrderComplaint>()
            {
                Success = false,
                Message = "OrderNotFound"
            };

            if (order.Status != OrderStatus.Floating && order.Status != OrderStatus.Waiting)
            {
                return new ApiResponseBaseModel<OrderComplaint>()
                {
                    Success = false,
                    Message = "StatusInvalid"
                };
            }
            var needReturn = false;
            if (order.Status == OrderStatus.Waiting && order.OrderResults.Count == 0)
            {
                needReturn = true;
                var transaction = _smsDataContext.UserTransactions.FirstOrDefault(r => r.OrderId == order.Id);
                if (transaction != null)
                {
                    _smsDataContext.UserTransactions.Remove(transaction);
                }
            }
            order.Status = OrderStatus.Cancelled;
            var gsm512IdLists = await _cacheService.Gsm512IdList();
            if (order.ConnectedGsmId.HasValue && gsm512IdLists.Contains(order.ConnectedGsmId.Value))
            {
                await StopComsRangeIdleAsync(order.ConnectedGsmId.Value, order.ProposedGsm512RangeName);
            }

            await _smsDataContext.SaveChangesAsync();

            if (needReturn)
            {
                await UpdateUserBallance(order.UserId, order.Price);
            }
            return new ApiResponseBaseModel()
            {
                Success = true
            };
        }

        public async Task<ApiResponseBaseModel<RentCodeOrder>> AssignResult(string phoneNumber, string message, string sender, int smsHistoryId)
        {
            var now = _dateTimeService.UtcNow();
            var availableOrder = await _smsDataContext.RentCodeOrders.Include(r => r.ServiceProvider)
                .FirstOrDefaultAsync(r => r.Status == OrderStatus.Waiting & r.Expired > now && r.PhoneNumber == phoneNumber);
            if (availableOrder == null) return new ApiResponseBaseModel<RentCodeOrder>() { Success = false, Message = "NoAvailableOrder" };
            if (availableOrder.ServiceProvider == null) return new ApiResponseBaseModel<RentCodeOrder>() { Success = false, Message = "AvailableOrderDoesNotHaveService" };
            var matchingResult = await CheckMessageIsMatchWithService(availableOrder.ServiceProvider, message, sender);
            if (!matchingResult.Success)
            {
                if (matchingResult.Results.HasValue)
                {
                    availableOrder.NotMatchServiceId = matchingResult.Results;
                }
                await _smsDataContext.SaveChangesAsync();
                return new ApiResponseBaseModel<RentCodeOrder>() { Success = false, Message = "MessageDidNotMatchWithService" };
            }

            var max = availableOrder.MaximunSms ?? 1;
            var remaining = availableOrder.RemainingSms ?? 1;

            var price = PrepareServicePriceForOrder(availableOrder);

            if (max != 1 && max > remaining)
            {
                if (!await ChargeForExtendingSms(availableOrder, price, false))
                {
                    return new ApiResponseBaseModel<RentCodeOrder>() { Success = false, Message = "NotEnoughtCredit" };
                }
            }

            var orderResult = new OrderResult()
            {
                OrderId = availableOrder.Id,
                Message = message,
                Sender = sender,
                PhoneNumber = phoneNumber,
                SmsHistoryId = smsHistoryId
            };
            if (availableOrder.ServiceProvider != null && availableOrder.ServiceProvider.ServiceType != ServiceType.ByTime)
            {
                orderResult.Cost = price;
                availableOrder.RemainingSms = availableOrder.RemainingSms ?? 1;
                availableOrder.RemainingSms--;
                if (availableOrder.RemainingSms == 0)
                {
                    availableOrder.Status = OrderStatus.Success;
                    availableOrder.PendingReferalCalculate = true;
                    var gsm512IdLists = await _cacheService.Gsm512IdList();
                    if (availableOrder.ConnectedGsmId.HasValue && gsm512IdLists.Contains(availableOrder.ConnectedGsmId.Value))
                    {
                        await StopComsRangeIdleAsync(availableOrder.ConnectedGsmId.Value, availableOrder.ProposedGsm512RangeName);
                    }
                    await ReportGsmValue(availableOrder);
                }
            }
            _smsDataContext.OrderResults.Add(orderResult);
            _cacheService.ResetServiceProviderContinuosFailedCount(availableOrder.ServiceProviderId);
            await _cacheService.ResetUserContinuosFailedCount(availableOrder.UserId);
            remaining--;
            await _smsDataContext.SaveChangesAsync();
            if (availableOrder.ServiceProvider.ServiceType == ServiceType.Any || availableOrder.ServiceProvider.ServiceType == ServiceType.Basic)
            {
                var count = await CountSuccessOrderByPhoneNumberAndServiceProvider(availableOrder.ServiceProviderId, availableOrder.PhoneNumber);
                if (count + 1 >= availableOrder.ServiceProvider.ReceivingThreshold)
                {
                    await _cacheService.RemoveAnAvailablePhoneNumberForServiceProvider(availableOrder.ServiceProviderId, availableOrder.PhoneNumber);
                }
                var com = await _smsDataContext.Coms.FirstOrDefaultAsync(r => r.PhoneNumber == availableOrder.PhoneNumber);
                if (com != null)
                {
                    com.PhoneEfficiency = null;
                    await _smsDataContext.SaveChangesAsync();
                }
            }
            try
            {
                if (availableOrder.ServiceProvider.ServiceType != ServiceType.ByTime)
                {
                    if (availableOrder.Status == OrderStatus.Success || remaining == max - 1)
                    {
                        var discount = await _smsDataContext.Discounts.FirstOrDefaultAsync(r => r.Month == availableOrder.Created.Value.Month - 1
                        && r.Year == availableOrder.Created.Value.Year && r.GsmDeviceId == availableOrder.ConnectedGsmId && r.ServiceProviderId == availableOrder.ServiceProviderId);

                        var profit = price * (decimal)discount.Percent / 100;
                        availableOrder.GsmDeviceProfit += profit;
                        var gsmOwnerIds = await _smsDataContext.UserGsmDevices.Where(r => r.GsmDeviceId == availableOrder.ConnectedGsmId).Select(r => r.UserId).ToListAsync();
                        var gsmOwnersCount = gsmOwnerIds.Count;
                        if (gsmOwnersCount > 0)
                        {
                            var eachProfit = profit / gsmOwnersCount;
                            var transactions = gsmOwnerIds.Select(r => new UserTransaction()
                            {
                                Amount = eachProfit,
                                Comment = $"Chiec Khau GSM<{availableOrder.ConnectedGsmId}>",
                                IsImport = true,
                                UserId = r,
                                OrderId = availableOrder.Id,
                                UserTransactionType = UserTransactionType.AgentDiscount
                            }).ToList();
                            _smsDataContext.UserTransactions.AddRange(transactions);
                        }
                        await _smsDataContext.SaveChangesAsync();
                        if (gsmOwnersCount > 0)
                        {
                            var eachProfit = profit / gsmOwnersCount;
                            foreach (var ownerId in gsmOwnerIds)
                            {
                                await UpdateUserBallance(ownerId, eachProfit);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Profit failed 1 - ");
            }
            return new ApiResponseBaseModel<RentCodeOrder>()
            {
                Success = true,
                Results = availableOrder,
                Message = availableOrder.ServiceProvider.Name
            };
        }

        public async Task<ApiResponseBaseModel<RentCodeOrder>> AssignAudioResult(string phoneNumber, string audioUrl, string sender, int smsHistoryId)
        {
            var now = _dateTimeService.UtcNow();
            var availableOrder = await _smsDataContext.RentCodeOrders.Include(r => r.ServiceProvider)
                .FirstOrDefaultAsync(r => r.Status == OrderStatus.Waiting & r.Expired > now && r.PhoneNumber == phoneNumber && r.AllowVoiceSms && r.VoiceSmsPrice != null);
            if (availableOrder == null) return new ApiResponseBaseModel<RentCodeOrder>() { Success = false, Message = "NoAvailableOrder" };
            if (availableOrder.ServiceProvider == null) return new ApiResponseBaseModel<RentCodeOrder>() { Success = false, Message = "AvailableOrderDoesNotHaveService" };

            var max = availableOrder.MaximunSms ?? 1;
            var remaining = availableOrder.RemainingSms ?? 1;

            var price = availableOrder.VoiceSmsPrice.GetValueOrDefault();

            if (max != 1 && max > remaining)
            {
                if (!await ChargeForExtendingSms(availableOrder, price, true))
                {
                    return new ApiResponseBaseModel<RentCodeOrder>() { Success = false, Message = "NotEnoughtCredit" };
                }
            }

            if (max == 1 || max == remaining)
            {
                if (!await ChargeForFirstAudioMessage(availableOrder, price))
                {
                    return new ApiResponseBaseModel<RentCodeOrder>() { Success = false, Message = "NotEnoughtCredit" };
                }
            }

            var orderResult = new OrderResult()
            {
                OrderId = availableOrder.Id,
                Message = "Audio",
                AudioUrl = audioUrl,
                Sender = sender,
                PhoneNumber = phoneNumber,
                SmsHistoryId = smsHistoryId,
                SmsType = SmsType.Audio
            };
            if (availableOrder.ServiceProvider != null && availableOrder.ServiceProvider.ServiceType != ServiceType.ByTime)
            {
                orderResult.Cost = price;
                availableOrder.RemainingSms = availableOrder.RemainingSms ?? 1;
                availableOrder.RemainingSms--;
                if (availableOrder.RemainingSms == 0)
                {
                    availableOrder.Status = OrderStatus.Success;
                    availableOrder.PendingReferalCalculate = true;
                    var gsm512IdLists = await _cacheService.Gsm512IdList();
                    if (availableOrder.ConnectedGsmId.HasValue && gsm512IdLists.Contains(availableOrder.ConnectedGsmId.Value))
                    {
                        await StopComsRangeIdleAsync(availableOrder.ConnectedGsmId.Value, availableOrder.ProposedGsm512RangeName);
                    }
                    await ReportGsmValue(availableOrder);
                }
            }
            _smsDataContext.OrderResults.Add(orderResult);
            _cacheService.ResetServiceProviderContinuosFailedCount(availableOrder.ServiceProviderId);
            await _cacheService.ResetUserContinuosFailedCount(availableOrder.UserId);
            remaining--;
            await _smsDataContext.SaveChangesAsync();
            if (availableOrder.ServiceProvider.ServiceType == ServiceType.Any || availableOrder.ServiceProvider.ServiceType == ServiceType.Basic)
            {
                var count = await CountSuccessOrderByPhoneNumberAndServiceProvider(availableOrder.ServiceProviderId, availableOrder.PhoneNumber);
                if (count + 1 >= availableOrder.ServiceProvider.ReceivingThreshold)
                {
                    await _cacheService.RemoveAnAvailablePhoneNumberForServiceProvider(availableOrder.ServiceProviderId, availableOrder.PhoneNumber);
                }
                var com = await _smsDataContext.Coms.FirstOrDefaultAsync(r => r.PhoneNumber == availableOrder.PhoneNumber);
                if (com != null)
                {
                    com.PhoneEfficiency = null;
                    await _smsDataContext.SaveChangesAsync();
                }
            }
            try
            {
                if (availableOrder.ServiceProvider.ServiceType != ServiceType.ByTime)
                {
                    if (availableOrder.Status == OrderStatus.Success || remaining == max - 1)
                    {
                        var discount = await _smsDataContext.Discounts.FirstOrDefaultAsync(r => r.Month == availableOrder.Created.Value.Month - 1
                        && r.Year == availableOrder.Created.Value.Year && r.GsmDeviceId == availableOrder.ConnectedGsmId && r.ServiceProviderId == availableOrder.ServiceProviderId);

                        var profit = price * (decimal)discount.Percent / 100;
                        availableOrder.GsmDeviceProfit += profit;
                        var gsmOwnerIds = await _smsDataContext.UserGsmDevices.Where(r => r.GsmDeviceId == availableOrder.ConnectedGsmId).Select(r => r.UserId).ToListAsync();
                        var gsmOwnersCount = gsmOwnerIds.Count;
                        if (gsmOwnersCount > 0)
                        {
                            var eachProfit = profit / gsmOwnersCount;
                            var transactions = gsmOwnerIds.Select(r => new UserTransaction()
                            {
                                Amount = eachProfit,
                                Comment = $"Chiec Khau GSM<{availableOrder.ConnectedGsmId}> Voice",
                                IsImport = true,
                                UserId = r,
                                OrderId = availableOrder.Id,
                                UserTransactionType = UserTransactionType.AgentDiscount
                            }).ToList();
                            _smsDataContext.UserTransactions.AddRange(transactions);
                        }
                        await _smsDataContext.SaveChangesAsync();
                        if (gsmOwnersCount > 0)
                        {
                            var eachProfit = profit / gsmOwnersCount;
                            foreach (var ownerId in gsmOwnerIds)
                            {
                                await UpdateUserBallance(ownerId, eachProfit);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Profit failed 1 - ");
            }
            return new ApiResponseBaseModel<RentCodeOrder>()
            {
                Success = true,
                Results = availableOrder,
                Message = availableOrder.ServiceProvider.Name
            };
        }
        private async Task<int> CountSuccessOrderByPhoneNumberAndServiceProvider(int serviceProvider, string phoneNumber)
        {
            return await _smsDataContext.RentCodeOrders
                .Where(r => r.ServiceProviderId == serviceProvider && r.Status == OrderStatus.Success && r.PhoneNumber == phoneNumber)
                .Select(r => 1)
                .CountAsync();
        }

        private decimal PrepareServicePriceForOrder(RentCodeOrder availableOrder)
        {
            var max = availableOrder.MaximunSms ?? 1;
            var remaining = availableOrder.RemainingSms ?? 1;
            decimal? price = 0;
            switch (max - remaining - 1)
            {
                case 2:
                    price = availableOrder.ServiceProvider.Price2;
                    break;
                case 3:
                    price = availableOrder.ServiceProvider.Price3;
                    break;
                case 4:
                    price = availableOrder.ServiceProvider.Price4;
                    break;
                case 5:
                    price = availableOrder.ServiceProvider.Price5;
                    break;
                default:
                    price = availableOrder.Price;
                    break;
            }
            return price.HasValue ? price.Value : availableOrder.Price;
        }

        private async Task<bool> ChargeForExtendingSms(RentCodeOrder availableOrder, decimal servicePrice, bool isAudio)
        {
            var user = await _userService.Get(availableOrder.UserId);
            if (user == null) return false;
            if (user.Ballance < servicePrice)
            {
                _logger.LogInformation("Assign result: charge failed! {0} - {1}", user.Ballance, servicePrice);
                return false;
            }
            var transaction = new UserTransaction()
            {
                Amount = servicePrice,
                Comment = $"Paid for{(isAudio ? " Voice" : "")} <{availableOrder.ServiceProvider.Name}>",
                IsImport = false,
                UserId = user.Id,
                OrderId = availableOrder.Id,
                UserTransactionType = UserTransactionType.PaidForService
            };
            _smsDataContext.UserTransactions.Add(transaction);
            await _smsDataContext.SaveChangesAsync();
            await UpdateUserBallance(user.Id, -1 * servicePrice);
            return true;
        }

        private async Task<bool> ChargeForFirstAudioMessage(RentCodeOrder availableOrder, decimal audioPrice)
        {
            var user = await _userService.Get(availableOrder.UserId);
            if (user == null) return false;
            var extendingPrice = audioPrice - availableOrder.Price;
            if (user.Ballance < extendingPrice)
            {
                _logger.LogInformation("Assign result: charge failed! {0} - {1}", user.Ballance, extendingPrice);
                return false;
            }
            var transaction = new UserTransaction()
            {
                Amount = extendingPrice,
                Comment = $"Paid for Voice <{availableOrder.ServiceProvider.Name}>",
                IsImport = false,
                UserId = user.Id,
                OrderId = availableOrder.Id,
                UserTransactionType = UserTransactionType.PaidForService
            };
            _smsDataContext.UserTransactions.Add(transaction);
            await _smsDataContext.SaveChangesAsync();
            await UpdateUserBallance(user.Id, -1 * extendingPrice);
            return true;
        }
        public async Task<ApiResponseBaseModel<int?>> CheckMessageIsMatchWithService(ServiceProvider serviceProvider, string message, string sender)
        {
            if (serviceProvider.ServiceType == ServiceType.ByTime || serviceProvider.ServiceType == ServiceType.Callback)
            {
                var all = await _serviceProviderService.GetAllAvailableServices(null);
                var noService = all.FirstOrDefault(r => r.Name == "No");
                if (noService != null)
                {
                    var matchWithNo = MessageMatchingHelpers.CheckMessageIsMatchWithServiceProviderPattern(noService.MessageRegex, message, sender);
                    if (matchWithNo)
                    {
                        return new ApiResponseBaseModel<int?>() { Success = false, Message = "MatchWithNo" };
                    }
                }
                return new ApiResponseBaseModel<int?>()
                {
                    Success = true,
                    Message = "ByTimeOrCallBack"
                };
            }
            var isMatched = Helpers.MessageMatchingHelpers.CheckMessageIsMatchWithServiceProviderPattern(serviceProvider.MessageRegex, message, sender);
            if (isMatched)
            {
                return new ApiResponseBaseModel<int?>()
                {
                    Success = true,
                    Message = "BasicMatched"
                };
            }
            if (serviceProvider.ServiceType == ServiceType.Basic)
            {
                return new ApiResponseBaseModel<int?>()
                {
                    Success = false,
                    Message = "BasicNotMatched",
                    Results = await FindBasicMatchedService(message, sender) ?? (int?)-1
                };
            }
            var allBasicServices = await _smsDataContext.ServiceProviders.Where(r => r.ServiceType == ServiceType.Basic && r.Disabled != true).ToListAsync();
            var basicServiceThatMatched = allBasicServices.FirstOrDefault(r => MessageMatchingHelpers.CheckMessageIsMatchWithServiceProviderPattern(r.MessageRegex, message, sender));
            if (basicServiceThatMatched != null)
            {
                return new ApiResponseBaseModel<int?>()
                {
                    Success = false,
                    Message = $"AnyNotMatch({basicServiceThatMatched.Name})",
                    Results = await FindBasicMatchedService(message, sender)
                };
            }
            var allAnyServicesHasKeywork = await _smsDataContext
                .ServiceProviders
                .Where(r => r.ServiceType == ServiceType.Any && r.Id != serviceProvider.Id && r.Disabled != true && !string.IsNullOrEmpty(r.MessageRegex))
                .ToListAsync();
            var anyServiceThatMatched = allAnyServicesHasKeywork.FirstOrDefault(r => MessageMatchingHelpers.CheckMessageIsMatchWithServiceProviderPattern(r.MessageRegex, message, sender));
            if (anyServiceThatMatched != null)
            {
                return new ApiResponseBaseModel<int?>()
                {
                    Success = false,
                    Message = $"AnyNotMatch({anyServiceThatMatched.Name})",
                    Results = anyServiceThatMatched.Id
                };
            }
            return new ApiResponseBaseModel<int?>()
            {
                Success = true,
                Message = "AnyMatched"
            };
        }

        private async Task<int?> FindBasicMatchedService(string message, string sender)
        {
            var allBasicServices = await _smsDataContext.ServiceProviders.Where(r => r.ServiceType == ServiceType.Basic && r.Disabled != true).ToListAsync();
            var basicServiceThatMatched = allBasicServices.FirstOrDefault(r => MessageMatchingHelpers.CheckMessageIsMatchWithServiceProviderPattern(r.MessageRegex, message, sender));
            if (basicServiceThatMatched != null && basicServiceThatMatched.Name != "No")
            {
                return basicServiceThatMatched.Id;
            }
            return null;
        }

        public async Task<bool> CheckOrderIsAvailableForUser(int orderId, int? userId)
        {
            userId = userId.HasValue ? userId.Value : _authService.CurrentUserId().GetValueOrDefault();
            if (userId == 0) return false;
            return await _smsDataContext.RentCodeOrders.AnyAsync(r => r.Id == orderId && r.UserId == userId);
        }

        public override async Task<FilterResponse<RentCodeOrder>> Paging(FilterRequest filterRequest)
        {
            var pagingResult = await base.Paging(filterRequest);
            bool ignoreComplaint = false;
            if (filterRequest != null && filterRequest.SearchObject != null)
            {
                if (filterRequest.SearchObject.ContainsKey("IgnoreComplaint"))
                {
                    ignoreComplaint = true;
                }
            }
            if (pagingResult.Results != null && pagingResult.Results.Count > 0 && !ignoreComplaint)
            {
                var ids = pagingResult.Results.Select(r => r.Id).ToList();
                var resultCounts = await (from o in _smsDataContext.OrderResults
                                          where ids.Contains(o.OrderId)
                                          group o by o.OrderId into gr
                                          select new { OrderId = gr.Key, Count = gr.Count() }).ToListAsync();
                var complains = await (from c in _smsDataContext.OrderComplaints
                                       where ids.Contains(c.OrderId)
                                       select c).ToListAsync();
                foreach (var item in pagingResult.Results)
                {
                    item.ResultsCount = resultCounts.Where(r => r.OrderId == item.Id).Select(c => c.Count).FirstOrDefault();
                    item.AlreadyComplain = complains.Any(r => r.OrderId == item.Id);
                }
            }
            return pagingResult;
        }

        public async Task<ApiResponseBaseModel<RentCodeOrder>> RequestThirdOrder(int userId, int duration, HoldSimDurationUnit unit, int? networkProvider, AppSourceType appSourceType)
        {
            if (duration <= 0) return new ApiResponseBaseModel<RentCodeOrder>()
            {
                Success = false,
                Message = "BadRequest"
            };
            var user = await _userService.Get(userId);
            if (user == null) return new ApiResponseBaseModel<RentCodeOrder>
            {
                Success = false,
                Message = "UserNotFound"
            };
            var serviceProvider = await _serviceProviderService.GetHoldingService();
            if (serviceProvider == null)
            {
                return new ApiResponseBaseModel<RentCodeOrder>()
                {
                    Success = false,
                    Message = "HoldingServiceNotFound"
                };
            }
            if (serviceProvider.Disabled)
            {
                return new ApiResponseBaseModel<RentCodeOrder>()
                {
                    Success = false,
                    Message = "HoldingServiceIsDisabled"
                };
            }
            var price = (unit == HoldSimDurationUnit.ByDay ? serviceProvider.Price : serviceProvider.AdditionalPrice.GetValueOrDefault()) * duration;
            if (user.Ballance < price)
            {
                return new ApiResponseBaseModel<RentCodeOrder>()
                {
                    Success = false,
                    Message = "NotEnoughCredit"
                };
            }
            var systemConfiguration = await _systemConfigurationService.GetSystemConfiguration();
            if (systemConfiguration != null && systemConfiguration.MaximumAvailableOrder != 0)
            {
                var userOrderCount = await _smsDataContext.RentCodeOrders.CountAsync(r => r.UserId == userId && (r.Status == OrderStatus.Floating || r.Status == OrderStatus.Waiting));
                if (userOrderCount >= systemConfiguration.MaximumAvailableOrder)
                {
                    return new ApiResponseBaseModel<RentCodeOrder>()
                    {
                        Success = false,
                        Message = "MaximumOrder"
                    };
                }
            }
            if (networkProvider == 0) networkProvider = null;
            var order = new RentCodeOrder()
            {
                Price = price,
                ServiceProviderId = serviceProvider.Id,
                Status = OrderStatus.Floating,
                UserId = user.Id,
                LockTime = duration * (unit == HoldSimDurationUnit.ByDay ? 24 : 1) * 60,
                NetworkProvider = networkProvider,
                AppSourceType = appSourceType
            };
            await Create(order);
            return new ApiResponseBaseModel<RentCodeOrder>()
            {
                Success = true,
                Results = order
            };
        }

        public async Task<ApiResponseBaseModel<RentCodeOrder>> RequestOrderCallback(int userId, string requestPhoneNumber, AppSourceType appSourceType)
        {
            var user = await _userService.Get(userId);
            if (user == null) return new ApiResponseBaseModel<RentCodeOrder>
            {
                Success = false,
                Message = "UserNotFound"
            };
            var listPhoneNumbers = await GetAllUserHistoryPhoneNumbers();
            if (!listPhoneNumbers.Results.Any(r => r == requestPhoneNumber))
            {
                return new ApiResponseBaseModel<RentCodeOrder>()
                {
                    Success = false,
                    Message = "NotYoursOrRemoved"
                };
            }
            var serviceProvider = await _serviceProviderService.GetCallbackService();
            if (serviceProvider == null)
            {
                return new ApiResponseBaseModel<RentCodeOrder>()
                {
                    Success = false,
                    Message = "CallbackServiceNotFound"
                };
            }
            if (serviceProvider.Disabled)
            {
                return new ApiResponseBaseModel<RentCodeOrder>()
                {
                    Success = false,
                    Message = "CallbackServiceIsDisabled"
                };
            }
            var price = serviceProvider.Price;
            if (user.Ballance < price)
            {
                return new ApiResponseBaseModel<RentCodeOrder>()
                {
                    Success = false,
                    Message = "NotEnoughCredit"
                };
            }
            var systemConfiguration = await _systemConfigurationService.GetSystemConfiguration();
            if (systemConfiguration != null && systemConfiguration.MaximumAvailableOrder != 0)
            {
                var userOrderCount = await _smsDataContext.RentCodeOrders.CountAsync(r => r.UserId == userId && (r.Status == OrderStatus.Floating || r.Status == OrderStatus.Waiting));
                if (userOrderCount >= systemConfiguration.MaximumAvailableOrder)
                {
                    return new ApiResponseBaseModel<RentCodeOrder>()
                    {
                        Success = false,
                        Message = "MaximumOrder"
                    };
                }
            }
            var order = new RentCodeOrder()
            {
                Price = price,
                ServiceProviderId = serviceProvider.Id,
                Status = OrderStatus.Floating,
                UserId = user.Id,
                LockTime = serviceProvider.LockTime,
                NetworkProvider = null,
                RequestPhoneNumber = requestPhoneNumber,
                AppSourceType = appSourceType
            };
            await Create(order);
            return new ApiResponseBaseModel<RentCodeOrder>()
            {
                Success = true,
                Results = order
            };
        }

        public async Task<ApiResponseBaseModel<List<string>>> GetAllUserHistoryPhoneNumbers()
        {
            var userId = _authService.CurrentUserId();
            if (userId == null)
            {
                return new ApiResponseBaseModel<List<string>>()
                {
                    Message = "Unauthorized",
                    Success = false
                };
            }
            var allComs = await (from com in _smsDataContext.Coms.Include(r => r.GsmDevice)
                                 join order in _smsDataContext.RentCodeOrders on com.PhoneNumber equals order.PhoneNumber
                                 where com.Disabled != true && !string.IsNullOrEmpty(com.PhoneNumber) && com.GsmDevice.Disabled != true
                                 && com.GsmDevice.IsInMaintenance != true && order.UserId == userId && order.Status == OrderStatus.Success
                                 select com.PhoneNumber).AsNoTracking().ToListAsync();

            return new ApiResponseBaseModel<List<string>>()
            {
                Success = true,
                Results = allComs.Distinct().ToList()
            };
        }

        public async Task ArchiveOldOrders(ILogger logger)
        {
            var systemConfiguration = await _systemConfigurationService.GetSystemConfiguration();
            if (systemConfiguration == null || !systemConfiguration.AllowArchiveOrder)
            {
                logger.LogWarning("Not allow archive order!!!");
                return;
            }
            var mileStone = _dateTimeService.UtcNow().AddMonths(-3);
            var query = _smsDataContext.RentCodeOrders
                .Where(r => r.Status != OrderStatus.Success && r.Created < mileStone)
                .OrderBy(r => r.Id);
            var maxCount = 1000;
            var orders = await query.Take(maxCount).ToListAsync();
            logger.LogInformation("Delete expired order: {0}", orders.Count);
            _smsDataContext.RentCodeOrders.RemoveRange(orders);
            await _smsDataContext.SaveChangesAsync();
        }
    }
}
