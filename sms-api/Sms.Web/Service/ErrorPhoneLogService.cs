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
    public interface IErrorPhoneLogService : IServiceBase<ErrorPhoneLog>
    {
        Task<ApiResponseBaseModel> ReopenError(int id);
    }
    public class ErrorPhoneLogService : ServiceBase<ErrorPhoneLog>, IErrorPhoneLogService
    {
        public ErrorPhoneLogService(SmsDataContext smsDataContext) : base(smsDataContext)
        {
        }

        public override void Map(ErrorPhoneLog entity, ErrorPhoneLog model)
        {
        }

        public async Task<ApiResponseBaseModel> ReopenError(int id)
        {
            var errorUsers = await _smsDataContext.ErrorPhoneLogUsers.Where(r => r.ErrorPhoneLogId == id).ToListAsync();
            foreach (var errorUser in errorUsers)
            {
                errorUser.IsIgnored = true;
            }
            var errorPhone = await _smsDataContext.ErrorPhoneLogs.FirstOrDefaultAsync(r => r.Id == id);
            if (errorPhone == null) return ApiResponseBaseModel.NotFoundResourceResponse();
            errorPhone.IsActive = false;
            await _smsDataContext.SaveChangesAsync();
            return new ApiResponseBaseModel();
        }

        public override async Task<FilterResponse<ErrorPhoneLog>> Paging(FilterRequest filterRequest)
        {
            var pageSize = Math.Min(1000, filterRequest.PageSize);
            if (pageSize == 0) pageSize = 20;
            var query = from epl in _smsDataContext.ErrorPhoneLogs.Include("ErrorPhoneLogOrders.User")
                        join com in _smsDataContext.Coms on epl.PhoneNumber equals com.PhoneNumber
                        where epl.IsActive == true
                        select new { epl, com };
            {
                if (filterRequest.SearchObject.TryGetValue("GsmIds", out object obj))
                {
                    var gsmIds = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<int?>>();

                    if (gsmIds != null && gsmIds.Count > 0)
                    {
                        query = query.Where(r => gsmIds.Contains(r.com.GsmDeviceId));
                    }
                }
            }
            {
                if (filterRequest.SearchObject.TryGetValue("phoneNumber", out object obj))
                {
                    var phoneNumber = obj.ToString();
                    if (!string.IsNullOrWhiteSpace(phoneNumber))
                    {
                        phoneNumber = phoneNumber.ToLower();
                        query = query.Where(r => r.epl.PhoneNumber.Contains(phoneNumber));
                    }
                }
            }

            {
                if (filterRequest.SearchObject.TryGetValue("serviceProviderIds", out object obj))
                {
                    var ts = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<int>>();
                    if (ts.Count > 0)
                    {
                        query = query.Where(r => ts.Contains(r.epl.ServiceProviderId));
                    }
                }
            }
            var count = await query.CountAsync();
            var list = await query.Skip(filterRequest.PageIndex * pageSize).Take(pageSize).ToListAsync();
            return new FilterResponse<ErrorPhoneLog>()
            {
                Total = count,
                Results = list.Select(r =>
                {
                    r.epl.GsmDeviceId = r.com.GsmDeviceId;
                    return r.epl;
                }).ToList()
            };
        }
    }
}
