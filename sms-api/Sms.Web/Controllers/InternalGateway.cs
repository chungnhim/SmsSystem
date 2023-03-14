using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Sms.Web.Entity;
using Sms.Web.Helpers;
using Sms.Web.Models;
using Sms.Web.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Controllers
{
    [AllowAnonymous]
    [Route("api/ig")]
    [ApiController]
    public class InternalGatewayController : ControllerBase
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
        private readonly ILogger _logger;
        private readonly ICacheService _cacheService;
        private readonly IDateTimeService _dateTimeService;
        private readonly AsyncLocker _asyncLocker;
        private readonly IServiceProviderPhoneNumberLiveCheckService _serviceProviderPhoneNumberLiveCheckService;
        private readonly IComHistoryService _comHistoryService;

        public InternalGatewayController(IUserService userService,
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
            ISmsHistoryService smsHistoryService,
            ILogger<InternalGatewayController> logger,
            ICacheService cacheService,
            IDateTimeService dateTimeService,
            AsyncLocker asyncLocker,
            IServiceProviderPhoneNumberLiveCheckService serviceProviderPhoneNumberLiveCheckService,
            IComHistoryService comHistoryService)
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
            _logger = logger;
            _cacheService = cacheService;
            _dateTimeService = dateTimeService;
            _asyncLocker = asyncLocker;
            _serviceProviderPhoneNumberLiveCheckService = serviceProviderPhoneNumberLiveCheckService;
            _comHistoryService = comHistoryService;
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
        [HttpPost("create-request")]
        public async Task<ApiResponseBaseModel<RentCodeOrder>> RequestAOrder(string apiKey, [FromBody] RequestOrderRequest request)
        {
            if (request.MaximumSms.HasValue)
            {
                request.MaximumSms = Math.Max(Math.Min(5, request.MaximumSms.Value), 1);
            }

            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return new ApiResponseBaseModel<RentCodeOrder>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            return await _orderService.RequestAOrder(userId, request.ServiceProviderId, request.NetworkProvider, request.MaximumSms, Helpers.AppSourceType.Api, request.OnlyAcceptFreshOtp.GetValueOrDefault(), request.AllowVoiceSms);
        }
        [HttpPost("create-holding-sim-request")]
        public async Task<ApiResponseBaseModel<RentCodeOrder>> RequestAHoldingSimOrder(string apiKey, [FromBody] RequestHoldingSim request)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return new ApiResponseBaseModel<RentCodeOrder>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            return await _orderService.RequestThirdOrder(userId, request.Duration, request.Unit, request.NetworkProvider, Helpers.AppSourceType.Api);
        }
        [HttpPost("create-callback-sim-request")]
        public async Task<ApiResponseBaseModel<RentCodeOrder>> RequestACallbackSimOrder(string apiKey, [FromBody] RequestSimCallback request)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return new ApiResponseBaseModel<RentCodeOrder>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            return await _orderService.RequestOrderCallback(userId, request.RequestPhoneNumber, Helpers.AppSourceType.Api);
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
        [HttpGet("orders/{orderId}/check-status")]
        public async Task<ApiResponseBaseModel<OrderStatusWithResultCount>> CheckOrderStatus(string apiKey, int orderId)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return new ApiResponseBaseModel<OrderStatusWithResultCount>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            var available = await _orderService.CheckOrderIsAvailableForUser(orderId, userId);
            if (!available)
            {
                return new ApiResponseBaseModel<OrderStatusWithResultCount>()
                {
                    Success = false,
                    Message = "NotFound"
                };
            }
            var resultFromService = await _orderStatusService.CheckOrdersStatus(new CheckOrdersStatusRequest()
            {
                OrderIds = new List<int>() { orderId }
            });
            if (resultFromService.Success && resultFromService.Results.Statuses.Count > 0)
            {
                var order = await _orderService.Get(orderId);
                var orderResult = resultFromService.Results.Statuses.FirstOrDefault();
                if (orderResult != null && order != null)
                {
                    orderResult.PhoneNumber = order.PhoneNumber;
                    var gsm512IdList = await _cacheService.Gsm512IdList();

                    if (order.Status == OrderStatus.Waiting
                        && order.Updated > _dateTimeService.UtcNow().AddSeconds(-16)
                        && order.ConnectedGsmId.HasValue
                        && gsm512IdList.Contains(order.ConnectedGsmId.Value))
                    {
                        orderResult.PhoneNumber = "";
                    }
                    orderResult.Expired = order.Expired;
                }
                return new ApiResponseBaseModel<OrderStatusWithResultCount>()
                {
                    Success = true,
                    Results = orderResult
                };
            }
            return new ApiResponseBaseModel<OrderStatusWithResultCount>()
            {
                Success = false,
                Message = "NotFound"
            };
        }
        [HttpPost("orders/{orderId}/results")]
        public async Task<FilterResponse<OrderResultModel>> OrderResults(string apiKey, int orderId, [FromBody] FilterRequest request)
        {
            return await _cache.GetOrCreateAsync($"OrderResults_{orderId}", async entry =>
            {
                entry.SetAbsoluteExpiration(TimeSpan.FromSeconds(3));

                var userId = await _userService.GetUserIdFromApiKey(apiKey);
                if (userId == 0)
                {
                    return new FilterResponse<OrderResultModel>()
                    {
                        Total = 0,
                        Results = new List<OrderResultModel>()
                    };
                }
                var checkOrder = await _orderService.CheckOrderIsAvailableForUser(orderId, userId);
                if (!checkOrder)
                {
                    return new FilterResponse<OrderResultModel>()
                    {
                        Results = new List<OrderResultModel>(),
                        Total = 0
                    };
                }
                request.SearchObject = request.SearchObject ?? new Dictionary<string, object>();
                request.SearchObject.Add("OrderId", orderId);
                var response = await _orderResultService.Paging(request);

                return new FilterResponse<OrderResultModel>()
                {
                    Total = response.Total,
                    Results = response.Results.Select(r => new OrderResultModel()
                    {
                        Message = r.Message,
                        PhoneNumber = r.PhoneNumber,
                        Sender = r.Sender,
                        AudioUrl = r.AudioUrl,
                        MessageType = r.SmsType.ToString(),
                    }).ToList()
                };
            });
        }
        [HttpPost("orders/{orderId}/cancel")]
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
        [HttpPost]
        [Route("put-sms")]
        public async Task<ApiResponseBaseModel<SmsHistory>> PutSms(string apiKey, [FromBody] PutSmsRequest request)
        {
            try
            {
                if ((request.FromPhoneNumber ?? string.Empty).Length > 15)
                {
                    return new ApiResponseBaseModel<SmsHistory>()
                    {
                        Success = false,
                        Message = "FromPhoneNumberInvalid"
                    };
                }
                var userId = await _userService.GetUserIdFromApiKey(apiKey);
                if (userId == 0)
                {
                    return new ApiResponseBaseModel<SmsHistory>()
                    {
                        Success = false,
                        Message = "Unauthorized"
                    };
                }
                var phoneNumber = request.FromPhoneNumber;
                var message = request.Message;

                await _asyncLocker.DedicatedPutSmsLocker(userId).WaitAsync();
                try
                {
                    _cache.TryGetValue($"CACHE_LAST_SMS_MESSAGE_{phoneNumber}", out string cacheMessage);
                    if (cacheMessage == message)
                    {
                        return new ApiResponseBaseModel<SmsHistory>()
                        {
                            Success = false,
                            Message = "DuplicateMessage"
                        };
                    }
                    _cache.Set($"CACHE_LAST_SMS_MESSAGE_{phoneNumber}", message, TimeSpan.FromSeconds(5));
                }
                finally
                {
                    _asyncLocker.DedicatedPutSmsLocker(userId).Release();
                }

                var user = await _userService.GetUser(userId);
                if (user == null || (user.Role != Helpers.RoleType.Administrator && user.Role != Helpers.RoleType.Staff))
                {
                    return new ApiResponseBaseModel<SmsHistory>()
                    {
                        Success = false,
                        Message = "Unauthorized"
                    };
                }
                return await _smsService.ReceiveSms(request.Message, request.FromPhoneNumber, request.Sender, request.ReceivedDate);
            }
            catch (Exception e)
            {
                _logger.LogError("putSmsError!");
                _logger.LogError(e, JsonConvert.SerializeObject(request));
                throw;
            }
        }
        [HttpPost]
        [Route("put-audio-sms")]
        public async Task<ApiResponseBaseModel<SmsHistory>> PutAudioSms(string apiKey, [FromBody] PutAudioSmsRequest request)
        {
            try
            {
                if ((request.FromPhoneNumber ?? string.Empty).Length > 15)
                {
                    return new ApiResponseBaseModel<SmsHistory>()
                    {
                        Success = false,
                        Message = "FromPhoneNumberInvalid"
                    };
                }
                var userId = await _userService.GetUserIdFromApiKey(apiKey);
                if (userId == 0)
                {
                    return new ApiResponseBaseModel<SmsHistory>()
                    {
                        Success = false,
                        Message = "Unauthorized"
                    };
                }
                var user = await _userService.GetUser(userId);
                if (user == null || (user.Role != Helpers.RoleType.Administrator && user.Role != Helpers.RoleType.Staff))
                {
                    return new ApiResponseBaseModel<SmsHistory>()
                    {
                        Success = false,
                        Message = "Unauthorized"
                    };
                }
                return await _smsService.ReceiveAudioSms(request.AudioUrl, request.FromPhoneNumber, request.Sender, request.ReceivedDate);
            }
            catch (Exception e)
            {
                _logger.LogError("putAudioSmsError!");
                _logger.LogError(e, JsonConvert.SerializeObject(request));
                throw;
            }
        }
        [HttpGet("gsm-devices")]
        public async Task<ApiResponseBaseModel<List<GsmDevice>>> GetAllGsmDevice(string apiKey)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return new ApiResponseBaseModel<List<GsmDevice>>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            var user = await _userService.GetUser(userId);
            if (user == null || (user.Role != Helpers.RoleType.Administrator && user.Role != Helpers.RoleType.Staff))
            {
                return new ApiResponseBaseModel<List<GsmDevice>>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            return new ApiResponseBaseModel<List<GsmDevice>>()
            {
                Results = await _gsmDeviceService.GetAlls()
            };
        }

        [HttpPost("gsm-devices")]
        public async Task<ApiResponseBaseModel<GsmDevice>> CreateGsmDevice(string apiKey, [FromBody] GsmDevice device)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return new ApiResponseBaseModel<GsmDevice>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            _logger.LogInformation("CreateGsmDevice: {0} -- {1}", userId, JsonConvert.SerializeObject(device));
            var user = await _userService.GetUser(userId);
            if (user == null || (user.Role != Helpers.RoleType.Administrator && user.Role != Helpers.RoleType.Staff))
            {
                return new ApiResponseBaseModel<GsmDevice>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            device.Id = 0;
            device.Priority = -1;
            return await _gsmDeviceService.Create(device);
        }
        [HttpDelete("gsm-devices/{code}")]
        public async Task<ApiResponseBaseModel<int>> DeleteGsmDevice(string apiKey, string code)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return new ApiResponseBaseModel<int>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            _logger.LogInformation("DeleteGsmDevice: {0} -- {1}", userId, code);
            var user = await _userService.GetUser(userId);
            if (user == null || (user.Role != Helpers.RoleType.Administrator && user.Role != Helpers.RoleType.Staff))
            {
                return new ApiResponseBaseModel<int>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            var deviceId = await _gsmDeviceService.LookupIdFromCode(code);
            if (deviceId == 0)
            {
                return new ApiResponseBaseModel<int>()
                {
                    Success = false,
                    Message = "CodeNotFound"
                };
            }
            return await _gsmDeviceService.Delete(deviceId);
        }
        [HttpPut("gsm-devices/{code}")]
        public async Task<ApiResponseBaseModel<GsmDevice>> UpdateGsmDevice(string apiKey, string code, [FromBody] GsmDevice device)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return new ApiResponseBaseModel<GsmDevice>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            _logger.LogInformation("UpdateGsmDevice: {0} -- {1}", userId, JsonConvert.SerializeObject(device));
            var user = await _userService.GetUser(userId);
            if (user == null || (user.Role != Helpers.RoleType.Administrator && user.Role != Helpers.RoleType.Staff))
            {
                return new ApiResponseBaseModel<GsmDevice>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            var id = await _gsmDeviceService.LookupIdFromCode(code);
            device.Id = id;
            device.Code = code;
            return await _gsmDeviceService.Update(device);
        }

        [HttpGet("gsm-device/{code}/listerning-coms")]
        public async Task<ApiResponseBaseModel<List<Com>>> GetSmsHistoryOrderDetail(string apiKey, string code)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return new ApiResponseBaseModel<List<Com>>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            var user = await _userService.GetUser(userId);
            if (user == null || (user.Role != Helpers.RoleType.Administrator && user.Role != Helpers.RoleType.Staff))
            {
                return new ApiResponseBaseModel<List<Com>>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            var id = await _gsmDeviceService.LookupIdFromCode(code);
            if (id == 0)
            {
                return new ApiResponseBaseModel<List<Com>>()
                {
                    Message = "GsmCodeNotFound",
                    Success = false
                };
            }
            return new ApiResponseBaseModel<List<Com>>()
            {
                Success = true,
                Results = await _comService.GetListerningComsByGsmId(id)
            };
        }
        [HttpGet("gsm-devices/{code}/coms")]
        public async Task<ApiResponseBaseModel<List<Com>>> GetComsByGsmId(string apiKey, string code)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return new ApiResponseBaseModel<List<Com>>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            var user = await _userService.GetUser(userId);
            if (user == null || (user.Role != Helpers.RoleType.Administrator && user.Role != Helpers.RoleType.Staff))
            {
                return new ApiResponseBaseModel<List<Com>>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            var id = await _gsmDeviceService.LookupIdFromCode(code);
            if (id == 0)
            {
                return new ApiResponseBaseModel<List<Com>>()
                {
                    Message = "GsmCodeNotFound",
                    Success = false
                };
            }
            return new ApiResponseBaseModel<List<Com>>()
            {
                Success = true,
                Results = await _comService.GetComsByGsmId(id)
            };
        }

        [HttpPost("gsm-devices/{code}/coms")]
        public async Task<ApiResponseBaseModel<Com>> CreateCom(string apiKey, string code, [FromBody] Com com)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return new ApiResponseBaseModel<Com>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            _logger.LogInformation("CreateCom: {0} -- {1}", userId, JsonConvert.SerializeObject(com));
            var user = await _userService.GetUser(userId);

            if (user == null || (user.Role != Helpers.RoleType.Administrator && user.Role != Helpers.RoleType.Staff))
            {
                return new ApiResponseBaseModel<Com>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            var gsmId = await _gsmDeviceService.LookupIdFromCode(code);
            if (gsmId == 0) return new ApiResponseBaseModel<Com>()
            {
                Message = "GsmCodeNotFound",
                Success = false
            };
            com.Id = 0;
            com.GsmDeviceId = gsmId;
            await _gsmDeviceService.TouchLastActive(com.GsmDeviceId);
            return await _comService.Create(com);
        }

        [HttpDelete("gsm-devices/{gsmCode}/coms/{comCode}")]
        public async Task<ApiResponseBaseModel<int>> DeleteCom(string apiKey, string gsmCode, string comCode)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return new ApiResponseBaseModel<int>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            _logger.LogInformation("DeleteCom: {0} -- {1} + {2}", userId, gsmCode, comCode);
            var user = await _userService.GetUser(userId);
            if (user == null || (user.Role != Helpers.RoleType.Administrator && user.Role != Helpers.RoleType.Staff))
            {
                return new ApiResponseBaseModel<int>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            Com com = await _comService.LookupComFromGsmCodeAndComCode(gsmCode, comCode);
            if (com == null)
            {
                return new ApiResponseBaseModel<int>()
                {
                    Success = false,
                    Message = "ComNotFound"
                };
            }
            await _gsmDeviceService.TouchLastActive(com.GsmDeviceId);
            return await _comService.Delete(com.Id);
        }

        [HttpPut("gsm-devices/{gsmCode}/coms/{comCode}")]
        public async Task<ApiResponseBaseModel<Com>> UpdateCom(string apiKey, string gsmCode, string comCode, [FromBody] Com com)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return new ApiResponseBaseModel<Com>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            _logger.LogInformation("UpdateCom: {0} -- {1} + {2} + {3}", userId, gsmCode, comCode, JsonConvert.SerializeObject(com));
            var user = await _userService.GetUser(userId);
            if (user == null || (user.Role != Helpers.RoleType.Administrator && user.Role != Helpers.RoleType.Staff))
            {
                return new ApiResponseBaseModel<Com>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            Com c = await _comService.LookupComFromGsmCodeAndComCode(gsmCode, comCode);
            if (com.PhoneNumber != c.PhoneNumber)
            {
                ComHistory comHistory = new ComHistory()
                {
                    ComName = comCode,
                    GsmDeviceId = c.GsmDeviceId,
                    OldPhoneNumber = c.PhoneNumber,
                    NewPhoneNumber = com.PhoneNumber
                };
                await _comHistoryService.Create(comHistory);
            }
            await _gsmDeviceService.TouchLastActive(com.GsmDeviceId);
            com.Id = c.Id;
            com.ComName = comCode;
            return await _comService.Update(com);
        }

        [HttpPost("offline-payment-notice")]
        public async Task<ApiResponseBaseModel> OfflinePaymentNotice(string apiKey, [FromBody] OfflinePaymentNoticeModel request)
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
            var user = await _userService.GetUser(userId);
            if (user == null || user.Role != Helpers.RoleType.Administrator)
            {
                return new ApiResponseBaseModel()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            return await _userOfflinePaymentReceiptService.HandleOfflinePaymentNotice(request, false);
        }
        [HttpGet("sms-history/{smsHistoryId}/order")]
        public async Task<ApiResponseBaseModel<Order>> GetSmsHistoryDetail(string apiKey, int smsHistoryId)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0) return ApiResponseBaseModel<Order>.UnAuthorizedResponse();

            var user = await _userService.GetUser(userId);
            if (user == null || user.Role != Helpers.RoleType.Administrator) return ApiResponseBaseModel<Order>.UnAuthorizedResponse();

            var result = await _smsHistoryService.GetOrderOfSmsHistory(smsHistoryId);
            return new ApiResponseBaseModel<Order>()
            {
                Results = result
            };
        }
        [HttpGet("sms-history/{smsHistoryId}/order-result")]
        public async Task<ApiResponseBaseModel<HistoryOrderResult>> GetSmsHistoryOrderDetail(string apiKey, int smsHistoryId)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0) return ApiResponseBaseModel<HistoryOrderResult>.UnAuthorizedResponse();

            var user = await _userService.GetUser(userId);
            if (user == null || (user.Role != Helpers.RoleType.Administrator && user.Role != Helpers.RoleType.Staff))
            {
                return ApiResponseBaseModel<HistoryOrderResult>.UnAuthorizedResponse();
            }
            var result = await _smsHistoryService.GetOrderOfSmsHistory(smsHistoryId);
            if (result == null) return ApiResponseBaseModel<HistoryOrderResult>.NotFoundResourceResponse();
            return new ApiResponseBaseModel<HistoryOrderResult>()
            {
                Results = new HistoryOrderResult()
                {
                    Created = result.Created,
                    Updated = result.Updated,
                    OrderGuid = result.Guid,
                    OrderId = result.Id,
                    OrderStatus = result.Status,
                    ServiceProviderGuid = result.ServiceProvider?.Guid,
                    ServiceProviderId = result.ServiceProviderId,
                    ServiceProviderName = result.ServiceProvider?.Name
                }
            };
        }

        [HttpGet("gsm-devices/{gsmCode}/maintenance-status")]
        public async Task<ApiResponseBaseModel<GsmMaintenanceStatus>> GetGsmMaintenanceStatus(string apiKey, string gsmCode)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return new ApiResponseBaseModel<GsmMaintenanceStatus>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            var user = await _userService.GetUser(userId);
            if (user == null || (user.Role != Helpers.RoleType.Administrator && user.Role != Helpers.RoleType.Staff))
            {
                return new ApiResponseBaseModel<GsmMaintenanceStatus>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            var id = await _gsmDeviceService.LookupIdFromCode(gsmCode);
            if (id == 0)
            {
                return new ApiResponseBaseModel<GsmMaintenanceStatus>()
                {
                    Message = "GsmCodeNotFound",
                    Success = false
                };
            }
            return new ApiResponseBaseModel<GsmMaintenanceStatus>()
            {
                Success = true,
                Results = await _gsmDeviceService.CheckMaintenanceStatus(id)
            };
        }
        [HttpPost("gsm-devices/{gsmCode}/turn-maintenance")]
        public async Task<ApiResponseBaseModel> TurnMaintenance(string apiKey, string gsmCode, bool isOn)
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
            var user = await _userService.GetUser(userId);
            if (user == null || (user.Role != Helpers.RoleType.Administrator && user.Role != Helpers.RoleType.Staff))
            {
                return new ApiResponseBaseModel()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            var id = await _gsmDeviceService.LookupIdFromCode(gsmCode);
            if (id == 0)
            {
                return new ApiResponseBaseModel()
                {
                    Message = "GsmCodeNotFound",
                    Success = false
                };
            }
            return await _gsmDeviceService.ToggleMaintenanceStatus(id, isOn);
        }

        [HttpPost("live-check/get-pending-tasks")]
        public async Task<ApiResponseBaseModel<List<ServiceProviderPhoneNumberLiveCheck>>> GetPendingLiveCheckingTasks(string apiKey, string serviceName)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return ApiResponseBaseModel<List<ServiceProviderPhoneNumberLiveCheck>>.UnAuthorizedResponse();
            }
            var user = await _userService.GetUser(userId);
            if (user == null || user.Role != Helpers.RoleType.UtilityTool)
            {
                return ApiResponseBaseModel<List<ServiceProviderPhoneNumberLiveCheck>>.UnAuthorizedResponse();
            }
            return await _serviceProviderPhoneNumberLiveCheckService.GetPendingCheckingTasks(serviceName);
        }
        [HttpPost("live-check/{liveCheckId}/confirm-success")]
        public async Task<ApiResponseBaseModel> ConfirmLiveCheckSuccess(string apiKey, string liveCheckId)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return ApiResponseBaseModel.UnAuthorizedResponse();
            }
            var user = await _userService.GetUser(userId);
            if (user == null || user.Role != Helpers.RoleType.UtilityTool)
            {
                return ApiResponseBaseModel.UnAuthorizedResponse();
            }
            return await _serviceProviderPhoneNumberLiveCheckService.ConfirmLiveCheck(liveCheckId, true);
        }
        [HttpPost("live-check/{liveCheckId}/confirm-fail")]
        public async Task<ApiResponseBaseModel> ConfirmLiveCheckFail(string apiKey, string liveCheckId)
        {
            var userId = await _userService.GetUserIdFromApiKey(apiKey);
            if (userId == 0)
            {
                return ApiResponseBaseModel.UnAuthorizedResponse();
            }
            var user = await _userService.GetUser(userId);
            if (user == null || user.Role != Helpers.RoleType.UtilityTool)
            {
                return ApiResponseBaseModel.UnAuthorizedResponse();
            }
            return await _serviceProviderPhoneNumberLiveCheckService.ConfirmLiveCheck(liveCheckId, false);
        }
    }
}
