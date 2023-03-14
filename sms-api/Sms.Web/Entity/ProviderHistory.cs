using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class ProviderHistory: BaseEntity
    {
        [ForeignKey("ServiceProvider")]
        public int? ServiceProviderId { get; set; }
        public ServiceProvider ServiceProvider { get; set; }

        public string PhoneNumber { get; set; }
    }
}
