using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class SystemConfiguration : BaseEntity
    {
        public int? ThresholdsForAutoSuspend { get; set; }
        public int MaximumAvailableOrder { get; set; }
        public int MaximumAvailableInternationSimOrder { get; set; }
        public int? ThresholdsForWarning { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string BrandName { get; set; }
        public string FacebookUrl { get; set; }
        public string YoutubeUrl { get; set; }
        public string TelegramUrl { get; set; }
        public string LogoUrl { get; set; }
        public decimal UsdRate { get; set; }

        // for momo payment
        public string MomoApiEndPoint { get; set; }
        public string MomoAccessKey { get; set; }
        public string MomoPartnerCode { get; set; }
        public string MomoSecretKey { get; set; }

        // for Ngan Luong
        public bool NganLuongIsLiveEnvironment { get; set; }
        public string NganLuongLiveMerchantCode { get; set; }
        public string NganLuongLiveMerchantPassword { get; set; }
        public string NganLuongLiveReceiver { get; set; }
        public string NganLuongSandboxMerchantCode { get; set; }
        public string NganLuongSandboxMerchantPassword { get; set; }
        public string NganLuongSandboxReceiver { get; set; }

        // for Perfect Money
        public string PayeeAccount { get; set; }
        public string PayeeName { get; set; }
        public string PayeeSecretKey { get; set; }

        // admin notice
        public string AdminNotification { get; set; }
        public int? AutoCancelOrderDuration { get; set; }

        // for order warning
        public int? FloatingOrderWarning { get; set; }

        public int ServiceProviderContinuosFailed { get; set; }
        public int UserContinuosFailed { get; set; }
        public int GsmServiceProviderContinuosFailed { get; set; }

        public string BccEmail { get; set; }

        public decimal ReferalFee { get; set; }
        public decimal ReferredUserFee { get; set; }

        // Transfer money fee
        public decimal InternalTransferFee { get; set; }
        public decimal ExternalTransferFee { get; set; }

        public int FrontendVersion { get; set; }

        // Running archive or not
        public bool AllowArchiveOrder { get; set; }
        public bool AllowArchiveUserTransaction { get; set; }
    }
}
