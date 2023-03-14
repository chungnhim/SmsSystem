using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class PhoneFailedCount : BaseEntity
    {
        public int GsmDeviceId { get; set; }
        public int TotalFailed { get; set; }
        public int ContinuousFailed { get; set; }
    }
}
