using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class OrderResult : BaseEntity
    {
        [ForeignKey("Order")]
        public int OrderId { get; set; }
        public string Message { get; set; }
        public string AudioUrl { get; set; }
        public string Sender { get; set; }
        public string PhoneNumber { get; set; }
        public int? SmsHistoryId { get; set; }
        [ForeignKey("SmsHistoryId")]
        public SmsHistory SmsHistory { get; set; }
        public Order Order { get; set; }
        public decimal Cost { get; set; }
        public SmsType SmsType { get; set; }
    }
}
