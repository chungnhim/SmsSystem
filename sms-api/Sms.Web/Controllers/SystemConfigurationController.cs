using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Sms.Web.Entity;
using Sms.Web.Models;
using Sms.Web.Service;

namespace Sms.Web.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  [Authorize(Roles = "Administrator")]
  public class SystemConfigurationController : BaseRestfulController<ISystemConfigurationService, SystemConfiguration>
  {
    public SystemConfigurationController(ISystemConfigurationService SystemConfigurationService) : base(SystemConfigurationService)
    {
    }
    [Obsolete]
    [ApiExplorerSettings(IgnoreApi = true)]
    public override Task<ApiResponseBaseModel<SystemConfiguration>> Put(int id, [FromBody] SystemConfiguration value)
    {
      return base.Put(id, value);
    }
    [Obsolete]
    [ApiExplorerSettings(IgnoreApi = true)]
    public override Task<ApiResponseBaseModel<SystemConfiguration>> Post([FromBody] SystemConfiguration value)
    {
      return base.Post(value);
    }
    [Obsolete]
    [ApiExplorerSettings(IgnoreApi = true)]
    public override Task<ApiResponseBaseModel<int>> Delete(int id)
    {
      return base.Delete(id);
    }

    [Obsolete]
    [ApiExplorerSettings(IgnoreApi = true)]
    public override Task<SystemConfiguration> Get(int id)
    {
      return base.Get(id);
    }
    [Obsolete]
    [ApiExplorerSettings(IgnoreApi = true)]
    public override Task<FilterResponse<SystemConfiguration>> Paging([FromBody] FilterRequest filterRequest)
    {
      return base.Paging(filterRequest);
    }
  }

  [Route("api/[controller]")]
  [ApiController]
  [AllowAnonymous]
  [ApiExplorerSettings(IgnoreApi = true)]
  public class ClientSystemConfigurationController : ControllerBase
  {
    private readonly ISystemConfigurationService _systemConfigurationService;
    public ClientSystemConfigurationController(ISystemConfigurationService systemConfigurationService)
    {
      _systemConfigurationService = systemConfigurationService;
    }
    [HttpGet("")]
    public async Task<ClientSystemConfigurationModel> Get()
    {
      var systemConfiguration = await _systemConfigurationService.GetSystemConfiguration();
      return new ClientSystemConfigurationModel()
      {
        BrandName = systemConfiguration.BrandName,
        Email = systemConfiguration.Email,
        FacebookUrl = systemConfiguration.FacebookUrl,
        YoutubeUrl = systemConfiguration.YoutubeUrl,
        TelegramUrl = systemConfiguration.TelegramUrl,
        LogoUrl = systemConfiguration.LogoUrl,
        PhoneNumber = systemConfiguration.PhoneNumber,
        AdminNotification = systemConfiguration.AdminNotification,
        UsdRate = systemConfiguration.UsdRate,
        ReferalFee = systemConfiguration.ReferalFee,
        ReferredUserFee = systemConfiguration.ReferredUserFee,
        InternalTransferFee = systemConfiguration.InternalTransferFee,
        ExternalTransferFee = systemConfiguration.ExternalTransferFee,
        FrontendVersion = systemConfiguration.FrontendVersion,
      };
    }
  }
}
