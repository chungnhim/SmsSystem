using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Sms.Web.Entity;
using Sms.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Sms.Web.Service
{
    public interface ICacheService
    {
        Task<List<int>> Gsm512IdList();
        Task<List<string>> GetAvailablePhoneNumberForServiceProvider(int serviceProviderId);
        Task SetAvailablePhoneNumberForServiceProvider(int serviceProviderId, List<string> phoneNumbers);
        Task RemoveAnAvailablePhoneNumberForServiceProvider(int serviceProviderId, string phoneNumber);
        Task<List<ServiceProviderAvailableCacheModel>> GetServiceProviderAvailableCache(List<int> serviceProviderIds);
        Task SetServiceProviderAvailableCache(ServiceProviderAvailableCacheModel models);
        Task<List<ServiceProvider>> GetAllServiceProviders();
        Task RemoveAllServiceProvidersCache();

        Task<SystemConfiguration> GetSystemConfiguration();
        Task RemoveSystemConfigurationCache();

        Task AddGsm512BlockedRangeName(int gsmId, string rangeName);
        Task RemoveGsm512BlockedRangeName(int gsmId, string rangeName);
        Task SetGsm512BlockedRangeNames(int gsmId, List<string> rangeNames);
        Task<List<string>> GetGsm512BlockedRangeNames(int gsmId);

        int IncreaseServiceProviderContinuosFailedCount(int serviceProviderId);
        int ResetServiceProviderContinuosFailedCount(int serviceProviderId);
        Task<int> IncreaseUserContinuosFailedCount(int serviceProviderId);
        Task<int> ResetUserContinuosFailedCount(int serviceProviderId);
        Task<int> IncreaseGsmServiceProviderContinuosFailedCount(int gsmId, int serviceProviderId);
        Task<int> ResetGsmServiceProviderContinuosFailedCount(int gsmId, int serviceProviderId);
        Task CacheTkaoPortalUser(PortalUser tkaoUser);
        Task<PortalUser> GetTkaoPortalUser(int userId);
    }
    public class CacheService : ICacheService
    {
        private readonly string GSM_512_ID_LIST_CACHE_KEY = "GSM_512_ID_LIST_CACHE_KEY";
        private readonly string SYSTEM_CONFIGURATION_CACHE_KEY = "SYSTEM_CONFIGURATION_CACHE_KEY";
        private readonly string ALL_SERVICE_PROVIDERS_CACHE_KEY = "ALL_SERVICE_PROVIDERS_CACHE_KEY";
        private string SERVICE_AVAILABLE_COUNT_CACHE_KEY(int serviceProviderId) => $"SERVICE_AVAILABLE_COUNT_CACHE_KEY_{serviceProviderId}";
        private string AVAILABLE_PHONE_NUMBER_FOR_SERVICE_PROVIDER(int s) => string.Format("AVAILABLE_PHONE_NUMBER_FOR_SERVICE_PROVIDER_{0}", s);
        private string GSM_512_BLOCKED_RANGE_NAMES_CACHE_KEY(int gsmId) => $"GSM_512_BLOCKED_RANGE_NAMES_CACHE_KEY_{gsmId}";
        private string SERVICE_PROVIDER_CONTINUOS_FAILED_COUNT(int serviceProviderId) => $"SERVICE_PROVIDER_CONTINUOS_FAILED_COUNT_{serviceProviderId}";
        private string USER_CONTINUOS_FAILED_COUNT(int userId) => $"USER_CONTINUOS_FAILED_COUNT_{userId}";
        private string GSM_SERVICE_PROVIDER_CONTINUOS_FAILED_COUNT(int gsmId, int serviceProviderId) => $"GSM_SERVICE_PROVIDER_CONTINUOS_FAILED_COUNT_{gsmId}_{serviceProviderId}";
        private string TKAO_PORTAL_USER_CACHE_KEY(int tkaoUserId) => $"TKAO_PORTAL_USER_CACHE_KEY_{tkaoUserId}";
        private readonly IMemoryCache _memoryCache;
        private readonly SmsDataContext _smsDataContext;
        private readonly IDistributedCache _distributedCache;
        private readonly IDateTimeService _dateTimeService;

        public CacheService(SmsDataContext smsDataContext,
            IMemoryCache memoryCache,
            IDateTimeService dateTimeService,
            IDistributedCache distributedCache)
        {
            _smsDataContext = smsDataContext;
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
            _dateTimeService = dateTimeService;
        }

        #region Private methods

        private async Task<T> GetObject<T>(string key, T objDefault = default(T))
        {
            var json = await _distributedCache.GetStringAsync(key);
            if (string.IsNullOrEmpty(json))
            {
                return objDefault;
            }
            var cacheObject = ToObject<CacheObject<T>>(json);
            if (cacheObject.Expired < _dateTimeService.UtcNow())
            {
                return objDefault;
            }
            return cacheObject.Object;
        }

        private async Task<DateTime?> GetExpiredOfKey<T>(string key)
        {
            var json = await _distributedCache.GetStringAsync(key);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            var cacheObject = ToObject<CacheObject<T>>(json);
            if (cacheObject.Expired < _dateTimeService.UtcNow()) return null;
            return cacheObject.Expired;
        }

        private async Task SaveObject<T>(string key, T obj, TimeSpan span, bool ignoreCurrentExpiredTime = false)
        {
            if (obj == null)
            {
                await _distributedCache.RemoveAsync(key);
                return;
            }
            var expired = _dateTimeService.UtcNow().Add(span);
            if (!ignoreCurrentExpiredTime)
            {
                var currentExpired = await GetExpiredOfKey<T>(key);
                if (currentExpired != null)
                {
                    expired = currentExpired.Value;
                }
            }

            var cacheObject = new CacheObject<T>()
            {
                Expired = expired,
                Object = obj
            };

            await _distributedCache.SetStringAsync(key, ToJson(cacheObject), OptionsFrom(TimeSpan.FromHours(1)));
        }

        private async Task RemoveCacheKey(string key)
        {
            await _distributedCache.RemoveAsync(key);
        }

        private string ToJson<T>(T entity)
        {
            return JsonConvert.SerializeObject(entity);
        }

        private T ToObject<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        private DistributedCacheEntryOptions OptionsFrom(TimeSpan span) => new DistributedCacheEntryOptions()
        {
            SlidingExpiration = span
        };
        #endregion

        public async Task<List<int>> Gsm512IdList()
        {
            return await _memoryCache.GetOrCreateAsync(GSM_512_ID_LIST_CACHE_KEY, async settings =>
            {
                settings.SetAbsoluteExpiration(TimeSpan.FromHours(3));
                return await _smsDataContext.GsmDevices
                    .Where(r => r.Name.StartsWith("gsm512", StringComparison.OrdinalIgnoreCase))
                    .Select(r => r.Id).ToListAsync();
            });
        }

        public async Task<List<string>> GetAvailablePhoneNumberForServiceProvider(int serviceProviderId)
        {
            return await GetObject<List<string>>(AVAILABLE_PHONE_NUMBER_FOR_SERVICE_PROVIDER(serviceProviderId));
        }

        public async Task SetAvailablePhoneNumberForServiceProvider(int serviceProviderId, List<string> phoneNumbers)
        {
            await SaveObject(AVAILABLE_PHONE_NUMBER_FOR_SERVICE_PROVIDER(serviceProviderId), phoneNumbers, TimeSpan.FromSeconds(20));
        }

        public async Task RemoveAnAvailablePhoneNumberForServiceProvider(int serviceProviderId, string phoneNumber)
        {
            var list = await GetAvailablePhoneNumberForServiceProvider(serviceProviderId);
            if (list == null || list.Count == 0 || !list.Contains(phoneNumber)) return;

            list = list.Where(r => r != phoneNumber).ToList();
            await SetAvailablePhoneNumberForServiceProvider(serviceProviderId, list);
        }

        public async Task<List<ServiceProviderAvailableCacheModel>> GetServiceProviderAvailableCache(List<int> serviceProviderIds)
        {
            var list = serviceProviderIds.Select(async x =>
                await GetObject(SERVICE_AVAILABLE_COUNT_CACHE_KEY(x), new ServiceProviderAvailableCacheModel()));
            return (await Task.WhenAll(list)).Where(r => r != null).ToList();
        }

        public async Task SetServiceProviderAvailableCache(ServiceProviderAvailableCacheModel model)
        {
            await RemoveCacheKey(SERVICE_AVAILABLE_COUNT_CACHE_KEY(model.ServiceProviderId));
            await SaveObject(SERVICE_AVAILABLE_COUNT_CACHE_KEY(model.ServiceProviderId), model, TimeSpan.FromMinutes(10));
        }

        public async Task<List<ServiceProvider>> GetAllServiceProviders()
        {
            var serviceProviders = await GetObject<List<ServiceProvider>>(ALL_SERVICE_PROVIDERS_CACHE_KEY);
            if (serviceProviders == null)
            {
                serviceProviders = await _smsDataContext.ServiceProviders.AsNoTracking().ToListAsync();
                await SaveObject(ALL_SERVICE_PROVIDERS_CACHE_KEY, serviceProviders, TimeSpan.FromDays(1));
            }
            return serviceProviders;
        }

        public async Task RemoveAllServiceProvidersCache()
        {
            await RemoveCacheKey(ALL_SERVICE_PROVIDERS_CACHE_KEY);
        }


        public async Task<SystemConfiguration> GetSystemConfiguration()
        {
            var obj = await GetObject<SystemConfiguration>(SYSTEM_CONFIGURATION_CACHE_KEY);
            if (obj == null)
            {
                obj = _smsDataContext.SystemConfigurations.FirstOrDefault();
                await SaveObject(SYSTEM_CONFIGURATION_CACHE_KEY, obj, TimeSpan.FromDays(1));
            }
            return obj;
        }

        public async Task RemoveSystemConfigurationCache()
        {
            await RemoveCacheKey(SYSTEM_CONFIGURATION_CACHE_KEY);
        }

        public async Task AddGsm512BlockedRangeName(int gsmId, string rangeName)
        {
            var list = await GetGsm512BlockedRangeNames(gsmId) ?? new List<string>();
            list.Add(rangeName);
            list = list.Distinct().ToList();
            await SetGsm512BlockedRangeNames(gsmId, list);
        }

        public async Task RemoveGsm512BlockedRangeName(int gsmId, string rangeName)
        {
            var list = await GetGsm512BlockedRangeNames(gsmId) ?? new List<string>();
            list = list.Where(r => r != rangeName).ToList();
            await SetGsm512BlockedRangeNames(gsmId, list);
        }

        public async Task<List<string>> GetGsm512BlockedRangeNames(int gsmId)
        {
            return await GetObject<List<string>>(GSM_512_BLOCKED_RANGE_NAMES_CACHE_KEY(gsmId));
        }

        public async Task SetGsm512BlockedRangeNames(int gsmId, List<string> rangeNames)
        {
            await SaveObject(GSM_512_BLOCKED_RANGE_NAMES_CACHE_KEY(gsmId), rangeNames, TimeSpan.FromSeconds(20));
        }

        public int IncreaseServiceProviderContinuosFailedCount(int serviceProviderId)
        {
            var current = _memoryCache.GetOrCreate(SERVICE_PROVIDER_CONTINUOS_FAILED_COUNT(serviceProviderId), settings =>
            {
                settings.SetSlidingExpiration(TimeSpan.FromDays(1));
                return 0;
            });
            var next = current + 1;

            _memoryCache.Set(SERVICE_PROVIDER_CONTINUOS_FAILED_COUNT(serviceProviderId), next);

            return next;
        }

        public int ResetServiceProviderContinuosFailedCount(int serviceProviderId)
        {
            _memoryCache.Remove(SERVICE_PROVIDER_CONTINUOS_FAILED_COUNT(serviceProviderId));
            return 0;
        }

        public async Task<int> IncreaseUserContinuosFailedCount(int userId)
        {
            var current = await GetObject<int>(USER_CONTINUOS_FAILED_COUNT(userId));
            var next = current + 1;
            await SaveObject(USER_CONTINUOS_FAILED_COUNT(userId), next, TimeSpan.FromHours(1), true);
            return next;
        }

        public async Task<int> ResetUserContinuosFailedCount(int userId)
        {
            await SaveObject(USER_CONTINUOS_FAILED_COUNT(userId), 0, TimeSpan.FromHours(1), false);
            return 0;
        }

        public async Task<int> IncreaseGsmServiceProviderContinuosFailedCount(int gsmId, int serviceProviderId)
        {
            var key = GSM_SERVICE_PROVIDER_CONTINUOS_FAILED_COUNT(gsmId, serviceProviderId);
            var current = await GetObject<int>(key);
            var next = current + 1;
            await SaveObject(key, next, TimeSpan.FromHours(1), true);
            return next;
        }

        public async Task<int> ResetGsmServiceProviderContinuosFailedCount(int gsmId, int serviceProviderId)
        {
            var key = GSM_SERVICE_PROVIDER_CONTINUOS_FAILED_COUNT(gsmId, serviceProviderId);
            await SaveObject(key, 0, TimeSpan.FromHours(1), false);
            return 0;
        }

        public async Task CacheTkaoPortalUser(PortalUser tkaoUser)
        {
            var key = TKAO_PORTAL_USER_CACHE_KEY(tkaoUser.UserId);
            await SaveObject(key, tkaoUser, TimeSpan.FromHours(1), true);
        }

        public async Task<PortalUser> GetTkaoPortalUser(int userId)
        {
            var key = TKAO_PORTAL_USER_CACHE_KEY(userId);
            return await GetObject<PortalUser>(key, null);
        }
    }
}
