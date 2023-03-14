using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Sms.Web.Service
{
    public interface IAuthService
    {
        int? CurrentUserId();
        void SetCurrentUserId(int userId);
    }
    public class AuthService : IAuthService
    {
        private int? currentUserId;
        private readonly IHttpContextAccessor _context;
        public AuthService(IHttpContextAccessor context)
        {
            _context = context;
        }

        public int? CurrentUserId()
        {
            if (currentUserId.HasValue) return currentUserId;
            if (this._context == null || _context.HttpContext == null) return null;
            var user = this._context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(user) && int.TryParse(user, out int userId))
            {
                currentUserId = userId;
                return userId;
            }
            return null;
        }

        public void SetCurrentUserId(int userId)
        {
            currentUserId = userId;
        }
    }
}
