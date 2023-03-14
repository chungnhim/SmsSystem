using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Models
{
    public class RechargeRequestModel
    {
        public decimal Amount { get; set; }
        public bool IsImport { get; set; }
        public string Comment { get; set; }
    }
    public class ApplyUserErrorRequest
    {
        public int Count { get; set; }
        public bool IsSingle { get; set; }
    }
}
