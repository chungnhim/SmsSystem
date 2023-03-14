using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class OrderComplaint : BaseEntity
    {
        public OrderComplaintStatus OrderComplaintStatus { get; set; }
        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public Order Order { get; set; }
        public string UserComment { get; set; }
        public string AdminComment { get; set; }
    }
}
