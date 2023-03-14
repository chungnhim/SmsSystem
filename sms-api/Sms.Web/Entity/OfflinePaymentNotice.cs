using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class OfflinePaymentNotice : BaseEntity
    {
        public string ReceiptCode { get; set; }
        public decimal Amount { get; set; }
        public string ServiceProvider { get; set; }
        public string OptionalMessage { get; set; }
        public OfflinePaymentNoticeStatus Status { get; set; }
        public OfflinePaymentNoticeErrorReason ErrorReason { get; set; }
        public int? SolvedReceiptId { get; set; }
        [ForeignKey("SolvedReceiptId")]
        public UserOfflinePaymentReceipt Receipt { get; set; }
    }
}
