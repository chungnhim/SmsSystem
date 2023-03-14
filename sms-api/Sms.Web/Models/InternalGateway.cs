using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Sms.Web.Helpers;

namespace Sms.Web.Models
{
    public class ApiResponseBaseModel
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; }
        public static ApiResponseBaseModel UnAuthorizedResponse() => new ApiResponseBaseModel()
        {
            Success = false,
            Message = "UnAuthorized"
        };
        public static ApiResponseBaseModel NotFoundResourceResponse() => new ApiResponseBaseModel()
        {
            Success = false,
            Message = "NotFound"
        };
    }
    public class ApiResponseBaseModel<T> : ApiResponseBaseModel
    {
        public T Results { get; set; }
        public static new ApiResponseBaseModel<T> UnAuthorizedResponse() => new ApiResponseBaseModel<T>()
        {
            Success = false,
            Message = "UnAuthorized"
        };
        public static new ApiResponseBaseModel<T> NotFoundResourceResponse() => new ApiResponseBaseModel<T>()
        {
            Success = false,
            Message = "NotFound"
        };
    }

    public class CheckBalanceResponse
    {
        public decimal Balance { get; set; }
    }

    public class PutSmsRequestBase
    {
        public string Sender { get; set; }
        [Required(ErrorMessage = "FromPhoneNumberRequired")]
        public string FromPhoneNumber { get; set; }
        public DateTime ReceivedDate { get; set; }
    }

    public class PutSmsRequest : PutSmsRequestBase
    {
        [Required(ErrorMessage = "MessageRequired")]
        public string Message { get; set; }
    }
    public class PutAudioSmsRequest : PutSmsRequestBase
    {
        public string TextMessage { get; set; }
        [Required(ErrorMessage = "AudioUrlRequired")]
        public string AudioUrl { get; set; }
    }
    public class OfflinePaymentNoticeModel
    {
        [Required]
        public string ReceiptCode { get; set; }
        [Required]
        public decimal Amount { get; set; }
        [Required]
        public string ServiceProvider { get; set; }
        public string OptionalMessage { get; set; }
    }
    public class HistoryOrderResult
    {
        public int OrderId { get; set; }
        public string OrderGuid { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }

        public string ServiceProviderName { get; set; }
        public int ServiceProviderId { get; set; }
        public string ServiceProviderGuid { get; set; }

    }
    public class GsmMaintenanceStatus
    {
        public bool IsMaintenance { get; set; }
        public DateTime? StartedTime { get; set; }
        public DateTime ServerCurrentTime { get; set; }
    }
    public class GsmWarningPayload
    {
        public string StaffEmail { get; set; }
        public int TotalErrors { get; set; }
        public int ContinuosErrors { get; set; }
        public string GsmCode { get; set; }
        public string GsmName { get; set; }
    }
    public class ServiceProviderContinuosFailedAlertPayload
    {
        public int ServiceProviderId { get; set; }
        public int ContinuosFailedCount { get; set; }
    }
    public class UserContinuosFailedAlertPayload
    {
        public int UserId { get; set; }
        public int ContinuosFailedCount { get; set; }
    }
    public class GsmServiceProviderContinuosFailedAlertPayload
    {
        public int GsmId { get; set; }
        public int ServiceProviderId { get; set; }
        public int ContinuosFailedCount { get; set; }
    }
    public class CacheObject<T>
    {
        public DateTime Expired { get; set; }
        public T Object { get; set; }
    }

    public class ErrorPhoneLogUserPreloadedModel
    {
        public int UserId { get; set; }
        public string PhoneNumber { get; set; }
        public int ServiceProviderId { get; set; }
    }

    public class SuccessedPhoneNumberByServiceProviderPreloadedModel
    {
        public int ServiceProviderId { get; set; }
        public string PhoneNumber { get; set; }
    }
}
