using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class GsmDeviceServiceProvider : BaseEntity
    {
        public int GsmDeviceId { get; set; }
        public int ServiceProviderId { get; set; }

        [ForeignKey("ServiceProviderId")]
        public ServiceProvider ServiceProvider { get; set; }
        [ForeignKey("GsmDeviceId")]
        public GsmDevice GsmDevice { get; set; }
    }
}
