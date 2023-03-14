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
    public interface IPerfectMoneyPaymentService
    {
        Task<ApiResponseBaseModel<GeneralPaymentResponse>> RequestPerfectMoneyPayment(GeneralPaymentRequest momoPaymentRequest);
        Task ProcessPerfectMoneyCallback(PerfectMoneyNotifyReturnModel returnModel);
    }
    public class PerfectMoneyPaymentService : IPerfectMoneyPaymentService
    {
        private readonly ISystemConfigurationService _systemConfigurationService;
        private readonly IAuthService _authService;
        private readonly IHttpContextAccessor _context;
        private readonly IUserPaymentTransactionService _userPaymentTransactionService;
        private readonly ILogger _logger;
        private readonly IUserTransactionService _userTransactionService;
        public PerfectMoneyPaymentService(ISystemConfigurationService systemConfigurationService, IAuthService authService, IHttpContextAccessor context, IUserPaymentTransactionService userPaymentTransactionService, IUserTransactionService userTransactionService, ILogger<PerfectMoneyPaymentService> logger)
        {
            _systemConfigurationService = systemConfigurationService;
            _authService = authService;
            _context = context;
            _userPaymentTransactionService = userPaymentTransactionService;
            _userTransactionService = userTransactionService;
            _logger = logger;
        }

        public async Task ProcessPerfectMoneyCallback(PerfectMoneyNotifyReturnModel returnModel)
        {
            var configuration = (await _systemConfigurationService.GetAlls()).FirstOrDefault();
            if (configuration == null)
            {
                return;
            }
            _logger.LogInformation("callback from PerfectMoney: {0}", JsonConvert.SerializeObject(returnModel));
            if(!VerifyHash(returnModel, configuration.PayeeSecretKey))
            {
                _logger.LogInformation("Signature from Perfect money is invalid");
                return;
            }
            var userPaymentTransaction = await _userPaymentTransactionService.GetByRequestId(returnModel.PaymentId);
            if (userPaymentTransaction == null) return;
            if(!decimal.TryParse(returnModel.PaymentAmount,  out decimal paymentAmount))
            {
                _logger.LogInformation("Payment amount from Perfect money is invalid: , {0}", returnModel.PaymentAmount);
                return;
            }
            if(configuration.UsdRate * paymentAmount < userPaymentTransaction.Money - 1000)
            {
                _logger.LogInformation("Payment amount from Perfect money is lower than transaction: , {0} * {1}(rate)", paymentAmount, configuration.UsdRate);
                return;
            }
            _logger.LogInformation($"Is payment transaction expired {userPaymentTransaction.IsExpired}");
            if (userPaymentTransaction.IsExpired) return;
            var transaction = new UserTransaction()
            {
                UserId = userPaymentTransaction.UserId,
                Comment = $"PerfectMoney: req_{userPaymentTransaction.RequestId}",
                Amount = userPaymentTransaction.Money,
                IsImport = true,
                UserTransactionType = UserTransactionType.UserRecharge,
                OfflinePaymentMethodType = OfflinePaymentMethodType.PerfectMoney
            };
            await _userTransactionService.Create(transaction);
            userPaymentTransaction.IsExpired = true;
            await _userPaymentTransactionService.Update(userPaymentTransaction);
        }

        public async Task<ApiResponseBaseModel<GeneralPaymentResponse>> RequestPerfectMoneyPayment(GeneralPaymentRequest momoPaymentRequest)
        {
            var configuration = (await _systemConfigurationService.GetAlls()).FirstOrDefault();
            if (configuration == null)
            {
                return new ApiResponseBaseModel<GeneralPaymentResponse>()
                {
                    Success = false,
                    Message = "ConfigurationNotSupportPerfectMoney"
                };
            }

            var transaction = new UserPaymentTransaction()
            {
                RequestId = Guid.NewGuid().ToString().ToLower(),
                Money = momoPaymentRequest.Money,
                UserId = _authService.CurrentUserId().GetValueOrDefault()
            };
            string strOrderID = transaction.RequestId;
            await _userPaymentTransactionService.Create(transaction);

            Dictionary<string, string> paymentInfo = new Dictionary<string, string>();
            paymentInfo.Add("PaymentId", strOrderID);
            paymentInfo.Add("PayeeAccount", configuration.PayeeAccount);

            return new ApiResponseBaseModel<GeneralPaymentResponse>()
            {
                Success = true,
                Results = new GeneralPaymentResponse()
                {
                    PayUrl = JsonConvert.SerializeObject(paymentInfo)
                }
            };
        }

        private bool VerifyHash(PerfectMoneyNotifyReturnModel model, string secretKey)
        {
            var passwordHash = GetMd5HashValue(secretKey);
            var v2String = $"{model.PaymentId}:{model.PayeeAccount}:{model.PaymentAmount}:{model.PaymentUnits}:{model.PaymentBatchNum}:{model.PayerAccount}:{passwordHash.ToUpper()}:{model.Timestamp}";
            return GetMd5HashValue(v2String).ToUpper().Equals(model.V2Hash);
        }
        private string GetMd5HashValue(string input)
        {
            using (var md5Hash = MD5.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                StringBuilder sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                // Return the hexadecimal string.
                return sBuilder.ToString();
            }
        }
    }
}
