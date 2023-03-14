using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class Discount : BaseEntity
    {
        public int GsmDeviceId { get; set; }
        public int ServiceProviderId { get; set; }
        public float Percent { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }

        [ForeignKey("GsmDeviceId")]
        public GsmDevice GsmDevice { get; set; }
        [ForeignKey("ServiceProviderId")]
        public ServiceProvider ServiceProvider { get; set; }
    }
}
