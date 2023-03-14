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
    public interface IProviderHistoryService : IServiceBase<ProviderHistory>
    {
        Task<List<ProviderHistory>> GetAllHistoriesByPhoneNumbers(List<string> phoneNumbers);
    }
    public class ProviderHistoryService : ServiceBase<ProviderHistory>, IProviderHistoryService
    {
        public ProviderHistoryService(SmsDataContext smsDataContext) : base(smsDataContext)
        {
        }

        public async Task<List<ProviderHistory>> GetAllHistoriesByPhoneNumbers(List<string> phoneNumbers)
        {
            return await GenerateQuery().Where(r => phoneNumbers.Contains(r.PhoneNumber)).ToListAsync();
        }

        public override void Map(ProviderHistory entity, ProviderHistory model)
        {
        }
    }
}
