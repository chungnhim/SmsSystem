using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class ServiceNetworkProvider : BaseEntity
    {
        public int ServiceProviderId { get; set; }
        public int NetworkProviderId { get; set; }
    }
}
