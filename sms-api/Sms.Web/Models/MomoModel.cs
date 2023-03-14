using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Sms.Web.Models
{
    public class GeneralPaymentResponse
    {
        public string PayUrl { get; set; }
    }
    public class GeneralPaymentRequest
    {
        public long Money { get; set; }
        public string CallbackUrl { get; set; }
    }

    public class MomoNotifyReturnModel
    {
        public string PartnerCode { get; set; }
        public string AccessKey { get; set; }
        public string RequestId { get; set; }
        public string Amount { get; set; }
        public string OrderId { get; set; }
        public string OrderInfo { get; set; }
        public string OrderType { get; set; }
        public string TransId { get; set; }
        public string ErrorCode { get; set; }
        public string Message { get; set; }
        public string LocalMessage { get; set; }
        public string PayType { get; set; }
        public string ResponseTime { get; set; }
        public string ExtraData { get; set; }
        public string Signature { get; set; }
    }
    public class NganLuongNotifyReturnModel
    {
        public string transaction_info { get; set; }
        public int price { get; set; }
        public int payment_id { get; set; }
        public int payment_type { get; set; }
        public string error_text { get; set; }
        public string secure_code { get; set; }
        public string token_nl { get; set; }
        public string order_code { get; set; }
    }
    public class PerfectMoneyNotifyReturnModel
    {
        [JsonProperty("PAYEE_ACCOUNT")]
        public string PayeeAccount { get; set; }
        [JsonProperty(PropertyName = "PAYMENT_ID")]
        public string PaymentId { get; set; }
        [JsonProperty("PAYMENT_AMOUNT")]
        public string PaymentAmount { get; set; }
        [JsonProperty("PAYMENT_UNITS")]
        public string PaymentUnits { get; set; }
        [JsonProperty("PAYMENT_BATCH_NUM")]
        public string PaymentBatchNum { get; set; }
        [JsonProperty("PAYER_ACCOUNT")]
        public string PayerAccount { get; set; }
        [JsonProperty("TIMESTAMPGMT")]
        public string Timestamp { get; set; }
        [JsonProperty("V2_HASH")]
        public string V2Hash { get; set; }
    }
    public class PerfectMoneyNotifyReturnRawModel
    {
        public string PAYEE_ACCOUNT { get; set; }
        public string PAYMENT_ID { get; set; }
        public string PAYMENT_AMOUNT { get; set; }
        public string PAYMENT_UNITS { get; set; }
        public string PAYMENT_BATCH_NUM { get; set; }
        public string PAYER_ACCOUNT { get; set; }
        public string TIMESTAMPGMT { get; set; }
        public string V2_HASH { get; set; }
    }
}
