using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Sms.Web.Helpers;

namespace Sms.Web.Middleware.Filters
{
  public class PortalAuthorize : ActionFilterAttribute
  {
    private readonly AppSettings _appSettings;
    public PortalAuthorize(IOptions<AppSettings> appSettings)
    {
      _appSettings = appSettings.Value;
    }
    public override void OnActionExecuting(ActionExecutingContext context)
    {
      var headers = context.HttpContext.Request.Headers;
      if (!headers.ContainsKey("Authorization"))
      {
        context.Result = new UnauthorizedResult();
        return;
      }
      var header = headers["Authorization"].ToString();
      var payload = header.Split(" ")[1];

      if (string.IsNullOrEmpty(payload))
      {
        context.Result = new UnauthorizedResult();
        return;
      }

      var isAuthenticated = PortalPayloadHelpers.VerifyPortalPayload(payload, _appSettings.PortalAuthenticationKey);

      if (!isAuthenticated)
      {
        context.Result = new UnauthorizedResult();
        return;
      }
    }
  }
}
