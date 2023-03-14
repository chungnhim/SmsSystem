using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class GsmDevice : BaseEntity
    {
        [MaxLength(10)]
        public string Code { get; set; }
        public string Name { get; set; }
        public string Note { get; set; }
        public bool Disabled { get; set; }
        public virtual ICollection<UserGsmDevice> UserGsmDevices { get; set; }
        public bool IsInMaintenance { get; set; }
        public DateTime? LastMaintenanceTime { get; set; }
        public bool IsServingForThirdService { get; set; }
        public int Priority { get; set; }
        public bool SpecifiedService { get; set; }
        public bool OnlyWebOrder { get; set; }
        public bool AllowVoiceOrder { get; set; }

        public virtual ICollection<GsmDeviceServiceProvider> GsmDeviceServiceProviders { get; set; }
        //[JsonIgnore]
        public virtual ICollection<Com> Coms { get; set; }
        public virtual ICollection<ComHistory> ComHistorys { get; set; }
        public virtual ICollection<ServiceProviderPhoneNumberLiveCheck> ServiceProviderPhoneNumberLiveChecks { get; set; }
        public DateTime LastActivedAt { get; set; }
    }
}