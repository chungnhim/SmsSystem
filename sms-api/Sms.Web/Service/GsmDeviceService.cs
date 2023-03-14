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
    public interface IGsmDeviceService : IServiceBase<GsmDevice>
    {
        Task<int> ToggleDeviceStatus(GsmDevice device);
        Task<int> LookupIdFromCode(string code);
        Task<ApiResponseBaseModel> ResetSpecifiedServices(int gsmId);
        Task<ApiResponseBaseModel> ToggleMaintenanceStatus(int gsmId, bool isInMaintenance);
        Task<ApiResponseBaseModel<int>> CountActiveOrder(int gsmId);
        Task<ApiResponseBaseModel> ToggleServingThirdService(int gsmId, bool isOn);
        Task<ApiResponseBaseModel> UpdateGsmDevicePriority(int gsmId, int priority);
        Task<ApiResponseBaseModel> SpecifiedServices(List<int> ids, SpecifiedServiceForGsmRequest request);
        Task<GsmMaintenanceStatus> CheckMaintenanceStatus(int id);
        Task<ApiResponseBaseModel> ToggleWebOnly(int gsmId, bool webOnly);
        Task ProcessBank512Check();
        Task TouchLastActive(int gsmDeviceId);
    }
    public class GsmDeviceService : ServiceBase<GsmDevice>, IGsmDeviceService
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger _logger;
        private readonly ICacheService _cacheService;
        public GsmDeviceService(SmsDataContext smsDataContext,
            IAuthService authService,
            IUserService userService,
            IDateTimeService dateTimeService,
            ILogger<GsmDeviceService> logger,
            ICacheService cacheService
            ) : base(smsDataContext)
        {
            _authService = authService;
            _userService = userService;
            _dateTimeService = dateTimeService;
            _logger = logger;
            _cacheService = cacheService;
        }

        public override async Task<ApiResponseBaseModel<GsmDevice>> Create(GsmDevice model)
        {
            var currentUserId = _authService.CurrentUserId();
            if (currentUserId == null) return ApiResponseBaseModel<GsmDevice>.UnAuthorizedResponse();
            var currentUser = await _userService.Get(currentUserId.Value);
            if (currentUser == null) return ApiResponseBaseModel<GsmDevice>.UnAuthorizedResponse();
            if (currentUser.Role == RoleType.Staff)
            {
                model.UserGsmDevices = new List<UserGsmDevice>();
                model.UserGsmDevices.Add(new UserGsmDevice()
                {
                    UserId = currentUser.Id
                });
            }
            return await base.Create(model);
        }
        public async Task<int> LookupIdFromCode(string code)
        {
            return await GenerateQuery().Where(r => r.Code == code).Select(r => r.Id).FirstOrDefaultAsync();
        }

        public override void Map(GsmDevice entity, GsmDevice model)
        {
            entity.Code = model.Code;
            entity.Disabled = model.Disabled;
            entity.Name = model.Name;
            entity.Note = model.Note;
        }

        public async Task<ApiResponseBaseModel> ResetSpecifiedServices(int gsmId)
        {
            var currentUser = await _userService.GetCurrentUser();
            if (currentUser == null || currentUser.Role != RoleType.Administrator) return ApiResponseBaseModel.UnAuthorizedResponse();
            var gsm = await Get(gsmId);
            if (gsm == null) return new ApiResponseBaseModel() { Success = false, Message = "NotFound" };
            gsm.SpecifiedService = false;
            await _smsDataContext.SaveChangesAsync();
            return new ApiResponseBaseModel()
            {
                Success = true
            };
        }

        public async Task<int> ToggleDeviceStatus(GsmDevice device)
        {
            var entity = await this.Get(device.Id);
            if (entity == null) throw new Exception(string.Format("Can not ToggleDeviceStatus device with id {0} - Not found", device.Id));
            entity.Disabled = device.Disabled;
            return await _smsDataContext.SaveChangesAsync();
        }
        protected async override Task<string> ValidateEntry(GsmDevice entity)
        {
            if (await _smsDataContext.GsmDevices.AnyAsync(r => r.Id != entity.Id && r.Code == entity.Code && !string.IsNullOrEmpty(r.Code)))
            {
                return "DuplicateCode";
            }
            return null;
        }
        protected override IQueryable<GsmDevice> GenerateQuery(FilterRequest filterRequest = null)
        {
            var query = base.GenerateQuery(filterRequest);
            if (filterRequest != null)
            {
                query = query.Include(r => r.GsmDeviceServiceProviders);
            }
            var userId = _authService.CurrentUserId();
            var user = _smsDataContext.Users.FirstOrDefault(r => r.Id == userId);
            if (user.Role == RoleType.Staff)
            {
                query = query.Where(r => r.UserGsmDevices.Any(k => k.UserId == userId));
            }
            if (filterRequest != null && filterRequest.SearchObject != null)
            {
                {
                    if (filterRequest.SearchObject.TryGetValue("gsmName", out object obj))
                    {
                        var gsmDeviceName = obj.ToString();
                        if (!string.IsNullOrWhiteSpace(gsmDeviceName))
                        {
                            gsmDeviceName = gsmDeviceName.ToLower();

                            if (gsmDeviceName.StartsWith("[") && gsmDeviceName.EndsWith("]"))
                            {
                                gsmDeviceName = gsmDeviceName.Substring(1, gsmDeviceName.Length - 2);
                                query = query.Where(r => r.Name.ToLower() == gsmDeviceName);
                            }
                            else
                            {
                                query = query.Where(r => r.Name.ToLower().Contains(gsmDeviceName));
                            }
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("disabled", out object obj))
                    {
                        if (obj != null)
                        {
                            var b = (bool)obj;
                            query = query.Where(r => r.Disabled == b);
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("isInMaintenance", out object obj))
                    {
                        if (obj != null)
                        {
                            var b = (bool)obj;
                            query = query.Where(r => r.IsInMaintenance == b);
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("onlyWebOrder", out object obj))
                    {
                        if (obj != null)
                        {
                            var b = (bool)obj;
                            query = query.Where(r => r.OnlyWebOrder == b);
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("isServingForThirdService", out object obj))
                    {
                        if (obj != null)
                        {
                            var b = (bool)obj;
                            query = query.Where(r => r.IsServingForThirdService == b);
                        }
                    }
                }
            }
            return query;
        }

        public async Task<ApiResponseBaseModel> ToggleMaintenanceStatus(int gsmId, bool isInMaintenance)
        {
            var userId = _authService.CurrentUserId();
            if (userId == null) return ApiResponseBaseModel.UnAuthorizedResponse();
            var user = await _userService.Get(userId.GetValueOrDefault());
            if (user == null || user.Role == RoleType.User) return ApiResponseBaseModel.UnAuthorizedResponse();
            if (user.Role == RoleType.Staff)
            {
                var isGsmAuthorized = await _smsDataContext.UserGsmDevices.AnyAsync(r => r.UserId == user.Id && r.GsmDeviceId == gsmId);
                if (!isGsmAuthorized) return ApiResponseBaseModel.UnAuthorizedResponse();
            }
            var gsm = await _smsDataContext.GsmDevices.FirstOrDefaultAsync(r => r.Id == gsmId);
            if (gsm == null) return ApiResponseBaseModel.NotFoundResourceResponse();
            gsm.IsInMaintenance = isInMaintenance;
            gsm.LastMaintenanceTime = _dateTimeService.UtcNow();
            if (isInMaintenance)
            {
                await _cacheService.SetGsm512BlockedRangeNames(gsmId, null);
            }
            await _smsDataContext.SaveChangesAsync();
            return new ApiResponseBaseModel();
        }

        public async Task<ApiResponseBaseModel<int>> CountActiveOrder(int gsmId)
        {
            return new ApiResponseBaseModel<int>()
            {
                Results = await _smsDataContext.RentCodeOrders.CountAsync(r => r.ConnectedGsmId == gsmId && r.Status == OrderStatus.Waiting)
            };
        }

        public async Task<ApiResponseBaseModel> ToggleServingThirdService(int gsmId, bool isOn)
        {
            var userId = _authService.CurrentUserId();
            if (userId == null) return ApiResponseBaseModel.UnAuthorizedResponse();
            var user = await _userService.Get(gsmId);
            if (user == null) return ApiResponseBaseModel.UnAuthorizedResponse();
            if (user.Role == RoleType.Staff)
            {
                var isGsmAuthorized = await _smsDataContext.UserGsmDevices.AnyAsync(r => r.UserId == user.Id && r.GsmDeviceId == gsmId);
                if (!isGsmAuthorized) return ApiResponseBaseModel.UnAuthorizedResponse();
            }
            var gsm = await _smsDataContext.GsmDevices.FirstOrDefaultAsync(r => r.Id == gsmId);
            if (gsm == null) return ApiResponseBaseModel.NotFoundResourceResponse();
            gsm.IsServingForThirdService = isOn;
            await _smsDataContext.SaveChangesAsync();
            return new ApiResponseBaseModel();
        }

        public async Task<ApiResponseBaseModel> UpdateGsmDevicePriority(int gsmId, int priority)
        {
            var gsm = await _smsDataContext.GsmDevices.FirstOrDefaultAsync(r => r.Id == gsmId);
            if (gsm == null) return ApiResponseBaseModel.NotFoundResourceResponse();

            gsm.Priority = priority;
            await _smsDataContext.SaveChangesAsync();

            return new ApiResponseBaseModel();
        }

        public async Task<ApiResponseBaseModel> SpecifiedServices(List<int> ids, SpecifiedServiceForGsmRequest request)
        {
            var gsmDevices = await _smsDataContext.GsmDevices.Where(r => ids.Any(c => c == r.Id)).ToListAsync();
            foreach (var gsmDevice in gsmDevices)
            {
                gsmDevice.SpecifiedService = request.SpecifiedService;
            }

            var allConfigs = await _smsDataContext.GsmDeviceServiceProviders.Where(r => ids.Contains(r.GsmDeviceId)).ToListAsync();
            foreach (var config in allConfigs)
            {
                if (!request.ServiceProviderIds.Any(r => r == config.ServiceProviderId))
                {
                    _smsDataContext.GsmDeviceServiceProviders.Remove(config);
                }
            }
            var notIn = request.ServiceProviderIds.Where(r => !allConfigs.Any(x => x.ServiceProviderId == r)).ToList();
            foreach (var gsmDeviceId in ids)
            {
                foreach (var ni in notIn)
                {
                    var config = new GsmDeviceServiceProvider()
                    {
                        GsmDeviceId = gsmDeviceId,
                        ServiceProviderId = ni
                    };
                    _smsDataContext.GsmDeviceServiceProviders.Add(config);
                }
            }
            await _smsDataContext.SaveChangesAsync();

            return new ApiResponseBaseModel()
            {
                Success = true
            };
        }

        public async Task<GsmMaintenanceStatus> CheckMaintenanceStatus(int id)
        {
            var gsm = await Get(id);

            return new GsmMaintenanceStatus()
            {
                IsMaintenance = gsm.IsInMaintenance,
                StartedTime = gsm.IsInMaintenance ? gsm.LastMaintenanceTime : null,
                ServerCurrentTime = _dateTimeService.UtcNow()
            };
        }

        public async Task<ApiResponseBaseModel> ToggleWebOnly(int gsmId, bool webOnly)
        {
            var userId = _authService.CurrentUserId();
            if (userId == null) return ApiResponseBaseModel.UnAuthorizedResponse();
            var user = await _userService.Get(gsmId);
            if (user == null) return ApiResponseBaseModel.UnAuthorizedResponse();
            if (user.Role == RoleType.Staff)
            {
                var isGsmAuthorized = await _smsDataContext.UserGsmDevices.AnyAsync(r => r.UserId == user.Id && r.GsmDeviceId == gsmId);
                if (!isGsmAuthorized) return ApiResponseBaseModel.UnAuthorizedResponse();
            }
            var gsm = await _smsDataContext.GsmDevices.FirstOrDefaultAsync(r => r.Id == gsmId);
            if (gsm == null) return ApiResponseBaseModel.NotFoundResourceResponse();
            gsm.OnlyWebOrder = webOnly;
            await _smsDataContext.SaveChangesAsync();
            return new ApiResponseBaseModel();
        }

        public async Task ProcessBank512Check()
        {
            var orderHasProposedGsm512RangeName = await _smsDataContext
              .RentCodeOrders
              .Where(r => r.Status == OrderStatus.Waiting || r.Status == OrderStatus.Floating)
              .Where(r => !string.IsNullOrEmpty(r.ProposedGsm512RangeName))
              .Select(r => new { GsmId = r.ConnectedGsmId, RangeName = r.ProposedGsm512RangeName })
              .ToListAsync();
            var rangeNameGroupByGsmIds = (from o in orderHasProposedGsm512RangeName
                                          group o by o.GsmId into gr
                                          where gr.Key.HasValue
                                          select gr).ToList();

            foreach (var gr in rangeNameGroupByGsmIds)
            {
                await _cacheService.SetGsm512BlockedRangeNames(gr.Key.Value, gr.Select(r => r.RangeName).Distinct().ToList());
            }
        }

        public async Task TouchLastActive(int gsmDeviceId)
        {
            try
            {
                var gsmDevice = await Get(gsmDeviceId);
                gsmDevice.LastActivedAt = _dateTimeService.UtcNow();
                await _smsDataContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TouchLastActive failed {0}", gsmDeviceId);
            }
        }
    }
}
