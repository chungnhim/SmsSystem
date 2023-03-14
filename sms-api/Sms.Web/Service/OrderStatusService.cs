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
  public interface IOrderStatusService
  {

    Task<ApiResponseBaseModel<CheckOrdersStatusResponse>> CheckOrdersStatus(CheckOrdersStatusRequest request);
    Task<CheckOrderResults> GetOrderResults(int orderId);
  }
  public class OrderStatusService : IOrderStatusService
  {
    private readonly SmsDataContext _smsDataContext;
    private readonly IAuthService _authService;
    private readonly ICacheService _cacheService;
    private readonly IDateTimeService _dateTimeService;
    public OrderStatusService(SmsDataContext smsDataContext, IAuthService authService,
        ICacheService cacheService,
        IDateTimeService dateTimeService)
    {
      _smsDataContext = smsDataContext;
      _authService = authService;
      _cacheService = cacheService;
      _dateTimeService = dateTimeService;
    }

    public async Task<ApiResponseBaseModel<CheckOrdersStatusResponse>> CheckOrdersStatus(CheckOrdersStatusRequest request)
    {
      var userId = _authService.CurrentUserId();
      if (userId == null) return new ApiResponseBaseModel<CheckOrdersStatusResponse>()
      {
        Success = false,
        Message = "Unauthorized"
      };
      var orderIds = request.OrderIds.Take(100).ToList();
      var orders = await (from o in _smsDataContext.Orders
                          where orderIds.Contains(o.Id)
                          select new { o.Id, o.Status }).ToListAsync();
      var resultsCounts = await (from r in _smsDataContext.OrderResults
                                 where orderIds.Contains(r.OrderId)
                                 group r by r.OrderId into k
                                 select new { Count = k.Count(), OrderId = k.Key }).ToListAsync();
      return new ApiResponseBaseModel<CheckOrdersStatusResponse>()
      {
        Success = true,
        Results = new CheckOrdersStatusResponse()
        {
          Statuses = (from o in orders
                      join r in resultsCounts on o.Id equals r.OrderId into gr
                      from rr in gr.DefaultIfEmpty()
                      select new OrderStatusWithResultCount()
                      {
                        OrderId = o.Id,
                        OrderStatus = o.Status,
                        ResultsCount = (rr?.Count).GetValueOrDefault()
                      }).ToList()
        }
      };
    }

    public async Task<CheckOrderResults> GetOrderResults(int orderId)
    {
      var order = await _smsDataContext.RentCodeOrders.Include(r => r.OrderResults).FirstOrDefaultAsync(r => r.Id == orderId);
      if (order == null) return new CheckOrderResults()
      {
        Success = false,
        Message = "NotFound"
      };
      var gsm512IdList = await _cacheService.Gsm512IdList();
      var needHide = order.Status == OrderStatus.Waiting
          && order.Updated > _dateTimeService.UtcNow().AddSeconds(-16)
          && order.ConnectedGsmId.HasValue
          && gsm512IdList.Contains(order.ConnectedGsmId.Value);

      return new CheckOrderResults()
      {
        Success = true,
        PhoneNumber = needHide ? "" : order.PhoneNumber,
        Message = order.OrderResults.LastOrDefault()?.Message,
        Messages = order.OrderResults.Select(r => new SmsMessage()
        {
          Message = r.Message,
          AudioUrl = r.AudioUrl,
          MessageType = r.SmsType.ToString(),
          Sender = r.Sender,
          Time = r.Created
        }).ToList()
      };
    }
  }
}
