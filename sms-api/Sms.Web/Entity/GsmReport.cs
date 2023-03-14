using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Sms.Web.Helpers;

namespace Sms.Web.Entity
{
    public class GsmReport : BaseEntity
    {
        public int Count { get; set; }
        public int GsmId { get; set; }
        public DateTime ReportedDate { get; set; }
        public int ServiceProviderId { get; set; }
        public OrderStatus OrderStatus { get; set; }
    }
}
