using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
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
    public interface IInternationalSimOrderService : IServiceBase<InternationalSimOrder>
    {
        Task<ApiResponseBaseModel<InternationalSimOrder>> RequestAOrder(int userId, AppSourceType appSourceType, RequestInternaltionSimOrderRequest request);
        Task BackgroundFindPhoneNumber();
        Task BackgroundConfirmPhoneNumber();
        Task BackgroundExpiredOrder();
        Task BackgroundProposedOrder();
        Task<ApiResponseBaseModel> CloseOrder(int id);
        Task<bool> CheckOrderIsAvailableForUser(int orderId);
    }
    public class InternationalSimOrderService : ServiceBase<InternationalSimOrder>, IInternationalSimOrderService
    {
        private readonly IUserService _userService;
        private readonly ISimCountryService _simCountryService;
        private readonly ISystemConfigurationService _systemConfigurationService;
        private readonly ILogger _logger;
        private readonly IDateTimeService _dateTimeService;
        private readonly IPreloadOrderSimCountryQueue _preloadOrderSimCountryQueue;
        private readonly IMemoryCache _memoryCache;
        private readonly TimeSpan PreloadCacheTimeOut = TimeSpan.FromSeconds(60 * 60 * 1000);
        public InternationalSimOrderService(SmsDataContext smsDataContext,
          ISimCountryService simCountryService,
          ISystemConfigurationService systemConfigurationService,
          ILogger<InternationalSimOrderService> logger,
          IDateTimeService dateTimeService,
          IPreloadOrderSimCountryQueue preloadOrderSimCountryQueue,
          IMemoryCache memoryCache,
          IUserService userService) : base(smsDataContext)
        {
            _userService = userService;
            _simCountryService = simCountryService;
            _systemConfigurationService = systemConfigurationService;
            _logger = logger;
            _dateTimeService = dateTimeService;
            _preloadOrderSimCountryQueue = preloadOrderSimCountryQueue;
            _memoryCache = memoryCache;
        }

        public async Task BackgroundConfirmPhoneNumber()
        {
            var floatingOrders = (await _smsDataContext.InternationalSimOrders
                .Where(r => r.Status == OrderStatus.Floating)
                .ToListAsync()).OrderBy(r => r.SimCountryId).ToList();
            floatingOrders = floatingOrders.Where(r => !string.IsNullOrEmpty(r.ProposedPhoneNumber)).ToList();
            if (floatingOrders.Count == 0) return;

            var activePhoneNumbers = await _smsDataContext
                .InternationalSimOrders
                .Where(r => r.Status == OrderStatus.Waiting)
                .Select(r => r.PhoneNumber).ToListAsync();

            var userIds = floatingOrders.Select(r => r.UserId).Distinct().ToList();
            var users = await _smsDataContext.Users.Where(r => userIds.Contains(r.Id)).AsNoTracking().ToListAsync();

            var simCountryIds = floatingOrders.Select(r => r.SimCountryId).ToList();
            var simCountries = await _simCountryService.GetAllAvailableSimCountries();

            foreach (var order in floatingOrders)
            {
                var proposedPhoneNumber = order.ProposedPhoneNumber;
                var simCountry = simCountries.FirstOrDefault(r => r.Id == order.SimCountryId);
                var user = users.FirstOrDefault(r => r.Id == order.UserId);
                if (simCountry == null)
                {
                    continue;
                }
                if (activePhoneNumbers.Contains(proposedPhoneNumber))
                {
                    continue;
                }
                order.Status = OrderStatus.Waiting;
                order.PhoneNumber = proposedPhoneNumber;
                order.Expired = _dateTimeService.UtcNow().AddMinutes(order.LockTime);
                activePhoneNumbers.Add(proposedPhoneNumber);
                var transaction = new UserTransaction()
                {
                    Amount = order.Price,
                    Comment = $"Paid for SIM <{simCountry.CountryName}>",
                    IsImport = false,
                    UserId = order.UserId,
                    OrderId = order.Id,
                    UserTransactionType = UserTransactionType.PaidForInternationalSim,
                    Balance = user.Ballance
                };
                _smsDataContext.UserTransactions.Add(transaction);
                await _smsDataContext.SaveChangesAsync();
                _logger.LogInformation("Order {0}, Phone {1} -> Assigning successed", order.Id, proposedPhoneNumber);
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
                    unfinalized.ProposedPhoneNumber = null;
                    unfinalized.ConnectedForwarderId = null;
                }
                await _smsDataContext.SaveChangesAsync();
            }
        }

        public async Task BackgroundExpiredOrder()
        {
            var now = _dateTimeService.UtcNow();
            var expiredOrders = await (from o in _smsDataContext.InternationalSimOrders
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
        public async Task BackgroundProposedOrder()
        {
            var now = _dateTimeService.UtcNow();
            var expiredOrders = await (from o in _smsDataContext.InternationalSimOrders
                                       where o.NeedProposedProcessing
                                       select o).ToListAsync();
            var simCountryIds = expiredOrders.Select(r => r.SimCountryId).ToList();
            var simCountries = await _smsDataContext.SimCountries.Where(r => simCountryIds.Contains(r.Id))
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

            foreach (var order in expiredOrders)
            {
                order.NeedProposedProcessing = false;
                var simCountry = simCountries.FirstOrDefault(r => r.Id == order.SimCountryId);
                var user = await _smsDataContext.Users.FirstOrDefaultAsync(r => r.Id == order.UserId);
                _logger.LogInformation("Update order {0} with status {1}", order.Id, order.Status);
                if (order.Status == OrderStatus.Error && !string.IsNullOrEmpty(order.PhoneNumber))
                {
                    var transaction = await _smsDataContext.UserTransactions.FirstOrDefaultAsync(r => r.OrderId == order.Id);
                    if (transaction != null)
                    {
                        _smsDataContext.UserTransactions.Remove(transaction);
                    }
                }
                await _smsDataContext.SaveChangesAsync();
                if (order.Status == OrderStatus.Error && !string.IsNullOrEmpty(order.PhoneNumber))
                {
                    var sql = @"UPDATE Users set Ballance = Ballance + {0} where Id = {1}";
                    await _smsDataContext.Database.ExecuteSqlCommandAsync(
                        sql,
                        order.Price,
                        user.Id
                        );
                }
            }
            _logger.LogInformation("End ProcessOrderStatus");
        }

        public async Task BackgroundFindPhoneNumber()
        {
            _logger.LogInformation("Start find phone number");
            var floatingOrders = (await _smsDataContext.InternationalSimOrders
              .Where(r => r.Status == OrderStatus.Floating)
              .ToListAsync()).OrderBy(r => r.SimCountryId).ToList();
            floatingOrders = floatingOrders.Where(r => string.IsNullOrEmpty(r.ProposedPhoneNumber)).ToList();
            if (floatingOrders.Count == 0) return;
            _logger.LogInformation("Floating orders: {0}", string.Join(", ", floatingOrders.Select(r => r.Id).ToArray()));
            var floatingSimCountryIds = floatingOrders.Select(r => r.SimCountryId).Distinct().ToList();
            var allInternationalSims = await LoadAllInternationalSim();
            _logger.LogInformation("Preloaded allInternationalSims: {0}", allInternationalSims.Count);

            var activePhoneNumbers = await _smsDataContext
                .InternationalSimOrders
                .Where(r => r.Status == OrderStatus.Waiting)
                .Select(r => r.PhoneNumber).ToListAsync();
            _logger.LogInformation("Preloaded activePhoneNumbers: {0}", activePhoneNumbers.Count);

            var userIds = floatingOrders.Select(r => r.UserId).Distinct().ToList();
            var users = await _smsDataContext.Users.Where(r => userIds.Contains(r.Id)).AsNoTracking().ToListAsync();
            _logger.LogInformation("Preloaded users: {0}", users.Count);

            var configuration = await _systemConfigurationService.GetSystemConfiguration();
            var autoCancelOrderDuration = configuration.AutoCancelOrderDuration ?? 2;

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
                _preloadOrderSimCountryQueue.QueuePreloadSimCountry(order.SimCountryId);
                var internationalSims = allInternationalSims.Where(r => r.SimCountryId == order.SimCountryId).ToList();
                _logger.LogInformation("Available sim for country: " + internationalSims.Count);
                if (internationalSims.Count == 0)
                {
                    continue;
                }
                internationalSims = internationalSims.Where(r => !activePhoneNumbers.Contains(r.PhoneNumber)).ToList();
                _logger.LogInformation("internationalSims is not active: " + internationalSims.Count);
                if (internationalSims.Count == 0)
                {
                    continue;
                }
                var internationalSimObject = internationalSims.OrderByDescending(r => Guid.NewGuid()).FirstOrDefault();
                var phoneNumber = internationalSimObject.PhoneNumber;
                _logger.LogInformation("Available phone number {0}", phoneNumber);
                if (string.IsNullOrEmpty(phoneNumber)) continue;
                activePhoneNumbers.Add(phoneNumber);
                order.ProposedPhoneNumber = phoneNumber;
                order.ConnectedForwarderId = internationalSimObject.ForwarderId;
                await _smsDataContext.SaveChangesAsync();
            }
            _logger.LogInformation("End AssignPhoneNumberToOrder");
        }

        private async Task<List<InternationalSim>> LoadAllInternationalSim(bool isPreloading = false)
        {
            var cacheKey = "INTERNATIONAL_SIM_FOR_COUNTRY_CACHE_KEY";
            Func<Task<List<InternationalSim>>> fetchFunc = async () =>
            await (from sim in _smsDataContext.InternationalSims
                   where !sim.IsDisabled && !string.IsNullOrEmpty(sim.PhoneNumber)
                   select sim).AsNoTracking().ToListAsync();
            List<InternationalSim> values = null;
            if (isPreloading)
            {
                values = await fetchFunc();
            }
            return await _memoryCache.GetOrCreateAsync(cacheKey, async settings =>
            {
                settings.SetAbsoluteExpiration(PreloadCacheTimeOut);
                return values ?? await fetchFunc();
            });
        }

        public override void Map(InternationalSimOrder entity, InternationalSimOrder model)
        {
        }

        public async Task<ApiResponseBaseModel<InternationalSimOrder>> RequestAOrder(int userId, AppSourceType appSourceType, RequestInternaltionSimOrderRequest request)
        {
            var simCountryId = request.SimCountryId;
            var user = await _userService.Get(userId);
            if (user == null) return new ApiResponseBaseModel<InternationalSimOrder>
            {
                Success = false,
                Message = "UserNotFound"
            };
            var simCountry = await _simCountryService.Get(simCountryId);
            if (simCountry == null)
            {
                return new ApiResponseBaseModel<InternationalSimOrder>()
                {
                    Success = false,
                    Message = "SimCountryNotFound"
                };
            }
            if (simCountry.IsDisabled)
            {
                return new ApiResponseBaseModel<InternationalSimOrder>()
                {
                    Success = false,
                    Message = "SimCountryDisabled"
                };
            }
            if (user.Ballance < simCountry.Price)
            {
                return new ApiResponseBaseModel<InternationalSimOrder>()
                {
                    Success = false,
                    Message = "NotEnoughCredit"
                };
            }
            var systemConfiguration = await _systemConfigurationService.GetSystemConfiguration();
            if (systemConfiguration != null && systemConfiguration.MaximumAvailableOrder != 0)
            {
                var userOrderCount = await _smsDataContext.InternationalSimOrders
                    .CountAsync(r => r.UserId == userId && (r.Status == OrderStatus.Floating || r.Status == OrderStatus.Waiting));

                if (userOrderCount >= systemConfiguration.MaximumAvailableInternationSimOrder)
                {
                    return new ApiResponseBaseModel<InternationalSimOrder>()
                    {
                        Success = false,
                        Message = "MaximumOrder"
                    };
                }
            }
            var lockTime = simCountry.LockTime;
            var maximumSms = request.MaximumSms;
            // increase lock time by 60% if maximumSms is more than 1
            // ex: if lock time is 5 minutes, it will be 8 if user request more than 1 SMS
            lockTime += (int)Math.Ceiling((maximumSms ?? 1) > 1 ? 0.6 * lockTime : 0);

            var order = new InternationalSimOrder()
            {
                Price = simCountry.Price,
                SimCountryId = simCountry.Id,
                Status = OrderStatus.Floating,
                UserId = user.Id,
                LockTime = lockTime,
                MaximunSms = maximumSms,
                RemainingSms = maximumSms,
                AppSourceType = appSourceType
            };
            await Create(order);
            return new ApiResponseBaseModel<InternationalSimOrder>()
            {
                Success = true,
                Results = order
            };
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

        public async Task<bool> CheckOrderIsAvailableForUser(int orderId)
        {
            var userId = (await _userService.GetCurrentUser()).Id;
            if (userId == 0) return false;
            return await _smsDataContext.InternationalSimOrders.AnyAsync(r => r.Id == orderId && r.UserId == userId);
        }

        protected override IQueryable<InternationalSimOrder> GenerateQuery(FilterRequest filterRequest = null)
        {
            var query = base.GenerateQuery(filterRequest);
            query = query.Include("User.UserProfile").Include(r => r.OrderResults);
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
                            ignoreDateTime = true;
                            username = username.ToLower();
                            query = query.Where(r => r.User != null && r.User.Username.Contains(username));
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
                    if (filterRequest.SearchObject.TryGetValue("simCountryIds", out object obj))
                    {
                        var ts = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<int>>();
                        if (ts.Count > 0)
                        {
                            query = query.Where(r => ts.Contains(r.SimCountryId));
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("phoneNumber", out object obj))
                    {
                        var phoneNumber = obj.ToString();
                        if (!string.IsNullOrWhiteSpace(phoneNumber))
                        {
                            ignoreDateTime = true;
                            phoneNumber = phoneNumber.ToLower();
                            query = query.Where(r => r.PhoneNumber.Contains(phoneNumber));
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("ForwarderIds", out object obj))
                    {
                        var forwarderIds = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<int?>>();

                        if (forwarderIds != null && forwarderIds.Count > 0)
                        {
                            query = query.Where(r => forwarderIds.Contains(r.ConnectedForwarderId));
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
    }
}
