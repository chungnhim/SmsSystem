using Sms.Web.Entity;
using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Models
{
    public class RequestOrderRequest
    {
        [Required]
        public int ServiceProviderId { get; set; }
        public int? NetworkProvider { get; set; }
        public int? MaximumSms { get; set; }
        public bool? OnlyAcceptFreshOtp { get; set; }
        public bool AllowVoiceSms { get; set; }
    }
    public class RequestHoldingSim
    {
        [Required]
        public int Duration { get; set; }
        public HoldSimDurationUnit Unit { get; set; }
        public int? NetworkProvider { get; set; }
    }
    public class RequestSimCallback
    {
        [Required]
        public string RequestPhoneNumber { get; set; }
    }
    public class OrderComplaintRequest
    {
        [Required]
        public string Comment { get; set; }
    }
    public class CheckOrdersStatusRequest
    {
        public List<int> OrderIds { get; set; }
    }
    public class CheckOrdersStatusResponse
    {
        public List<OrderStatusWithResultCount> Statuses { get; set; }
    }
    public class OrderStatusWithResultCount
    {
        public int OrderId { get; set; }
        public int ResultsCount { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? Expired { get; set; }
    }
    public class OrderResultModel
    {
        public string Sender { get; set; }
        public string Message { get; set; }
        public string PhoneNumber { get; set; }
        public string AudioUrl { get; set; }
        public string MessageType { get; set; }
    }
    public class PhoneNumberEfficiency
    {
        public string PhoneNumber { get; set; }
        public List<ServicePhoneNumberEfficiency> Services { get; set; }
    }
    public class CheckPhoneNumberEfficiencyRequest
    {
        public List<string> PhoneNumbers { get; set; }
    }
    public class SpecifiedServiceForComRequest
    {
        public List<int> ServiceProviderIds { get; set; }
        public bool SpecifiedService { get; set; }
        public List<int> ApplyFor { get; set; }
    }
    public class SpecifiedServiceForGsmRequest
    {
        public List<int> ServiceProviderIds { get; set; }
        public bool SpecifiedService { get; set; }
        public List<int> ApplyFor { get; set; }
    }
    public class AssignGsmToUserRequest
    {
        public List<int> GsmDeviceIds { get; set; }
    }
    public class ServicePhoneNumberEfficiency
    {
        public ServiceProvider ServiceProvider { get; set; }
        public int UsedCount { get; set; }
    }
    public class ClientSystemConfigurationModel
    {
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string BrandName { get; set; }
        public string FacebookUrl { get; set; }
        public string LogoUrl { get; set; }
        public string AdminNotification { get; set; }
        public DateTime ServerUtcNow { get; set; } = new Service.DateTimeService().UtcNow();
        public string YoutubeUrl { get; set; }
        public string TelegramUrl { get; set; }
        public decimal UsdRate { get; set; }
        public decimal ReferalFee { get; set; }
        public decimal ReferredUserFee { get; set; }
        // Transfer money fee
        public decimal InternalTransferFee { get; set; }
        public decimal ExternalTransferFee { get; set; }
        public int FrontendVersion { get; set; }
    }
    public class SmsHistoryCleanUpRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
    public class UserReceiptConfirmRequest
    {
        public string ReceiptCode { get; set; }
        public decimal Amount { get; set; }
        public string Comment { get; set; }
        public string AccountOwner { get; set; }
    }
    public class ServiceProviderWithAvailableCount : ServiceProvider
    {
        public ServiceProviderWithAvailableCount(ServiceProvider serviceProvider)
        {
            Created = serviceProvider.Created;
            CreatedBy = serviceProvider.CreatedBy;
            Disabled = serviceProvider.Disabled;
            ErrorThreshold = serviceProvider.ErrorThreshold;
            Guid = serviceProvider.Guid;
            Id = serviceProvider.Id;
            LockTime = serviceProvider.LockTime;
            MessageRegex = serviceProvider.MessageRegex;
            MessageCodeRegex = serviceProvider.MessageCodeRegex;
            Name = serviceProvider.Name;
            Price = serviceProvider.Price;
            Price2 = serviceProvider.Price2;
            Price3 = serviceProvider.Price3;
            Price4 = serviceProvider.Price4;
            Price5 = serviceProvider.Price5;
            ReceivingThreshold = serviceProvider.ReceivingThreshold;
            ServiceType = serviceProvider.ServiceType;
            Updated = serviceProvider.Updated;
            UpdatedBy = serviceProvider.UpdatedBy;
            AdditionalPrice = serviceProvider.AdditionalPrice;
            ServiceNetworkProviders = serviceProvider.ServiceNetworkProviders;
            AllowReceiveCall = serviceProvider.AllowReceiveCall;
            PriceReceiveCall = serviceProvider.PriceReceiveCall;
        }
        public int AvailableCount { get; set; }
    }
    public class ServiceProviderMatchingTokens : SmsMatchingTokens
    {
        public ServiceProvider ServiceProvider { get; set; }
    }

    public class SmsMatchingTokens
    {
        public List<string> ContentTokens { get; set; } = new List<string>();
        public List<string> SenderTokens { get; set; } = new List<string>();
    }

    public class TestMatchingSmsRequest
    {
        public string Sender { get; set; }
        public string Message { get; set; }
    }
    public class ServiceProviderAvailableCacheModel
    {
        public int ServiceProviderId { get; set; }
        public int AvailableCount { get; set; }
        public int AllCount { get; set; }
        public int ErrorCount { get; set; }
        public int SuccessCount { get; set; }
    }
    public class TransferMoneyRequest
    {
        [Required]
        public int ToUserId { get; set; }
        [Range(typeof(decimal), "10000", "500000000")]
        public decimal Amount { get; set; }
    }
    public class ExternalTransferMoneyRequest : TransferMoneyRequest
    {
        public string PortalName { get; set; }
    }
    public class SortableCom
    {
        public int SuccessedCount { get; set; }
        public Com Com { get; set; }
    }
    public class ExportOrdersRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<ServiceType> ServiceType { get; set; }
    }
}
