using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class UserPaymentTransaction : BaseEntity
    {
        public long Money { get; set; }
        public int UserId { get; set; }
        public string RequestId { get; set; }
        public bool IsExpired { get; set; }
    }
}
