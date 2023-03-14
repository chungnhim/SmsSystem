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
    public interface IUserProfileService : IServiceBase<UserProfile>
    {
    }
    public class UserProfileService : ServiceBase<UserProfile>, IUserProfileService
    {
        public UserProfileService(SmsDataContext smsDataContext) : base(smsDataContext)
        {
        }

        public override void Map(UserProfile entity, UserProfile model)
        {
            entity.Email = model.Email;
            entity.Name = model.Name;
            entity.PhoneNumber = model.PhoneNumber;
        }
        protected override async Task<string> ValidateEntry(UserProfile entity)
        {
            var dupplicatePhoneNumber = await _smsDataContext.UserProfiles.AnyAsync(r => r.PhoneNumber == entity.PhoneNumber && r.Id != entity.Id);
            if (dupplicatePhoneNumber)
            {
                return "DuplicatePhoneNumber";
            }
            var dupplicateCOM = await _smsDataContext.UserProfiles.AnyAsync(r => r.Email == entity.Email && r.Id != entity.Id);
            if (dupplicateCOM)
            {
                return "dupplicateEmail";
            }
            return await base.ValidateEntry(entity);
        }
    }
}
