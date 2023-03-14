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
    public interface IUserReferredFeeService : IServiceBase<UserReferredFee>
    {
    }
    public class UserReferredFeeService : ServiceBase<UserReferredFee>, IUserReferredFeeService
    {
        public UserReferredFeeService(SmsDataContext smsDataContext) : base(smsDataContext)
        {
        }

        public override void Map(UserReferredFee entity, UserReferredFee model)
        {
        }
        protected override IQueryable<UserReferredFee> GenerateQuery(FilterRequest filterRequest = null)
        {
            var query = base.GenerateQuery(filterRequest);
            query = query.OrderByDescending(r => r.ReportTime);
            query = query.Include(r => r.User);
            if (filterRequest != null)
            {
                {
                    if (filterRequest.SearchObject.TryGetValue("UserId", out object obj))
                    {
                        var userId = int.Parse(obj.ToString());
                        query = query.Where(r => r.UserId == userId);
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("startDate", out object obj))
                    {
                        var date = (DateTime)obj;
                        query = query.Where(r => r.ReportTime >= date);
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("endDate", out object obj))
                    {
                        var date = ((DateTime)obj).AddDays(1);
                        query = query.Where(r => r.ReportTime < date);
                    }
                }
            }

            return query;
        }
    }
}
