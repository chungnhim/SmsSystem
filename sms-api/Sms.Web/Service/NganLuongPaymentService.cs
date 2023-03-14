using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sms.Web.Entity;
using Sms.Web.Helpers;
using Sms.Web.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Sms.Web.Service
{
    public interface INganLuongPaymentService
    {
        Task<ApiResponseBaseModel<GeneralPaymentResponse>> RequestNganLuongPayment(GeneralPaymentRequest momoPaymentRequest);
        Task ProcessNganLuongCallback(NganLuongNotifyReturnModel returnModel);
    }
    public class NganLuongPaymentService : INganLuongPaymentService
    {
        private readonly ISystemConfigurationService _systemConfigurationService;
        private readonly IAuthService _authService;
        private readonly IHttpContextAccessor _context;
        private readonly IUserPaymentTransactionService _userPaymentTransactionService;
        private readonly ILogger _logger;
        private readonly IUserTransactionService _userTransactionService;
        public NganLuongPaymentService(ISystemConfigurationService systemConfigurationService, IAuthService authService, IHttpContextAccessor context, IUserPaymentTransactionService userPaymentTransactionService, IUserTransactionService userTransactionService, ILogger<NganLuongPaymentService> logger)
        {
            _systemConfigurationService = systemConfigurationService;
            _authService = authService;
            _context = context;
            _userPaymentTransactionService = userPaymentTransactionService;
            _userTransactionService = userTransactionService;
            _logger = logger;
        }

        public async Task ProcessNganLuongCallback(NganLuongNotifyReturnModel returnModel)
        {
            if (!string.IsNullOrEmpty(returnModel.error_text)) return;
            var configuration = (await _systemConfigurationService.GetAlls()).FirstOrDefault();
            if (configuration == null)
            {
                return;
            }
            _logger.LogInformation("callback from Ngan Luong: {0}", JsonConvert.SerializeObject(returnModel));
            String transaction_info = returnModel.transaction_info;
            String order_code = returnModel.order_code;
            String price = returnModel.price+"";
            String payment_id = returnModel.payment_id + "";
            String payment_type = returnModel.payment_type + "";
            String error_text = returnModel.error_text;
            String secure_code = returnModel.secure_code;
            NL_Checkout checkOut = new NL_Checkout();
            checkOut.merchant_site_code = configuration.NganLuongIsLiveEnvironment ? configuration.NganLuongLiveMerchantCode : configuration.NganLuongSandboxMerchantCode;
            checkOut.secure_pass = configuration.NganLuongIsLiveEnvironment ? configuration.NganLuongLiveMerchantPassword : configuration.NganLuongSandboxMerchantPassword;
            bool rs = checkOut.verifyPaymentUrl(transaction_info, order_code, price, payment_id, payment_type, error_text, secure_code);

            if (!rs)
            {
                _logger.LogInformation("Signature from NganLuong invalid");
                return;
            }
            var userPaymentTransaction = await _userPaymentTransactionService.GetByRequestId(returnModel.order_code);
            if (userPaymentTransaction == null) return;
            _logger.LogInformation($"Is payment transaction expired {userPaymentTransaction.IsExpired}");
            if (userPaymentTransaction.IsExpired) return;
            var transaction = new UserTransaction()
            {
                UserId = userPaymentTransaction.UserId,
                Comment = $"NganLuong: req_{userPaymentTransaction.RequestId}",
                Amount = userPaymentTransaction.Money,
                IsImport = true,
                UserTransactionType = UserTransactionType.UserRecharge
            };
            await _userTransactionService.Create(transaction);
            userPaymentTransaction.IsExpired = true;
            await _userPaymentTransactionService.Update(userPaymentTransaction);
        }

        public async Task<ApiResponseBaseModel<GeneralPaymentResponse>> RequestNganLuongPayment(GeneralPaymentRequest momoPaymentRequest)
        {
            var configuration = (await _systemConfigurationService.GetAlls()).FirstOrDefault();
            if (configuration == null)
            {
                return new ApiResponseBaseModel<GeneralPaymentResponse>()
                {
                    Success = false,
                    Message = "ConfigurationNotSupportNganLuong"
                };
            }

            NL_Checkout nlcheckout = new NL_Checkout();
            nlcheckout.merchant_site_code = configuration.NganLuongIsLiveEnvironment ? configuration.NganLuongLiveMerchantCode : configuration.NganLuongSandboxMerchantCode;
            nlcheckout.secure_pass = configuration.NganLuongIsLiveEnvironment ? configuration.NganLuongLiveMerchantPassword : configuration.NganLuongSandboxMerchantPassword;
            var receiver = configuration.NganLuongIsLiveEnvironment ? configuration.NganLuongLiveReceiver : configuration.NganLuongSandboxReceiver;
            nlcheckout.nganluong_url = configuration.NganLuongIsLiveEnvironment ? "https://www.nganluong.vn/checkout.php" : "https://sandbox.nganluong.vn:8088/nl35/checkout.php";

            var transaction = new UserPaymentTransaction()
            {
                RequestId = Guid.NewGuid().ToString().ToLower(),
                Money = momoPaymentRequest.Money,
                UserId = _authService.CurrentUserId().GetValueOrDefault()
            };
            string strOrderID = transaction.RequestId;

            var current = _context.HttpContext;
            string notifyurl = $"{current.Request.Scheme}://{current.Request.Host}{current.Request.PathBase}/api/NganLuong/ReturnCallback";
            string rs = nlcheckout.buildCheckoutUrlNew(momoPaymentRequest.CallbackUrl, notifyurl, receiver, "", strOrderID, momoPaymentRequest.Money.ToString(), "vnd", 1, 0, 0, 0, 0, "", "", "");

            await _userPaymentTransactionService.Create(transaction);

            return new ApiResponseBaseModel<GeneralPaymentResponse>()
            {
                Success = true,
                Results = new GeneralPaymentResponse()
                {
                    PayUrl = rs
                }
            };
        }
    }
}
