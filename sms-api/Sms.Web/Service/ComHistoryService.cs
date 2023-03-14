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
    public interface IComHistoryService : IServiceBase<ComHistory>
    {
    }
    public class ComHistoryService : ServiceBase<ComHistory>, IComHistoryService
    {
        private readonly IAuthService _authService;
        public ComHistoryService(SmsDataContext smsDataContext,
            IAuthService authService) : base(smsDataContext)
        {
            _authService = authService;
        }
        public override void Map(ComHistory entity, ComHistory model)
        {
        }

        protected override IQueryable<ComHistory> GenerateQuery(FilterRequest filterRequest)
        {
            var query = base.GenerateQuery(filterRequest);
            query = query.Include(x => x.GsmDevice);

            var userId = _authService.CurrentUserId();
            var user = _smsDataContext.Users.FirstOrDefault(r => r.Id == userId);
            if (user.Role == RoleType.Staff)
            {
                query = query.Where(r => r.GsmDevice.UserGsmDevices.Any(k => k.UserId == userId));
            }
            if (filterRequest != null)
            {
                var sortColumn = filterRequest.SortColumnName ?? string.Empty;
                var isAsc = filterRequest.IsAsc;
                var gsmDeviceIds = new List<int>();
                {
                    if (filterRequest.SearchObject.TryGetValue("gsmDevice", out object obj) == true)
                    {
                        gsmDeviceIds = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<int>>();
                    }
                }
                if (gsmDeviceIds.Count > 0)
                {
                    query = query.Where(x => gsmDeviceIds.Contains(x.GsmDevice.Id));
                }
                DateTime? fromDate = null;
                DateTime? toDate = null;
                {
                    if (filterRequest.SearchObject.TryGetValue("startDate", out object obj))
                    {
                        fromDate = (DateTime)obj;
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("endDate", out object obj))
                    {
                        toDate = (DateTime)obj;
                        toDate = toDate.Value.AddDays(1).AddSeconds(-1);
                    }
                }
                if (fromDate != null)
                {
                    query = query.Where(r => r.Created >= fromDate);
                }
                if (toDate != null)
                {
                    query = query.Where(r => r.Created < toDate);
                }
                switch (sortColumn)
                {
                    case "name":
                        query = isAsc ? query.OrderBy(x => x.GsmDevice.Name) : query.OrderByDescending(x => x.GsmDevice.Name);
                        break;
                    case "code":
                        query = isAsc ? query.OrderBy(x => x.ComName) : query.OrderByDescending(x => x.ComName);
                        break;
                    case "oldPhoneNumber":
                        query = isAsc ? query.OrderBy(x => x.OldPhoneNumber) : query.OrderByDescending(x => x.OldPhoneNumber);
                        break;
                    case "newPhoneNumber":
                        query = isAsc ? query.OrderBy(x => x.NewPhoneNumber) : query.OrderByDescending(x => x.NewPhoneNumber);
                        break;
                    case "created":
                        query = isAsc ? query.OrderBy(x => x.Created) : query.OrderByDescending(x => x.Created);
                        break;
                    default:
                        break;
                }
            }
            return query;
        }

        public override async Task<Dictionary<string, object>> GetFilterAdditionalData(FilterRequest filterRequest, IQueryable<ComHistory> query)
        {
            var count = await (from c in query
                               where c.OldPhoneNumber != null && c.NewPhoneNumber != null
                               select 1).CountAsync();

            return new Dictionary<string, object>()
            {
                {"TotalCount", count }
            };
        }

    }
}