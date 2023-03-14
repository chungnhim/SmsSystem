using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Sms.Web.Entity;
using Sms.Web.Helpers;
using Sms.Web.Middleware.Filters;
using Sms.Web.Models;
using Sms.Web.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Controllers
{
  [AllowAnonymous]
  [Route("api/portal")]
  [ApiController]
  [ServiceFilter(typeof(PortalAuthorize))]
  public class PortalConnectorController : ControllerBase
  {
    private readonly IPortalService _portalService;
    public PortalConnectorController()
    {
    }
    [HttpGet("check-user")]
    public async Task<PortalUser> CheckUsername(string username)
    {
      return await _portalService.CheckUsername(username);
    }
    [HttpPost("transfer-money")]
    public async Task<ApiResponseBaseModel> TransferMoney([FromBody]PortalTransferMoneyRequest request)
    {
      return await _portalService.TransferMoney(request);
    }
  }
}
