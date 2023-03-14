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
    public interface IOfflinePaymentNoticeService : IServiceBase<OfflinePaymentNotice>
    {
    }
    public class OfflinePaymentNoticeService : ServiceBase<OfflinePaymentNotice>, IOfflinePaymentNoticeService
    {
        public OfflinePaymentNoticeService(SmsDataContext smsDataContext) : base(smsDataContext)
        {
        }

        public override void Map(OfflinePaymentNotice entity, OfflinePaymentNotice model)
        {
        }
        protected override IQueryable<OfflinePaymentNotice> GenerateQuery(FilterRequest filterRequest = null)
        {
            var query = base.GenerateQuery(filterRequest);
            query = query.Include("Receipt.User");

            if (filterRequest != null)
            {
                {
                    if (filterRequest.SearchObject.TryGetValue("searchReceiptCode", out object obj))
                    {
                        var c = ((string)obj).ToLower();
                        if (!string.IsNullOrEmpty(c))
                        {
                            query = query.Where(r => r.ReceiptCode.ToLower().Contains(c));
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("user", out object obj))
                    {
                        var c = ((string)obj).ToLower();
                        if (!string.IsNullOrEmpty(c))
                        {
                            query = query.Where(r => r.Receipt != null && r.Receipt.User != null && r.Receipt.User.Username.ToLower().Contains(c));
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("searchServiceProvider", out object obj) == true)
                    {
                        var serviceProviderIds = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<string>>();
                        if (serviceProviderIds.Count > 0)
                        {
                            query = query.Where(r => serviceProviderIds.Contains(r.ServiceProvider));
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("searchStatus", out object obj))
                    {
                        var ts = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<OfflinePaymentNoticeErrorReason>>();
                        if (ts.Count > 0)
                        {
                            query = query.Where(r => ts.Contains(r.ErrorReason));
                        }
                    }
                }
                DateTime? fromDate = null;
                DateTime? toDate = null;
                {
                    if (filterRequest.SearchObject.TryGetValue("searchStartDate", out object obj))
                    {
                        fromDate = (DateTime)obj;
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("searchEndDate", out object obj))
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
            }
            return query;
        }

        public override async Task<Dictionary<string, object>> GetFilterAdditionalData(FilterRequest filterRequest, IQueryable<OfflinePaymentNotice> query)
        {
            var dic = new Dictionary<string, object>();
            var totalAmount = await query.SumAsync(r => r.Amount);
            dic.Add("totalAmount", totalAmount);
            return dic;
        }
    }
}
