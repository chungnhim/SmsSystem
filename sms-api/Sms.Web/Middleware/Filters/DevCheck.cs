using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Sms.Web.Helpers;

namespace Sms.Web.Middleware.Filters
{
    public class DevelopmentOnly: ActionFilterAttribute
    {
        private readonly AppSettings _appSettings;
        public DevelopmentOnly(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!_appSettings.IsDevelopment.GetValueOrDefault())
            {
                context.Result = new BadRequestObjectResult("Development only");
            }
        }

    }
}
