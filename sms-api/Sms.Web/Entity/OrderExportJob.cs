using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class OrderExportJob : BaseEntity
    {
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string ServiceType { get; set; }
        public OrderExportStatus Status { get; set; }
        public string UrlExport { get; set; }
        public User User { get; set; }
    }
}
