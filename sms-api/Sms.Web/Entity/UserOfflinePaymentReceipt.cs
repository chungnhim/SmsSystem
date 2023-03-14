using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class UserOfflinePaymentReceipt : BaseEntity
    {
        public int UserId { get; set; }
        public string ReceiptCode { get; set; }
        public int? PaymentMethodConfigurationId { get; set; }

        public bool IsExpired { get; set; }
        public decimal Amount { get; set; }
        public string ReceiptResult { get; set; }
        public decimal? UserConfirmedAmount { get; set; }
        public string UserConfirmedComment { get; set; }
        public string UserConfirmedAccountOwner { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [ForeignKey("PaymentMethodConfigurationId")]
        public PaymentMethodConfiguration PaymentMethodConfiguration { get; set; }
    }
}
