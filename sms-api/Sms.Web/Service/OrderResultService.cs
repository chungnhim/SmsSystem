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
    public interface IOrderResultService : IServiceBase<OrderResult>
    {
        Task<int> CountOrderResult(int orderId);
    }
    public class OrderResultService : ServiceBase<OrderResult>, IOrderResultService
    {
        public OrderResultService(SmsDataContext smsDataContext) : base(smsDataContext)
        {
        }
        public async Task<int> CountOrderResult(int orderId) => await _smsDataContext.OrderResults.CountAsync(r => r.OrderId == orderId);

        public override void Map(OrderResult entity, OrderResult model)
        {
        }
        protected override IQueryable<OrderResult> GenerateQuery(FilterRequest filterRequest = null)
        {
            var query = base.GenerateQuery(filterRequest);
            if (filterRequest != null)
            {
                int orderId = 0;
                if (filterRequest.SearchObject.TryGetValue("OrderId", out object orderIdObj))
                {
                    orderId = int.Parse(orderIdObj.ToString());
                }
                if (orderId != 0)
                {
                    query = query.Where(r => r.OrderId == orderId);
                }
            }
            return query;
        }
    }
}
