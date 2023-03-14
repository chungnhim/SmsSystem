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
    public interface IUserTokenService : IServiceBase<UserToken>
    {
        Task<int> RemoveAllUserToken(int userId, Guid except);
    }
    public class UserTokenService : ServiceBase<UserToken>, IUserTokenService
    {
        public UserTokenService(SmsDataContext smsDataContext) : base(smsDataContext)
        {
        }

        public override void Map(UserToken entity, UserToken model)
        {
        }

        public async Task<int> RemoveAllUserToken(int userId, Guid except)
        {
            var tokens = await _smsDataContext.UserTokens.Where(x => x.UserId == userId && x.Token != except).ToListAsync();
            _smsDataContext.UserTokens.RemoveRange(tokens);

            return await _smsDataContext.SaveChangesAsync();
        }
    }
}
