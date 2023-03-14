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
    public interface IMomoPaymentService
    {
        Task<ApiResponseBaseModel<GeneralPaymentResponse>> RequestMomoPayment(GeneralPaymentRequest momoPaymentRequest);
        Task ProcessMomoCallback(MomoNotifyReturnModel returnModel);
    }
    public class MomoPaymentService : IMomoPaymentService
    {
        private readonly ISystemConfigurationService _systemConfigurationService;
        private readonly IAuthService _authService;
        private readonly IHttpContextAccessor _context;
        private readonly IUserPaymentTransactionService _userPaymentTransactionService;
        private readonly ILogger _logger;
        private readonly IUserTransactionService _userTransactionService;
        public MomoPaymentService(ISystemConfigurationService systemConfigurationService, IAuthService authService, IHttpContextAccessor context, IUserPaymentTransactionService userPaymentTransactionService, IUserTransactionService userTransactionService, ILogger<MomoPaymentService> logger)
        {
            _systemConfigurationService = systemConfigurationService;
            _authService = authService;
            _context = context;
            _userPaymentTransactionService = userPaymentTransactionService;
            _userTransactionService = userTransactionService;
            _logger = logger;
        }

        public async Task ProcessMomoCallback(MomoNotifyReturnModel returnModel)
        {
            if (returnModel.ErrorCode != "0") return;
            var configuration = (await _systemConfigurationService.GetAlls()).FirstOrDefault();
            if (configuration == null)
            {
                return;
            }
            string serectkey = configuration.MomoSecretKey;
            _logger.LogInformation("callback from Momo: {0}", JsonConvert.SerializeObject(returnModel));
            string rawHash = $"partnerCode={returnModel.PartnerCode}&accessKey={returnModel.AccessKey}&requestId={returnModel.RequestId}&amount={returnModel.Amount}&orderId={returnModel.OrderId}&orderInfo={returnModel.OrderInfo}&orderType={returnModel.OrderType}&transId={returnModel.TransId}&message={returnModel.Message}&localMessage={returnModel.LocalMessage}&responseTime={returnModel.ResponseTime}&errorCode={returnModel.ErrorCode}&payType={returnModel.PayType}&extraData={returnModel.ExtraData}";
            rawHash = WebUtility.UrlDecode(rawHash);
            MoMoSecurity crypto = new MoMoSecurity(_logger);
            //sign signature SHA256
            string signature = crypto.signSHA256(rawHash, serectkey);
            if (signature != returnModel.Signature)
            {
                _logger.LogInformation("Signature from Momo invalid");
                return;
            }
            var userPaymentTransaction = await _userPaymentTransactionService.GetByRequestId(returnModel.OrderId);
            if (userPaymentTransaction == null) return;
            _logger.LogInformation($"Is payment transaction expired {userPaymentTransaction.IsExpired}");
            if (userPaymentTransaction.IsExpired) return;
            var transaction = new UserTransaction()
            {
                UserId = userPaymentTransaction.UserId,
                Comment = $"MOMO: req_{userPaymentTransaction.RequestId}",
                Amount = userPaymentTransaction.Money,
                IsImport = true,
                UserTransactionType = UserTransactionType.UserRecharge
            };
            await _userTransactionService.Create(transaction);
            userPaymentTransaction.IsExpired = true;
            await _userPaymentTransactionService.Update(userPaymentTransaction);
        }

        public async Task<ApiResponseBaseModel<GeneralPaymentResponse>> RequestMomoPayment(GeneralPaymentRequest momoPaymentRequest)
        {
            var configuration = (await _systemConfigurationService.GetAlls()).FirstOrDefault();
            if (configuration == null)
            {
                return new ApiResponseBaseModel<GeneralPaymentResponse>()
                {
                    Success = false,
                    Message = "ConfigurationNotSupportMomo"
                };
            }
            //request params need to request to MoMo system
            string endpoint = configuration.MomoApiEndPoint;
            string partnerCode = configuration.MomoPartnerCode;
            string accessKey = configuration.MomoAccessKey;
            string serectkey = configuration.MomoSecretKey;
            var transaction = new UserPaymentTransaction()
            {
                RequestId = Guid.NewGuid().ToString().ToLower(),
                Money = momoPaymentRequest.Money,
                UserId = _authService.CurrentUserId().GetValueOrDefault()
            };
            string orderInfo = transaction.RequestId;
            string returnUrl = momoPaymentRequest.CallbackUrl;
            var current = _context.HttpContext;
            string notifyurl = $"{current.Request.Scheme}://{current.Request.Host}{current.Request.PathBase}/api/Momo/ReturnCallback";

            string amount = transaction.Money.ToString();

            string orderid = transaction.RequestId;
            string requestId = $"req_{transaction.RequestId}";
            string extraData = "merchantName=;merchantId=";//pass empty value if your merchant does not have stores else merchantName=[storeName]; merchantId=[storeId] to identify a transaction map with a physical store

            //before sign HMAC SHA256 signature
            string rawHash = "partnerCode=" +
                partnerCode + "&accessKey=" +
                accessKey + "&requestId=" +
                requestId + "&amount=" +
                amount + "&orderId=" +
                orderid + "&orderInfo=" +
                orderInfo + "&returnUrl=" +
                returnUrl + "&notifyUrl=" +
                notifyurl + "&extraData=" +
                extraData;
            _logger.LogInformation("rawHash = " + rawHash);

            MoMoSecurity crypto = new MoMoSecurity(_logger);
            //sign signature SHA256
            string signature = crypto.signSHA256(rawHash, serectkey);
            _logger.LogInformation("Signature = " + signature);

            //build body json request
            JObject message = new JObject
            {
                { "partnerCode", partnerCode },
                { "accessKey", accessKey },
                { "requestId", requestId },
                { "amount", amount },
                { "orderId", orderid },
                { "orderInfo", orderInfo },
                { "returnUrl", returnUrl },
                { "notifyUrl", notifyurl },
                { "requestType", "captureMoMoWallet" },
                { "signature", signature },
                {"extraData", extraData }

            };
            _logger.LogInformation("Json request to MoMo: " + message.ToString());
            string responseFromMomo = PaymentRequest.sendPaymentRequest(endpoint, message.ToString());

            JObject jmessage = JObject.Parse(responseFromMomo);
            _logger.LogInformation("Return from MoMo: " + jmessage.ToString());
            if (jmessage.GetValue("errorCode").ToString() == "0")
            {
                await _userPaymentTransactionService.Create(transaction);
                return new ApiResponseBaseModel<GeneralPaymentResponse>()
                {
                    Success = true,
                    Results = new GeneralPaymentResponse()
                    {
                        PayUrl = jmessage.GetValue("payUrl").ToString()
                    }
                };
            }
            return new ApiResponseBaseModel<GeneralPaymentResponse>()
            {
                Success = false,
                Message = "ErrorFromMomo"
            };
        }
    }

    class MoMoSecurity
    {
        private static RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
        private readonly ILogger _logger;
        public MoMoSecurity(ILogger logger)
        {
            _logger = logger;
            //encrypt and decrypt password using secure
        }
        public string getHash(string partnerCode, string merchantRefId,
            string amount, string paymentCode, string storeId, string storeName, string publicKey)
        {
            string json = "{\"partnerCode\":\"" +
                partnerCode + "\",\"partnerRefId\":\"" +
                merchantRefId + "\",\"amount\":" +
                amount + ",\"paymentCode\":\"" +
                paymentCode + "\",\"storeId\":\"" +
                storeId + "\",\"storeName\":\"" +
                storeName + "\"}";
            _logger.LogInformation("Raw hash: " + json);
            byte[] data = Encoding.UTF8.GetBytes(json);
            string result = null;
            using (var rsa = new RSACryptoServiceProvider(4096)) // or 4096, base on key length
            {
                try
                {
                    // Client encrypting data with public key issued by server
                    // "publicKey" must be XML format, use https://superdry.apphb.com/tools/online-rsa-key-converter
                    // to convert from PEM to XML before hash
                    rsa.FromXmlString(publicKey);
                    var encryptedData = rsa.Encrypt(data, false);
                    var base64Encrypted = Convert.ToBase64String(encryptedData);
                    result = base64Encrypted;
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }

            }

            return result;

        }
        public string buildQueryHash(string partnerCode, string merchantRefId,
            string requestid, string publicKey)
        {
            string json = "{\"partnerCode\":\"" +
                partnerCode + "\",\"partnerRefId\":\"" +
                merchantRefId + "\",\"requestId\":\"" +
                requestid + "\"}";
            _logger.LogInformation("Raw hash: " + json);
            byte[] data = Encoding.UTF8.GetBytes(json);
            string result = null;
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                try
                {
                    // client encrypting data with public key issued by server
                    rsa.FromXmlString(publicKey);
                    var encryptedData = rsa.Encrypt(data, false);
                    var base64Encrypted = Convert.ToBase64String(encryptedData);
                    result = base64Encrypted;
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }

            }

            return result;

        }

        public string buildRefundHash(string partnerCode, string merchantRefId,
            string momoTranId, long amount, string description, string publicKey)
        {
            string json = "{\"partnerCode\":\"" +
                partnerCode + "\",\"partnerRefId\":\"" +
                merchantRefId + "\",\"momoTransId\":\"" +
                momoTranId + "\",\"amount\":" +
                amount + ",\"description\":\"" +
                description + "\"}";
            _logger.LogInformation("Raw hash: " + json);
            byte[] data = Encoding.UTF8.GetBytes(json);
            string result = null;
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                try
                {
                    // client encrypting data with public key issued by server
                    rsa.FromXmlString(publicKey);
                    var encryptedData = rsa.Encrypt(data, false);
                    var base64Encrypted = Convert.ToBase64String(encryptedData);
                    result = base64Encrypted;
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }

            }

            return result;

        }
        public string signSHA256(string message, string key)
        {
            byte[] keyByte = Encoding.UTF8.GetBytes(key);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                string hex = BitConverter.ToString(hashmessage);
                hex = hex.Replace("-", "").ToLower();
                return hex;

            }
        }
    }

    class PaymentRequest
    {
        public PaymentRequest()
        {
        }
        public static string sendPaymentRequest(string endpoint, string postJsonString)
        {

            try
            {
                HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(endpoint);

                var postData = postJsonString;

                var data = Encoding.UTF8.GetBytes(postData);

                httpWReq.ProtocolVersion = HttpVersion.Version11;
                httpWReq.Method = "POST";
                httpWReq.ContentType = "application/json";

                httpWReq.ContentLength = data.Length;
                httpWReq.ReadWriteTimeout = 30000;
                httpWReq.Timeout = 15000;
                Stream stream = httpWReq.GetRequestStream();
                stream.Write(data, 0, data.Length);
                stream.Close();

                HttpWebResponse response = (HttpWebResponse)httpWReq.GetResponse();

                string jsonresponse = "";

                using (var reader = new StreamReader(response.GetResponseStream()))
                {

                    string temp = null;
                    while ((temp = reader.ReadLine()) != null)
                    {
                        jsonresponse += temp;
                    }
                }


                //todo parse it
                return jsonresponse;
                //return new MomoResponse(mtid, jsonresponse);

            }
            catch (WebException e)
            {
                return e.Message;
            }
        }
    }
}
