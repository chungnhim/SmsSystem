using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
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
    public interface IServiceProviderService : IServiceBase<ServiceProvider>
    {
        Task<List<ServiceProvider>> GetAllAvailableServices(ServiceType? serviceType);
        Task<ServiceProvider> GetHoldingService();
        Task<ServiceProvider> GetCallbackService();
        Task<ApiResponseBaseModel> ApplyUserErrorForAll(int n);
        Task<ApiResponseBaseModel> ApplySingleErrorForAll(int n);
        Task<ApiResponseBaseModel<List<ServiceProviderMatchingTokens>>> TestAllMatchedService(string message, string sender);
        Task<ServiceProvider> GetInCache(int id);
    }
    public class ServiceProviderService : ServiceBase<ServiceProvider>, IServiceProviderService
    {
        private readonly ICacheService _cacheService;
        public ServiceProviderService(SmsDataContext smsDataContext, ICacheService cacheService) : base(smsDataContext)
        {
            _cacheService = cacheService;
        }

        public async Task<ServiceProvider> GetInCache(int id)
        {
            return (await _cacheService.GetAllServiceProviders()).FirstOrDefault(r => r.Id == id);
        }

        public async Task<List<ServiceProvider>> GetAllAvailableServices(ServiceType? serviceType)
        {
            var serviceTypes = new List<ServiceType>();
            if (serviceType.HasValue)
            {
                serviceTypes.Add(serviceType.Value);
            }
            else
            {
                serviceTypes.Add(ServiceType.Any);
                serviceTypes.Add(ServiceType.Basic);
            }

            var all = await _cacheService.GetAllServiceProviders();

            return all.Where(r => r.Disabled == false && serviceTypes.Contains(r.ServiceType)).ToList();
        }

        public async Task<ServiceProvider> GetHoldingService()
        {
            return (await _cacheService.GetAllServiceProviders()).FirstOrDefault(r => r.ServiceType == ServiceType.ByTime);
        }

        public async Task<ServiceProvider> GetCallbackService()
        {
            return (await _cacheService.GetAllServiceProviders()).FirstOrDefault(r => r.ServiceType == ServiceType.Callback);
        }

        public override void Map(ServiceProvider entity, ServiceProvider model)
        {
            entity.Disabled = model.Disabled;
            entity.LockTime = model.LockTime;
            entity.MessageRegex = model.MessageRegex;
            entity.MessageCodeRegex = model.MessageCodeRegex;
            entity.Name = model.Name;
            entity.Price = model.Price;
            entity.Price2 = model.Price2;
            entity.Price3 = model.Price3;
            entity.Price4 = model.Price4;
            entity.Price5 = model.Price5;
            entity.AdditionalPrice = model.AdditionalPrice;
            entity.ReceivingThreshold = model.ReceivingThreshold;
            entity.ErrorThreshold = model.ErrorThreshold;
            entity.TotalErrorThreshold = model.TotalErrorThreshold;
            entity.ServiceType = model.ServiceType;
            entity.AllowReceiveCall = model.AllowReceiveCall;
            entity.PriceReceiveCall = model.PriceReceiveCall;
            foreach (var network in entity.ServiceNetworkProviders)
            {
                if (!model.ServiceNetworkProviders.Any(r => r.NetworkProviderId == network.NetworkProviderId))
                {
                    _smsDataContext.ServiceNetworkProviders.Remove(network);
                }
            }
            foreach (var network in model.ServiceNetworkProviders)
            {
                if (!entity.ServiceNetworkProviders.Any(r => r.NetworkProviderId == network.NetworkProviderId))
                {
                    _smsDataContext.ServiceNetworkProviders.Add(new ServiceNetworkProvider()
                    {
                        NetworkProviderId = network.NetworkProviderId,
                        ServiceProviderId = entity.Id
                    });
                }
            }
        }
        protected override IQueryable<ServiceProvider> GenerateQuery(FilterRequest filterRequest = null)
        {
            var query = base.GenerateQuery(filterRequest);
            query = query.Include(r => r.ServiceNetworkProviders);
            if (filterRequest != null)
            {
                if (filterRequest.SearchObject != null)
                {
                    {
                        if (filterRequest.SearchObject.TryGetValue("Disabled", out object obj))
                        {
                            var disabled = (bool)obj;
                            query = query.Where(r => r.Disabled == disabled);
                        }
                    }
                    {
                        if (filterRequest.SearchObject.TryGetValue("SearchText", out object obj) == true)
                        {
                            var search = (string)obj;
                            query = query.Where(r => r.Name.Contains(search));
                        }
                    }
                    {
                        if (filterRequest.SearchObject.TryGetValue("ForClient", out object obj) == true)
                        {
                            var search = (bool)obj;
                            if (search)
                            {
                                query = query.Where(r => r.ServiceType != ServiceType.ByTime && r.ServiceType != ServiceType.Callback);
                            }
                        }
                    }
                    {
                        if (filterRequest.SearchObject.TryGetValue("ServiceType", out object obj))
                        {
                            var ts = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<ServiceType>>();
                            if (ts.Count > 0)
                            {
                                query = query.Where(r => ts.Contains(r.ServiceType));
                            }
                        }
                    }
                }
            }
            return query;
        }
        protected override async Task<string> ValidateEntry(ServiceProvider entity)
        {
            var dupplicateName = await _smsDataContext.ServiceProviders.AnyAsync(r => r.Name == entity.Name && r.Id != entity.Id);
            if (dupplicateName)
            {
                return "DuplicateName";
            }
            return await base.ValidateEntry(entity);
        }

        public async Task<ApiResponseBaseModel> ApplyUserErrorForAll(int n)
        {
            var services = await _smsDataContext.ServiceProviders.ToListAsync();
            foreach (var service in services)
            {
                service.TotalErrorThreshold = n;
            }
            await _smsDataContext.SaveChangesAsync();
            return new ApiResponseBaseModel();
        }

        public async Task<ApiResponseBaseModel> ApplySingleErrorForAll(int n)
        {
            var services = await _smsDataContext.ServiceProviders.ToListAsync();
            foreach (var service in services)
            {
                service.ErrorThreshold = n;
            }
            await _smsDataContext.SaveChangesAsync();
            return new ApiResponseBaseModel();
        }

        public async Task<ApiResponseBaseModel<List<ServiceProviderMatchingTokens>>> TestAllMatchedService(string message, string sender)
        {
            var allBasicServices = await _smsDataContext.ServiceProviders.Where(r => r.ServiceType == ServiceType.Basic && r.Disabled != true).ToListAsync();
            var tokens = allBasicServices.Select(r => {
                var matchingTokens = Helpers.MessageMatchingHelpers.BuildSmsMatchingTokens(r.MessageRegex, message, sender);
                return new ServiceProviderMatchingTokens()
                {
                    ServiceProvider = r,
                    ContentTokens = matchingTokens.ContentTokens,
                    SenderTokens = matchingTokens.SenderTokens
                };
            }).ToList();
            return new ApiResponseBaseModel<List<ServiceProviderMatchingTokens>>()
            {
                Results = tokens.Where(r => r.SenderTokens.Any() || r.ContentTokens.Any()).ToList()
            };
        }

        protected async override Task AfterCreated()
        {
            await _cacheService.RemoveAllServiceProvidersCache();
        }

        protected async override Task AfterUpdated()
        {
            await _cacheService.RemoveAllServiceProvidersCache();
        }

        protected async override Task AfterDeleted()
        {
            await _cacheService.RemoveAllServiceProvidersCache();
        }
    }
}
