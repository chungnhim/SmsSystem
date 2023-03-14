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
    public interface ISmsHistoryService : IServiceBase<SmsHistory>
    {
        Task<int> CleanUpSmsHistory(DateTime? fromDate, DateTime? toDate);
    Task<RentCodeOrder> GetOrderOfSmsHistory(int smsHistoryId);
        Task MarkSmsHistoryNotMatch(int id);
        Task<ApiResponseBaseModel<int?>> CheckSmsMatchWithService(int id, int serviceProviderId);
        Task<ApiResponseBaseModel<int?>> RematchWithService(int id);
    }
    public class SmsHistoryService : ServiceBase<SmsHistory>, ISmsHistoryService
    {
        private readonly IAuthService _authService;
        private readonly IServiceProviderService _serviceProviderService;
        private readonly IOrderService _orderService;
        public SmsHistoryService(SmsDataContext smsDataContext, IAuthService authService, IOrderService orderService, IServiceProviderService serviceProviderService) : base(smsDataContext)
        {
            _authService = authService;
            _orderService = orderService;
            _serviceProviderService = serviceProviderService;
        }

        public async Task<ApiResponseBaseModel<int?>> CheckSmsMatchWithService(int id, int serviceProviderId)
        {
            var history = await Get(id);
            return await _orderService.CheckMessageIsMatchWithService(await _serviceProviderService.Get(serviceProviderId), history.Content, history.Sender);
        }

        public async Task<int> CleanUpSmsHistory(DateTime? fromDate, DateTime? toDate)
        {
            var query = _smsDataContext.Set<SmsHistory>().AsQueryable();
            if (fromDate.HasValue)
            {
                query = query.Where(r => r.ReceivedDate >= fromDate);
            }
            if (toDate.HasValue)
            {
                toDate = toDate.Value.AddDays(1);
                query = query.Where(r => r.ReceivedDate < toDate);
            }
            _smsDataContext.SmsHistorys.RemoveRange(query);
            return await _smsDataContext.SaveChangesAsync();
        }

    public async Task<RentCodeOrder> GetOrderOfSmsHistory(int smsHistoryId)
        {
      var orderId = await _smsDataContext.OrderResults.Where(r => r.SmsHistoryId == smsHistoryId).Select(r => r.OrderId).FirstOrDefaultAsync();
      var order = await _smsDataContext.RentCodeOrders.Include(r => r.ServiceProvider).Where(r => r.Id == orderId).FirstOrDefaultAsync();
      return order;
        }

        public override void Map(SmsHistory entity, SmsHistory model)
        {
            entity.Content = model.Content;
            entity.PhoneNumber = model.PhoneNumber;
            entity.Sender = model.Sender;
        }

        public async Task MarkSmsHistoryNotMatch(int id)
        {
            var history = await Get(id);
            var phoneNumber = history.PhoneNumber;
      var availableOrder = await _smsDataContext.RentCodeOrders.Include(r => r.ServiceProvider)
                .Where(r => r.PhoneNumber == phoneNumber && r.Status == OrderStatus.Waiting).FirstOrDefaultAsync();
            if (availableOrder == null) return;
            history.NotMappedOrderId = availableOrder.Id;
            history.PermanentServiceType = availableOrder.ServiceProvider.ServiceType;
            history.PermanentMessageRegex = availableOrder.ServiceProvider.MessageRegex;
            await _smsDataContext.SaveChangesAsync();
        }

        public async Task<ApiResponseBaseModel<int?>> RematchWithService(int id)
        {
      var history = await _smsDataContext.SmsHistorys.FirstOrDefaultAsync(r => r.Id == id);

      if (history == null) return new ApiResponseBaseModel<int?>()
      {
        Success = false,
        Message = "FailCheck"
      };

      var order = await _smsDataContext.RentCodeOrders.Include(r => r.ServiceProvider).FirstOrDefaultAsync(r => r.Id == history.NotMappedOrderId);
      if (order == null || order.ServiceProvider == null) return new ApiResponseBaseModel<int?>()
            {
                Success = false,
                Message = "FailCheck"
            };
      return await _orderService.CheckMessageIsMatchWithService(order.ServiceProvider, history.Content, history.Sender);
        }

        protected override IQueryable<SmsHistory> GenerateQuery(FilterRequest filterRequest = null)
        {
            var query = from s in _smsDataContext.SmsHistorys
                        select s;
            query = query.OrderByDescending(s => s.Created);

            var userId = _authService.CurrentUserId();
            var user = _smsDataContext.Users.FirstOrDefault(r => r.Id == userId);
            if (user.Role == RoleType.Staff)
            {
                query = from sms in query
                        join com in _smsDataContext.Coms on sms.PhoneNumber equals com.PhoneNumber
                        where com.GsmDevice.UserGsmDevices.Any(r => r.UserId == userId)
                        select sms;
            }

            if (filterRequest != null)
            {
                {
                    if (filterRequest.SearchObject.TryGetValue("searchContent", out object obj))
                    {
                        var searchString = (string)obj;
                        if (!string.IsNullOrEmpty(searchString))
                        {
                            query = query.Where(r => r.Content.Contains((string)obj));
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("serchSender", out object obj))
                    {
                        query = query.Where(r => r.Sender.Contains((string)obj));
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("searchPhone", out object obj))
                    {
                        query = query.Where(r => r.PhoneNumber.Contains((string)obj));
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
                    }
                }
                if (fromDate != null)
                {
                    query = query.Where(r => r.ReceivedDate >= fromDate);
                }
                if (toDate != null)
                {
                    query = query.Where(r => r.ReceivedDate < toDate);
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("onlyNotMatched", out object obj))
                    {
                        bool onlyNotMatched = (bool)obj;
                        if (onlyNotMatched)
                        {
                            query = query.Where(r => r.NotMappedOrderId != null);
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("serviceProviderIds", out object obj))
                    {
                        var ts = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<int>>();
                        if (ts.Count > 0)
                        {
                            query = query.Where(r => r.Order.OrderType == OrderType.RentCode && ts.Contains((r.Order as RentCodeOrder).ServiceProviderId));
                        }
                    }
                }
            }

            return query;
        }


    }
}
