using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class OrderResultReport : BaseEntity
    {
        public OrderResultReportStatus OrderResultReportStatus { get; set; }
        public int OrderResultId { get; set; }
        public OrderResult OrderResult { get; set; }
    }
}
