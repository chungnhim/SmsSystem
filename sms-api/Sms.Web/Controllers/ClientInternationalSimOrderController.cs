using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sms.Web.Entity;
using Sms.Web.Models;
using Sms.Web.Service;

namespace Sms.Web.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  [Authorize(Roles = "User")]
  public class ClientInternationalSimOrderController : ControllerBase
  {
    private readonly IInternationalSimOrderService _internationalSimOrderService;
    private readonly IAuthService _authService;
    public ClientInternationalSimOrderController(IInternationalSimOrderService internationalSimOrderService,
      IAuthService authService)
    {
      _internationalSimOrderService = internationalSimOrderService;
      _authService = authService;
    }
    [HttpPost("request-order")]
    public async Task<ApiResponseBaseModel> RequestAOrder([FromBody]RequestInternaltionSimOrderRequest request)
    {
      var id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
      if (request.MaximumSms.HasValue)
      {
        request.MaximumSms = Math.Max(Math.Min(5, request.MaximumSms.Value), 1);
      }

      return await _internationalSimOrderService.RequestAOrder(id, Helpers.AppSourceType.Web, request);
    }
    [HttpPost("{orderId}/close-order")]
    public async Task<ApiResponseBaseModel> CloseOrder(int orderId)
    {
      var checkOrder = await _internationalSimOrderService.CheckOrderIsAvailableForUser(orderId);
      if (!checkOrder)
      {
        return new ApiResponseBaseModel()
        {
          Success = false,
          Message = "Unauthorized"
        };
      }
      return await _internationalSimOrderService.CloseOrder(orderId);
    }
    [HttpPost("my-orders")]
    public async Task<FilterResponse<InternationalSimOrder>> MyOrders([FromBody]FilterRequest pagingRequest)
    {
      var userId = _authService.CurrentUserId();
      if (!userId.HasValue) return new FilterResponse<InternationalSimOrder>();
      pagingRequest.SearchObject = pagingRequest.SearchObject ?? new Dictionary<string, object>();
      pagingRequest.SearchObject.Add("UserId", userId.GetValueOrDefault());
      return await _internationalSimOrderService.Paging(pagingRequest);
    }
  }
}
