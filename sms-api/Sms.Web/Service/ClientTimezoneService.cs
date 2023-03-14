using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Sms.Web.Service
{
    public interface IClientTimezoneService
    {
        int? ClientTimezone();
    }
    public class ClientTimezoneService : IClientTimezoneService
    {
        private readonly IHttpContextAccessor _context;
        public ClientTimezoneService(IHttpContextAccessor context)
        {
            _context = context;
        }

        public int? ClientTimezone()
        {
            if (this._context == null || _context.HttpContext == null) return null;
            if (_context.HttpContext.Request.Headers.TryGetValue("timezone",out StringValues headerValue))
            {
                if (int.TryParse(headerValue, out int result))
                {
                    return result;
                }
            }
            return null;
        }
    }
}
