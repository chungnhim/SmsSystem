using Microsoft.AspNetCore.Http;
using Sms.Web.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Sms.Web.Middleware
{
    public class UserTokenChecking
    {
        private readonly RequestDelegate _next;

        public UserTokenChecking(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IUserService userService)
        {
            var token = context.User.FindFirst(ClaimTypes.Hash)?.Value;
            if (!string.IsNullOrEmpty(token) && Guid.TryParse(token, out Guid tokenGuid) && ! (await userService.CheckToken(tokenGuid)))
            {
                context.User = null;
            }
            if (!string.IsNullOrEmpty(token) && Guid.TryParse(token, out Guid tokenGuid2) && await userService.IsBanned(tokenGuid2))
            {
                if (context.Request.Path.HasValue && !context.Request.Path.Value.ToLower().StartsWith("/api/auth/"))
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await context.Response.WriteAsync("\"Banned\"", System.Text.Encoding.UTF8);
                    return;
                }
            }
            await _next(context);
        }
    }
}
