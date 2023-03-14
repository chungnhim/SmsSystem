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
    public interface IUserPaymentTransactionService : IServiceBase<UserPaymentTransaction>
    {
        Task<UserPaymentTransaction> GetByRequestId(string requestId);
    }
    public class UserPaymentTransactionService : ServiceBase<UserPaymentTransaction>, IUserPaymentTransactionService
    {
        public UserPaymentTransactionService(SmsDataContext smsDataContext) : base(smsDataContext)
        {
        }

        public async Task<UserPaymentTransaction> GetByRequestId(string requestId)
        {
            return await GenerateQuery().FirstOrDefaultAsync(r => r.RequestId == requestId);
        }

        public override void Map(UserPaymentTransaction entity, UserPaymentTransaction model)
        {
            entity.IsExpired = model.IsExpired;
        }
    }
}
