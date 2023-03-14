using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class ReferFee : BaseEntity
    {
        [Required]
        public DateTime ReportTime { get; set; }
        public int TotalOrderCount { get; set; }
        public decimal TotalCost { get; set; }
        public decimal FeeAmount { get; set; }
        public int UserId { get; set; }
        public decimal ReferFeePercent { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
    }

    public class UserReferalFee : ReferFee
    {
        public int ReferredUserId { get; set; }
        [ForeignKey("ReferredUserId")]
        public User ReferredUser { get; set; }
    }
    public class UserReferredFee : ReferFee
    {
    }
}
