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
    public interface IPhoneFailedNotificationService : IServiceBase<PhoneFailedNotification>
    {
        Task MaskAsRead(int id);
    }
    public class PhoneFailedNotificationService : ServiceBase<PhoneFailedNotification>, IPhoneFailedNotificationService
    {
        public PhoneFailedNotificationService(SmsDataContext smsDataContext) : base(smsDataContext)
        {
        }

        public override void Map(PhoneFailedNotification entity, PhoneFailedNotification model)
        {
        }

        public async Task MaskAsRead(int id)
        {

        }
    }
}
