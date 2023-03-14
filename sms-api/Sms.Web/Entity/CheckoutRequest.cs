using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class CheckoutRequest : BaseEntity
    {
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public CheckoutRequestStatus Status { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }
        public string Comment { get; set; }

        public string BankCode { get; set; }
        public string BankAccountNumber { get; set; }
        public string BankAccountName { get; set; }
    }
}
