using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class ServiceProviderPhoneNumberLiveCheck : BaseEntity
    {
        public int ServiceProviderId { get; set; }
        public LiveCheckStatus LiveCheckStatus { get; set; }

        public string PhoneNumber { get; set; }

        public ServiceProvider ServiceProvider { get; set; }

        public DateTime? PostedAt { get; set; }

        public DateTime? ReturnedAt { get; set; }
        public int? CheckBy { get; set; }

        [ForeignKey("GsmDeviceId")]
        public int? GsmDeviceId { get; set; }
        public GsmDevice GsmDevice { get; set; }
    }
}
