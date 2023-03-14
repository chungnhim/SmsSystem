using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Sms.Web.Helpers;

namespace Sms.Web.Entity
{
    public class SmsHistory : BaseEntity
    {
        public string Sender { get; set; }
        public string PhoneNumber { get; set; }
        public string Content { get; set; }
        public DateTime ReceivedDate { get; set; }

        [ForeignKey("Order")]
        public int? NotMappedOrderId { get; set; }

        public Order Order { get; set; }

        public ServiceType PermanentServiceType { get; set; }
        public string PermanentMessageRegex { get; set; }
        public SmsType SmsType { get; set; }
        public string AudioUrl { get; set; }
    }
}
