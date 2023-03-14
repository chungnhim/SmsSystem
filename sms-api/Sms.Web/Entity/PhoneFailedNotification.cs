using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class PhoneFailedNotification : BaseEntity
    {
        public int UserId { get; set; }
        public bool IsRead { get; set; }
        public int TotalFailed { get; set; }
        public int ContinuosFailed { get; set; }
        public string GsmDeviceCode { get; set; }
        public string GsmDeviceName { get; set; }
        public int GsmDeviceId { get; set; }
    }
}
