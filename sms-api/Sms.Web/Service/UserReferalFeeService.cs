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
    public interface IUserReferalFeeService : IServiceBase<UserReferalFee>
    {
    }
    public class UserReferalFeeService : ServiceBase<UserReferalFee>, IUserReferalFeeService
    {
        public UserReferalFeeService(SmsDataContext smsDataContext) : base(smsDataContext)
        {
        }

        public override void Map(UserReferalFee entity, UserReferalFee model)
        {
        }
        public override async Task<Dictionary<string, object>> GetFilterAdditionalData(FilterRequest filterRequest, IQueryable<UserReferalFee> query)
        {
            return  new Dictionary<string, object>()
            {
                {"TotalCost", await query.SumAsync(r=>r.TotalCost) },
                {"TotalFeeAmount", await query.SumAsync(r=>r.FeeAmount) }
            };
        }

        protected override IQueryable<UserReferalFee> GenerateQuery(FilterRequest filterRequest = null)
        {
            var query = base.GenerateQuery(filterRequest);
            query = query.OrderByDescending(r => r.ReportTime);
            query = query.Include(r => r.User).Include(r => r.ReferredUser);
            if (filterRequest != null)
            {
                var sortColumn = (filterRequest.SortColumnName ?? string.Empty).ToLower();
                var isAsc = filterRequest.IsAsc;

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
                {
                    if (filterRequest.SearchObject.TryGetValue("user", out object obj) || filterRequest.SearchObject.TryGetValue("referredUser", out obj))
                    {
                        var username = obj.ToString();
                        if (!string.IsNullOrWhiteSpace(username))
                        {
                            username = username.ToLower();

                            if (username.StartsWith("[") && username.EndsWith("]"))
                            {
                                username = username.Substring(1, username.Length - 2);
                                query = query.Where(r => r.ReferredUser != null && r.ReferredUser.Username.ToLower() == username);
                            }
                            else
                            {
                                query = query.Where(r => r.ReferredUser != null && r.ReferredUser.Username.ToLower().Contains(username));
                            }
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("referalUser", out object obj))
                    {
                        var username = obj.ToString();
                        if (!string.IsNullOrWhiteSpace(username))
                        {
                            username = username.ToLower();

                            if (username.StartsWith("[") && username.EndsWith("]"))
                            {
                                username = username.Substring(1, username.Length - 2);
                                query = query.Where(r => r.User.Username == username);
                            }
                            else
                            {
                                query = query.Where(r => r.User.Username.Contains(username));
                            }
                        }
                    }
                }
                switch (sortColumn)
                {
                    case "feeamount":
                        query = isAsc ? query.OrderBy(x => x.FeeAmount) : query.OrderByDescending(x => x.FeeAmount);
                        break;
                    case "totalordercount":
                        query = isAsc ? query.OrderBy(x => x.TotalOrderCount) : query.OrderByDescending(x => x.TotalOrderCount);
                        break;
                    case "reporttime":
                        query = isAsc ? query.OrderBy(x => x.ReportTime) : query.OrderByDescending(x => x.ReportTime);
                        break;
                    case "totalcost":
                        query = isAsc ? query.OrderBy(x => x.TotalCost) : query.OrderByDescending(x => x.TotalCost);
                        break;
                    default:
                        break;
                }
            }

            return query;
        }
       
    }
}
