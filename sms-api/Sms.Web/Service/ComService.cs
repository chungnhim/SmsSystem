using Microsoft.EntityFrameworkCore;
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
    public interface IComService : IServiceBase<Com>
    {
        Task<List<Com>> GetComsByGsmId(int gsmId);
        Task<Com> LookupComFromGsmCodeAndComCode(string gsmCode, string comCode);
        Task<ApiResponseBaseModel<List<PhoneNumberEfficiency>>> CheckPhoneNumberEfficiency(CheckPhoneNumberEfficiencyRequest request);
        Task<List<Com>> GetListerningComsByGsmId(int id);
        Task CalculatePhoneEfficiencyForAllComs();
    }
    public class ComService : ServiceBase<Com>, IComService
    {
        private readonly IServiceProviderService _serviceProviderService;
        private readonly IProviderHistoryService _providerHistoryService;
        private readonly IAuthService _authService;
        private readonly ILogger _logger;
        public ComService(SmsDataContext smsDataContext, IServiceProviderService serviceProviderService, IProviderHistoryService providerHistoryService, IAuthService authService, ILogger<ComService> logger) : base(smsDataContext)
        {
            _serviceProviderService = serviceProviderService;
            _providerHistoryService = providerHistoryService;
            _authService = authService;
            _logger = logger;
        }

        public override void Map(Com entity, Com model)
        {
            entity.PhoneNumber = model.PhoneNumber;
            entity.NetworkProvider = model.NetworkProvider;
            if (entity.PhoneNumber != model.PhoneNumber || !model.Disabled && entity.Disabled)
            {
                entity.PhoneEfficiency = null;
            }
            entity.Disabled = model.Disabled;
        }
        protected override async Task<string> ValidateEntry(Com entity)
        {
            var duplicateComCode = await _smsDataContext.Coms.AnyAsync(r => r.ComName != null && r.ComName == entity.ComName && r.GsmDeviceId == entity.GsmDeviceId && r.Id != entity.Id);
            if (duplicateComCode)
            {
                return "DuplicateComCode";
            }
            return await base.ValidateEntry(entity);
        }
        protected override IQueryable<Com> GenerateQuery(FilterRequest filterRequest)
        {
            var query = base.GenerateQuery(filterRequest);
            query = query.Include(x => x.GsmDevice);

            var userId = _authService.CurrentUserId();
            var user = _smsDataContext.Users.FirstOrDefault(r => r.Id == userId);
            if (user.Role == RoleType.Staff)
            {
                query = query.Where(r => r.GsmDevice.UserGsmDevices.Any(k => k.UserId == userId));
            }
            if (filterRequest != null)
            {
                var sortColumn = filterRequest.SortColumnName ?? string.Empty;
                var isAsc = filterRequest.IsAsc;
                var phoneNumber = string.Empty;
                var gsmDeviceIds = new List<int>();
                var serviceProviderIds = new List<int>();
                bool? disabled = null;
                {
                    if (filterRequest.SearchObject.TryGetValue("phoneNumber", out object obj) == true)
                    {
                        phoneNumber = (string)obj;
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("gsmDevice", out object obj) == true)
                    {
                        gsmDeviceIds = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<int>>();
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("serviceProviderIds", out object obj) == true)
                    {
                        serviceProviderIds = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<int>>();
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("disabled", out object obj) == true)
                    {
                        disabled = (bool)obj;
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("fullEnegy", out object obj) == true)
                    {
                        var fullEnegy = (bool)obj;
                        if (fullEnegy)
                        {
                            query = query.Where(r => r.PhoneEfficiency == 100);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(phoneNumber))
                {
                    query = query.Where(x => x.PhoneNumber.Contains(phoneNumber));
                }
                if (gsmDeviceIds.Count > 0)
                {
                    query = query.Where(x => gsmDeviceIds.Contains(x.GsmDevice.Id));
                }
                if (disabled != null)
                {
                    query = query.Where(x => x.Disabled == disabled && x.GsmDevice.Disabled == disabled);
                }
                if (serviceProviderIds.Any())
                {
                    query = from c in query
                            join s in _smsDataContext.ProviderHistories on c.PhoneNumber equals s.PhoneNumber
                            where s.ServiceProviderId.HasValue && serviceProviderIds.Contains(s.ServiceProviderId.Value)
                            group c by c.Id into gr
                            select gr.FirstOrDefault();
                    query = query.Include(r => r.GsmDevice);
                }
                switch (sortColumn)
                {
                    case "name":
                        query = isAsc ? query.OrderBy(x => x.GsmDevice.Name) : query.OrderByDescending(x => x.GsmDevice.Name);
                        break;
                    case "code":
                        query = isAsc ? query.OrderBy(x => x.ComName) : query.OrderByDescending(x => x.ComName);
                        break;
                    case "note":
                        query = isAsc ? query.OrderBy(x => x.PhoneNumber) : query.OrderByDescending(x => x.PhoneNumber);
                        break;
                    case "disabled":
                        query = isAsc ? query.OrderBy(x => x.Disabled) : query.OrderByDescending(x => x.Disabled);
                        break;
                    case "phoneEfficiency":
                        query = isAsc ? query.OrderBy(x => x.PhoneEfficiency) : query.OrderByDescending(x => x.PhoneEfficiency);
                        break;
                    default:
                        break;
                }
            }
            return query;
        }

        public override async Task<Dictionary<string, object>> GetFilterAdditionalData(FilterRequest filterRequest, IQueryable<Com> query)
        {
            if (filterRequest != null)
            {
                if (filterRequest.SearchObject.TryGetValue("serviceProviderIds", out object obj) == true)
                {
                    var serviceProviderIds = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<int>>();
                    if (serviceProviderIds.Count > 0)
                    {
                        return new Dictionary<string, object>()
                        {
                            {"TotalCount", 0 }
                        };
                    }
                }
            }
            var count = await (from c in query
                            join o in _smsDataContext.RentCodeOrders on c.PhoneNumber equals o.PhoneNumber
                            where c.PhoneNumber != null && o.Status == OrderStatus.Success
                                    && (o.ServiceProvider.ServiceType == ServiceType.Basic || o.ServiceProvider.ServiceType == ServiceType.Any)
                            select 1
                ).CountAsync();

            return new Dictionary<string, object>()
            {
                {"TotalCount", count }
            };
        }

        public async Task<List<Com>> GetComsByGsmId(int gsmId)
        {
            return await _smsDataContext.Coms.Where(r => r.GsmDeviceId == gsmId).ToListAsync();
        }

        public async Task<Com> LookupComFromGsmCodeAndComCode(string gsmCode, string comCode)
        {
            return await (from gsm in _smsDataContext.GsmDevices
                          where gsm.Code == gsmCode
                          join com in _smsDataContext.Coms on gsm.Id equals com.GsmDeviceId
                          where com.ComName == comCode
                          select com
                          ).FirstOrDefaultAsync();
        }

        public async Task<ApiResponseBaseModel<List<PhoneNumberEfficiency>>> CheckPhoneNumberEfficiency(CheckPhoneNumberEfficiencyRequest request)
        {
            request.PhoneNumbers = request.PhoneNumbers.Where(r => !string.IsNullOrEmpty(r)).ToList();
            var serviceProviders = await _serviceProviderService.GetAllAvailableServices(null);
            var capacityServices = serviceProviders.Where(r => r.ServiceType == ServiceType.Basic || r.ServiceType == ServiceType.Any).ToList();
            var successOrderQuery = _smsDataContext.RentCodeOrders.Where(r => r.Status == OrderStatus.Success);
            var providerHistories = await successOrderQuery.Where(r => request.PhoneNumbers.Contains(r.PhoneNumber)).ToListAsync();
            return new ApiResponseBaseModel<List<PhoneNumberEfficiency>>()
            {
                Success = true,
                Results = request.PhoneNumbers.Select(phoneNumber =>
                {
                    return new PhoneNumberEfficiency()
                    {
                        PhoneNumber = phoneNumber,
                        Services = capacityServices.Select(serviceProvider =>
                        {
                            return new ServicePhoneNumberEfficiency()
                            {
                                ServiceProvider = serviceProvider,
                                UsedCount = providerHistories.Count(r => r.ServiceProviderId == serviceProvider.Id && r.PhoneNumber == phoneNumber)
                            };
                        }).ToList()
                    };
                }).ToList()
            };
        }
        public async Task<List<Com>> GetListerningComsByGsmId(int gsmId)
        {
            return await (from com in _smsDataContext.Coms
                          where com.GsmDeviceId == gsmId
                          join o in _smsDataContext.Orders on com.PhoneNumber equals o.PhoneNumber
                          where o.Status == OrderStatus.Waiting
                          select com).ToListAsync();
        }

        public async Task CalculatePhoneEfficiencyForAllComs()
        {
            var pendingComQuery = _smsDataContext.Coms.Where(r => !string.IsNullOrEmpty(r.PhoneNumber) && r.PhoneEfficiency == null && !r.Disabled && !r.GsmDevice.Disabled);
            var successOrderQuery = _smsDataContext.Orders.Where(r => r.Status == OrderStatus.Success);
            var capacityServices = (await _serviceProviderService.GetAllAvailableServices(null)).Where(r => r.ServiceType == ServiceType.Basic || r.ServiceType == ServiceType.Any).ToList();
            var totalCapacity = capacityServices.Sum(r => r.ReceivingThreshold);
            var topComs = await pendingComQuery.Take(100).ToListAsync();
            foreach (var com in topComs)
            {
                var doneCount = await successOrderQuery.Where(r => r.PhoneNumber == com.PhoneNumber).CountAsync();
                var phoneEfficiency = totalCapacity == 0 ? 0 : doneCount * 100.0f / totalCapacity;
                com.PhoneEfficiency = 100 - phoneEfficiency;
                _logger.LogInformation(string.Format("Calculate efficiency {0} -----> {1}", com.PhoneNumber, com.PhoneEfficiency));
            }
            await _smsDataContext.SaveChangesAsync();
        }
    }
}
