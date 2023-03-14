using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class PaymentMethodConfiguration : BaseEntity
    {
        public string BankCode { get; set; }
        public string Name { get; set; }
        public bool IsDisabled { get; set; }
        public string MessageFromAdmin { get; set; }
        public string Sender { get; set; }
        public string BankAccount { get; set; }
        public string OwnerName { get; set; }
        public string BankName { get; set; }
        public string Thumbnail { get; set; }
        public PaymentMethodType PaymentMethodType { get; set; }
    }
}
