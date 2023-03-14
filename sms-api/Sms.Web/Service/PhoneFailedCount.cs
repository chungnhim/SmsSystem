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
    public interface IPhoneFailedCountService : IServiceBase<PhoneFailedCount>
    {
        Task ResetContinuousFailedOfPhoneNumber(string phoneNumber);
    }
    public class PhoneFailedCountService : ServiceBase<PhoneFailedCount>, IPhoneFailedCountService
    {
        public PhoneFailedCountService(SmsDataContext smsDataContext) : base(smsDataContext)
        {
        }

        public override void Map(PhoneFailedCount entity, PhoneFailedCount model)
        {
        }

        public async Task ResetContinuousFailedOfPhoneNumber(string phoneNumber)
        {
            var com = await _smsDataContext.Coms.FirstOrDefaultAsync(r => r.PhoneNumber == phoneNumber);
            if (com == null) return;
            var failed = await _smsDataContext.PhoneFailedCounts.FirstOrDefaultAsync(r => r.GsmDeviceId == com.GsmDeviceId);
            if (failed != null && failed.ContinuousFailed != 0)
            {
                failed.ContinuousFailed = 0;
                await _smsDataContext.SaveChangesAsync();
            }
        }
    }
}
