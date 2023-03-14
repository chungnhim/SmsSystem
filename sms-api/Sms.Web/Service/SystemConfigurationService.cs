using Microsoft.EntityFrameworkCore;
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
  public interface ISystemConfigurationService : IServiceBase<SystemConfiguration>
  {
    Task<SystemConfiguration> GetSystemConfiguration();
  }
  public class SystemConfigurationService : ServiceBase<SystemConfiguration>, ISystemConfigurationService
  {
    private readonly ICacheService _cacheService;
    public SystemConfigurationService(SmsDataContext smsDataContext, ICacheService cacheService) : base(smsDataContext)
    {
      _cacheService = cacheService;
    }
    public override async Task<List<SystemConfiguration>> GetAlls()
    {
      var entity = await GetSystemConfiguration();

      if (entity == null)
      {
        entity = new SystemConfiguration();
        _smsDataContext.SystemConfigurations.Add(entity);
        await _smsDataContext.SaveChangesAsync();
        await _cacheService.RemoveSystemConfigurationCache();
      }
      return new List<SystemConfiguration>() { entity };
    }

    public async Task<SystemConfiguration> GetSystemConfiguration()
    {
      return await _cacheService.GetSystemConfiguration();
    }

    public override void Map(SystemConfiguration entity, SystemConfiguration model)
    {
      entity.MaximumAvailableOrder = model.MaximumAvailableOrder;
      entity.ThresholdsForAutoSuspend = model.ThresholdsForAutoSuspend;
      entity.BrandName = model.BrandName;
      entity.UsdRate = model.UsdRate;
      entity.Email = model.Email;
      entity.FacebookUrl = model.FacebookUrl;
      entity.YoutubeUrl = model.YoutubeUrl;
      entity.TelegramUrl = model.TelegramUrl;
      entity.LogoUrl = model.LogoUrl;
      entity.MomoAccessKey = model.MomoAccessKey;
      entity.MomoApiEndPoint = model.MomoApiEndPoint;
      entity.MomoPartnerCode = model.MomoPartnerCode;
      entity.MomoSecretKey = model.MomoSecretKey;
      entity.NganLuongIsLiveEnvironment = model.NganLuongIsLiveEnvironment;
      entity.NganLuongLiveMerchantCode = model.NganLuongLiveMerchantCode;
      entity.NganLuongLiveMerchantPassword = model.NganLuongLiveMerchantPassword;
      entity.NganLuongLiveReceiver = model.NganLuongLiveReceiver;
      entity.NganLuongSandboxMerchantCode = model.NganLuongSandboxMerchantCode;
      entity.NganLuongSandboxMerchantPassword = model.NganLuongSandboxMerchantPassword;
      entity.NganLuongSandboxReceiver = model.NganLuongSandboxReceiver;
      entity.PhoneNumber = model.PhoneNumber;
      entity.ThresholdsForWarning = model.ThresholdsForWarning;
      entity.AdminNotification = model.AdminNotification;
      entity.InternalTransferFee = model.InternalTransferFee;
      entity.ExternalTransferFee = model.ExternalTransferFee;
      entity.FrontendVersion = model.FrontendVersion;
    }

    protected override async Task AfterUpdated()
    {
      await _cacheService.RemoveSystemConfigurationCache();
    }
  }
}
