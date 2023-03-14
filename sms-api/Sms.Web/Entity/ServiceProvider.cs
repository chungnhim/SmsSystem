using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class ServiceProvider : BaseEntity
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal? Price2 { get; set; }
        public decimal? Price3 { get; set; }
        public decimal? Price4 { get; set; }
        public decimal? Price5 { get; set; }
        public int LockTime { get; set; }
        public string MessageRegex { get; set; }
        public ServiceType ServiceType { get; set; }
        public int ReceivingThreshold { get; set; }
        public bool Disabled { get; set; }
        public int? ErrorThreshold { get; set; }
        public int? TotalErrorThreshold { get; set; }
        public decimal? AdditionalPrice { get; set; }
        public string MessageCodeRegex { get; set; }
        public bool AllowReceiveCall { get; set; }
        public decimal? PriceReceiveCall { get; set; }

        public List<ServiceNetworkProvider> ServiceNetworkProviders { get; set; }
        public bool NeedLiveCheckBeforeUse { get; set; }
    }
}
