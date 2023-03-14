using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Sms.Web.Entity;
using Sms.Web.Models;
using Sms.Web.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Controllers
{
    [AllowAnonymous]
    [Route("api/v2")]
    [ApiController]
    public class ExternalGatewayController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ISmsService _smsService;
        private readonly IPhoneFailedCountService _phoneFailedCountService;
        private readonly IGsmDeviceService _gsmDeviceService;
        private readonly IComService _comService;
        private readonly IServiceProviderService _serviceProviderService;
        private readonly IOrderService _orderService;
        private readonly IOrderStatusService _orderStatusService;
        private readonly IOrderResultService _orderResultService;
        private readonly IUserOfflinePaymentReceiptService _userOfflinePaymentReceiptService;
        private readonly IMemoryCache _cache;
        private readonly ISmsHistoryService _smsHistoryService;


        public ExternalGatewayController(IUserService userService,
            ISmsService smsService,
            IPhoneFailedCountService phoneFailedCountService,
            IGsmDeviceService gsmDeviceService,
            IComService comService,
            IServiceProviderService serviceProviderService,
            IOrderService orderService,
            IOrderStatusService orderStatusService,
            IOrderResultService orderResultService,
            IUserOfflinePaymentReceiptService userOfflinePaymentReceiptService,
            IMemoryCache cache,
            ISmsHistoryService smsHistoryService)
        {
            _orderService = orderService;
            _userService = userService;
            _smsService = smsService;
            _phoneFailedCountService = phoneFailedCountService;
            _gsmDeviceService = gsmDeviceService;
            _comService = comService;
            _serviceProviderService = serviceProviderService;
            _orderStatusService = orderStatusService;
            _orderResultService = orderResultService;
            _userOfflinePaymentReceiptService = userOfflinePaymentReceiptService;
            _cache = cache;
            _smsHistoryService = smsHistoryService;
        }
        [HttpGet]
        [Route("balance")]
        public async Task<ApiResponseBaseModel<CheckBalanceResponse>> CheckBalance(string apiKey)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return new ApiResponseBaseModel<CheckBalanceResponse>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            var user = await _userService.GetUser(userId);
            if (user == null) return new ApiResponseBaseModel<CheckBalanceResponse>()
            {
                Success = false,
                Message = "NotFound"
            };
            return new ApiResponseBaseModel<CheckBalanceResponse>()
            {
                Success = true,
                Results = new CheckBalanceResponse()
                {

                    Balance = user.Ballance
                }
            };
        }
        [HttpGet]
        [Route("available-services")]
        public async Task<ApiResponseBaseModel<List<ServiceProvider>>> AvailableServices(string apiKey)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return new ApiResponseBaseModel<List<ServiceProvider>>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            var user = await _userService.GetUser(userId);
            if (user == null) return new ApiResponseBaseModel<List<ServiceProvider>>()
            {
                Success = false,
                Message = "NotFound"
            };
            return new ApiResponseBaseModel<List<ServiceProvider>>()
            {
                Success = true,
                Results = await _serviceProviderService.GetAllAvailableServices(null)
            };
        }

        [HttpGet("order/request")]
        public async Task<CreateOrderResponse> RequestAOrder(string apiKey, [FromQuery]RequestOrderRequest request)
        {
            if (request.MaximumSms.HasValue)
            {
                request.MaximumSms = Math.Max(Math.Min(5, request.MaximumSms.Value), 1);
            }

            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return new CreateOrderResponse()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            return new CreateOrderResponse(await _orderService.RequestAOrder(userId, request.ServiceProviderId, request.NetworkProvider, request.MaximumSms, Helpers.AppSourceType.Api, request.OnlyAcceptFreshOtp.GetValueOrDefault(), request.AllowVoiceSms));
        }
        [HttpGet("order/request-holding")]
        public async Task<CreateOrderResponse> RequestAHoldingSimOrder(string apiKey, [FromQuery]RequestHoldingSim request)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return new CreateOrderResponse()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            return new CreateOrderResponse(await _orderService.RequestThirdOrder(userId, request.Duration, request.Unit, request.NetworkProvider, Helpers.AppSourceType.Api));
        }
        [HttpGet("order/request-reuse")]
        public async Task<CreateOrderResponse> RequestACallbackSimOrder(string apiKey, [FromQuery]RequestSimCallback request)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return new CreateOrderResponse()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            return new CreateOrderResponse(await _orderService.RequestOrderCallback(userId, request.RequestPhoneNumber, Helpers.AppSourceType.Api));
        }
        [HttpGet("order/{orderId}/check")]
        public async Task<CheckOrderResults> CheckOrderStatus(string apiKey, int orderId)
        {
            return await _cache.GetOrCreateAsync($"OrderCheck_{orderId}", async entry =>
            {
                entry.SetAbsoluteExpiration(TimeSpan.FromSeconds(3));
                var userId = await _userService.GetUserIdFromApiKey(apiKey);
                if (userId == 0)
                {
                    return new CheckOrderResults()
                    {
                        Success = false,
                        Message = "Unauthorized"
                    };
                }
                var available = await _orderService.CheckOrderIsAvailableForUser(orderId, userId);
                if (!available)
                {
                    return new CheckOrderResults()
                    {
                        Success = false,
                        Message = "NotFound"
                    };
                }
                return await _orderStatusService.GetOrderResults(orderId);
            });
        }
        [HttpGet("order/{orderId}/cancel")]
        public async Task<ApiResponseBaseModel> CloseOrder(string apiKey, int orderId)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return new ApiResponseBaseModel()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
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
        [HttpGet("history-phone-numbers")]
        public async Task<ApiResponseBaseModel<List<string>>> HistoryPhoneNumbers(string apiKey)
        {

            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return new ApiResponseBaseModel<List<string>>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            return await _orderService.GetAllUserHistoryPhoneNumbers();
        }
    }
}
