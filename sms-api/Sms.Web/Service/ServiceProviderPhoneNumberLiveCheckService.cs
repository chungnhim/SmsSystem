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
    public interface IServiceProviderPhoneNumberLiveCheckService : IServiceBase<ServiceProviderPhoneNumberLiveCheck>
    {
        Task BackgroundEnqueueCheckingJob();
        Task<ApiResponseBaseModel<List<ServiceProviderPhoneNumberLiveCheck>>> GetPendingCheckingTasks(string serviceName);
        Task<ApiResponseBaseModel> ConfirmLiveCheck(string liveCheckId, bool success);
    }
    public class ServiceProviderPhoneNumberLiveCheckService : ServiceBase<ServiceProviderPhoneNumberLiveCheck>, IServiceProviderPhoneNumberLiveCheckService
    {
        private readonly ICacheService _cacheService;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger _logger;
        private readonly IAuthService _authService;
        private readonly IDateTimeService _dateTimeService;
        public ServiceProviderPhoneNumberLiveCheckService(SmsDataContext smsDataContext,
            ICacheService cacheService,
            IMemoryCache memoryCache,
            IDateTimeService dateTimeService,
            IAuthService authService,
            ILogger<ServiceProviderPhoneNumberLiveCheckService> logger) : base(smsDataContext)
        {
            _cacheService = cacheService;
            _memoryCache = memoryCache;
            _logger = logger;
            _dateTimeService = dateTimeService;
            _authService = authService;
        }

        public async Task BackgroundEnqueueCheckingJob()
        {
            var allServices = await _cacheService.GetAllServiceProviders();
            var needLiveCheckServices = allServices.Where(r => r.NeedLiveCheckBeforeUse).ToList();
            if (needLiveCheckServices.Count == 0) return;
            _logger.LogInformation("Start enqueue with services: {[0}]", string.Join(", ", needLiveCheckServices.Select(r => r.Name)));
            _memoryCache.TryGetValue<List<Com>>("COMS_FOR_SERVICE_CACHE_KEY", out var allComs);
            if (allComs == null || allComs.Count == 0) return;
            _logger.LogInformation("Coms count: {0}", allComs.Count);
            foreach (var service in needLiveCheckServices)
            {
                try
                {
                    var availableLiveChecks = await _smsDataContext.ServiceProviderPhoneNumberLiveChecks.Where(r => r.ServiceProviderId == service.Id).ToListAsync();
                    var addingList = new List<ServiceProviderPhoneNumberLiveCheck>();
                    var phoneAndGsms = allComs.Select(r => new { PhoneNumber = r.PhoneNumber, GsmDeviceId = r.GsmDeviceId }).Distinct().ToList();
                    foreach (var phoneAndGsm in phoneAndGsms)
                    {
                        if (!availableLiveChecks.Any(r => r.PhoneNumber == phoneAndGsm.PhoneNumber))
                        {
                            addingList.Add(new ServiceProviderPhoneNumberLiveCheck()
                            {
                                ServiceProviderId = service.Id,
                                PhoneNumber = phoneAndGsm.PhoneNumber,
                                GsmDeviceId = phoneAndGsm.GsmDeviceId,
                            });
                        }
                    }
                    var notExistingPhoneNumbers = availableLiveChecks.Where(r => r.LiveCheckStatus == LiveCheckStatus.None && !allComs.Any(k => k.PhoneNumber == r.PhoneNumber)).ToList();

                    _logger.LogInformation("Add count: {0} Coms: {1}", service.Name, addingList.Count);
                    const int pageSize = 50;
                    int pageCount = 0;
                    foreach (var item in addingList)
                    {
                        pageCount++;
                        _smsDataContext.ServiceProviderPhoneNumberLiveChecks.Add(item);
                        if (pageCount % pageSize == 0 || pageCount == addingList.Count)
                        {
                            await _smsDataContext.SaveChangesAsync();
                            _logger.LogInformation("Add successed: {0}, chunk: {1}", service.Name, pageCount);
                        }
                    }
                    foreach (var phoneNumber in notExistingPhoneNumbers)
                    {
                        _smsDataContext.ServiceProviderPhoneNumberLiveChecks.Remove(phoneNumber);
                    }
                    await _smsDataContext.SaveChangesAsync();
                    _logger.LogInformation("Remove successed: {0} - {1}", service.Name, notExistingPhoneNumbers.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed - {0}", service.Name);
                }
            }
        }

        public async Task<ApiResponseBaseModel> ConfirmLiveCheck(string liveCheckId, bool success)
        {
            var liveCheck = await _smsDataContext.ServiceProviderPhoneNumberLiveChecks.FirstOrDefaultAsync(r => r.Guid == liveCheckId);
            if (liveCheck == null) return ApiResponseBaseModel.UnAuthorizedResponse();
            var currentToolId = _authService.CurrentUserId();
            if (liveCheck.CheckBy != currentToolId) return ApiResponseBaseModel.UnAuthorizedResponse();
            liveCheck.LiveCheckStatus = success ? LiveCheckStatus.Ok : LiveCheckStatus.Failed;
            liveCheck.ReturnedAt = _dateTimeService.UtcNow();
            await _smsDataContext.SaveChangesAsync();

            return new ApiResponseBaseModel() { Success = true };
        }

        public async Task<ApiResponseBaseModel<List<ServiceProviderPhoneNumberLiveCheck>>> GetPendingCheckingTasks(string serviceName)
        {
            var currentToolId = _authService.CurrentUserId();
            var allServices = await _cacheService.GetAllServiceProviders();
            var pendingQuery = _smsDataContext.ServiceProviderPhoneNumberLiveChecks
                .Include(x => x.ServiceProvider)
                .Where(r => r.LiveCheckStatus == LiveCheckStatus.None);
            if (!string.IsNullOrEmpty(serviceName))
            {
                var service = allServices.FirstOrDefault(r => serviceName.Equals(r.Name, StringComparison.OrdinalIgnoreCase));
                if (service != null)
                {
                    pendingQuery = pendingQuery.Where(r => r.ServiceProviderId == service.Id);
                }
            }
            var topPending = await pendingQuery
                .OrderBy(r => r.Created).Take(50).ToListAsync();
            foreach (var pending in topPending)
            {
                pending.CheckBy = currentToolId;
                pending.LiveCheckStatus = LiveCheckStatus.Checking;
                pending.PostedAt = _dateTimeService.UtcNow();
            }
            await _smsDataContext.SaveChangesAsync();
            return new ApiResponseBaseModel<List<ServiceProviderPhoneNumberLiveCheck>>()
            {
                Results = topPending
            };
        }

        public override void Map(ServiceProviderPhoneNumberLiveCheck entity, ServiceProviderPhoneNumberLiveCheck model)
        {
            entity.LiveCheckStatus = model.LiveCheckStatus;
            entity.PostedAt = model.PostedAt;
            entity.ReturnedAt = model.ReturnedAt;
        }
        protected override IQueryable<ServiceProviderPhoneNumberLiveCheck> GenerateQuery(FilterRequest filterRequest = null)
        {
            var query = base.GenerateQuery(filterRequest);
            query = query.Include(r => r.ServiceProvider).Include(r => r.GsmDevice);

            if (filterRequest != null)
            {
                var sortColumn = filterRequest.SortColumnName ?? string.Empty;
                var isAsc = filterRequest.IsAsc;
                {
                    if (filterRequest.SearchObject.TryGetValue("staffId", out object obj) == true)
                    {
                        var staffId = (int)obj;
                        query = query.Where(r => r.GsmDevice.UserGsmDevices.Any(k => k.UserId == staffId));
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("serviceProviderIds", out object obj) == true)
                    {
                        var serviceProviderIds = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<int>>();
                        if (serviceProviderIds.Any())
                        {
                            query = from c in query
                                    join s in _smsDataContext.ProviderHistories on c.PhoneNumber equals s.PhoneNumber
                                    where s.ServiceProviderId.HasValue && serviceProviderIds.Contains(s.ServiceProviderId.Value)
                                    group c by c.Id into gr
                                    select gr.FirstOrDefault();
                            query = query.Include(r => r.ServiceProvider).Include(r => r.GsmDevice);
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("phoneNumber", out object obj) == true)
                    {
                        var phoneNumber = (string)obj;
                        if (!string.IsNullOrEmpty(phoneNumber))
                        {
                            query = query.Where(x => x.PhoneNumber.Contains(phoneNumber));
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("gsmDevice", out object obj) == true)
                    {
                        var gsmDeviceIds = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<int>>();
                        if (gsmDeviceIds.Count > 0)
                        {
                            query = query.Where(r => gsmDeviceIds.Contains(r.GsmDevice.Id));
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("liveCheckStatus", out object obj) == true)
                    {
                        var liveCheckStatus = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<LiveCheckStatus>>();
                        if (liveCheckStatus.Count > 0)
                        {
                            query = query.Where(x => liveCheckStatus.Contains(x.LiveCheckStatus));
                        }
                    }
                }
                switch (sortColumn)
                {
                    case "serviceProviderId":
                        query = isAsc ? query.OrderBy(x => x.ServiceProviderId) : query.OrderByDescending(x => x.ServiceProviderId);
                        break;
                    case "phoneNumber":
                        query = isAsc ? query.OrderBy(x => x.PhoneNumber) : query.OrderByDescending(x => x.PhoneNumber);
                        break;
                    case "liveCheckStatus":
                        query = isAsc ? query.OrderBy(x => x.LiveCheckStatus) : query.OrderByDescending(x => x.LiveCheckStatus);
                        break;
                    case "postedAt":
                        query = isAsc ? query.OrderBy(x => x.PostedAt) : query.OrderByDescending(x => x.PostedAt);
                        break;
                    case "returnedAt":
                        query = isAsc ? query.OrderBy(x => x.ReturnedAt) : query.OrderByDescending(x => x.ReturnedAt);
                        break;
                    case "checkBy":
                        query = isAsc ? query.OrderBy(x => x.CheckBy) : query.OrderByDescending(x => x.CheckBy);
                        break;
                    default:
                        break;
                }
            }
            return query;
        }
    }
}
