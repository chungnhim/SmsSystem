using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Sms.Web.Entity;
using Sms.Web.Helpers;
using Sms.Web.Models;
using Sms.Web.Service;

namespace Sms.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "User")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ClientOrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IOrderStatusService _orderStatusService;
        private readonly IAuthService _authService;
        private readonly IOrderComplaintService _orderComplaintService;
        private readonly IOrderResultService _orderResultService;
        private readonly IExportService _exportService;
        private readonly IMemoryCache _memoryCache;
        private readonly IOrderExportJobService _orderExportJobService;
        public ClientOrderController(IOrderService orderService, IAuthService authService, IOrderComplaintService orderComplaintService, IOrderStatusService orderStatusService, IOrderResultService orderResultService, IExportService exportService, IMemoryCache memoryCache, IOrderExportJobService orderExportJobService)
        {
            _orderService = orderService;
            _orderComplaintService = orderComplaintService;
            _authService = authService;
            _orderStatusService = orderStatusService;
            _orderResultService = orderResultService;
            _exportService = exportService;
            _memoryCache = memoryCache;
            _orderExportJobService = orderExportJobService;
        }
        [HttpPost("request-order")]
        public async Task<ApiResponseBaseModel> RequestAOrder([FromBody] RequestOrderRequest request)
        {
            var id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (request.MaximumSms.HasValue)
            {
                request.MaximumSms = Math.Max(Math.Min(5, request.MaximumSms.Value), 1);
            }

            return await _orderService.RequestAOrder(id, request.ServiceProviderId, request.NetworkProvider, request.MaximumSms, Helpers.AppSourceType.Web, request.OnlyAcceptFreshOtp.GetValueOrDefault(), request.AllowVoiceSms);
        }
        [HttpPost("request-3rd-order")]
        public async Task<ApiResponseBaseModel> RequestThirdOrder([FromBody] RequestHoldingSim request)
        {
            var id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            return await _orderService.RequestThirdOrder(id, request.Duration, request.Unit, request.NetworkProvider, Helpers.AppSourceType.Web);
        }

        [HttpPost("request-order-callback")]
        public async Task<ApiResponseBaseModel> RequestOrderCallback([FromBody] RequestSimCallback request)
        {
            var id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            return await _orderService.RequestOrderCallback(id, request.RequestPhoneNumber, Helpers.AppSourceType.Web);
        }
        [HttpPost("{orderId}/complain-order")]
        public async Task<ApiResponseBaseModel<OrderComplaint>> ComplainOrder(int orderId, [FromBody] OrderComplaintRequest request)
        {
            return await _orderComplaintService.Complain(orderId, request);
        }
        [HttpPost("{orderId}/order-results")]
        public async Task<FilterResponse<OrderResult>> OrderResults(int orderId, [FromBody] FilterRequest request)
        {
            var checkOrder = await _orderService.CheckOrderIsAvailableForUser(orderId);
            if (!checkOrder)
            {
                return new FilterResponse<OrderResult>()
                {
                    Results = new List<OrderResult>(),
                    Total = 0
                };
            }
            request.SearchObject.Add("OrderId", orderId);
            return await _orderResultService.Paging(request);
        }
        [HttpPost("{orderId}/close-order")]
        public async Task<ApiResponseBaseModel> CloseOrder(int orderId)
        {
            var checkOrder = await _orderService.CheckOrderIsAvailableForUser(orderId);
            if (!checkOrder)
            {
                return new ApiResponseBaseModel()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            return await _orderService.CloseOrder(orderId);
        }
        [HttpPost("my-orders")]
        public async Task<FilterResponse<RentCodeOrder>> MyOrders([FromBody] FilterRequest pagingRequest)
        {
            var userId = _authService.CurrentUserId();
            if (!userId.HasValue) return new FilterResponse<RentCodeOrder>();
            pagingRequest.SearchObject = pagingRequest.SearchObject ?? new Dictionary<string, object>();
            pagingRequest.SearchObject.Add("UserId", userId.GetValueOrDefault());
            return await _orderService.Paging(pagingRequest);
        }
        [HttpGet("my-history-phone-numbers")]
        public async Task<ApiResponseBaseModel<List<string>>> GetMyHistoryPhoneNumbers()
        {
            return await _orderService.GetAllUserHistoryPhoneNumbers();
        }

        [HttpPost("export-orders")]
        public async Task<ApiResponseBaseModel<OrderExportJob>> ExportOrders(ExportOrdersRequest request)
        {
            var keyCache = $"EXPORT_LIMIT_FLAG_USER_{_authService.CurrentUserId().GetValueOrDefault()}";
            if (_memoryCache.TryGetValue<int>(keyCache, out var b))
            {
                if (b > 5)
                {
                    return new ApiResponseBaseModel<OrderExportJob>()
                    {
                        Success = false,
                        Message = "LimitRate"
                    };
                }
            }
            b++;
            _memoryCache.Set(keyCache, b, TimeSpan.FromSeconds(60));
            OrderExportJob orderExportJobLast = await _orderExportJobService.GetOrderExportByStatus(OrderExportStatus.Waiting);
            if (orderExportJobLast != null)
            {
                return new ApiResponseBaseModel<OrderExportJob>()
                {
                    Success = false,
                    Message = "ProcessExport"
                };
            }
            OrderExportJob orderExportJob = new OrderExportJob()
            {
                UserId = _authService.CurrentUserId().GetValueOrDefault(),
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                Status = OrderExportStatus.Waiting,
            };
            return await _orderExportJobService.Create(orderExportJob);
            //return await _exportService.ExportOrders(request.FromDate, request.ToDate, request.ServiceType);
        }

        [HttpPost("cancel-export-orders")]
        public async Task<ApiResponseBaseModel<OrderExportJob>> CancelExportOrders()
        {
            //return await _exportService.ExportOrders(request.FromDate, request.ToDate, request.ServiceType);
            OrderExportJob orderExportJob = await _orderExportJobService.GetOrderExportByStatus(OrderExportStatus.Waiting);
            orderExportJob.Status = OrderExportStatus.Cancelled;
            return await _orderExportJobService.Update(orderExportJob);
        }

        [HttpPost("export-orders-success")]
        public async Task<ApiResponseBaseModel<OrderExportJob>> ExportOrdersSuccess()
        {
            return new ApiResponseBaseModel<OrderExportJob>()
            {
                Results = await _orderExportJobService.GetOrderExportByStatus(OrderExportStatus.Success)
            };
        }
    }
}
