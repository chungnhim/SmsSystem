using Newtonsoft.Json;
using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class UserTransaction : BaseEntity
    {
        [Required]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        public bool IsImport { get; set; }
        [Required]
        public decimal Amount { get; set; }
        [Required]
        public UserTransactionType UserTransactionType { get; set; }
        public string Comment { get; set; }
        public int? OrderId { get; set; }
        [ForeignKey("OrderId")]
        public Order Order { get; set; }
        public OfflinePaymentMethodType? OfflinePaymentMethodType { get; set; }
        public decimal? Balance { get; set; }
        public bool IsAdminConfirm { get; set; }
    }
}
