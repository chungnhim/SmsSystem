using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class SystemAlert : BaseEntity
    {
        [MaxLength(20)]
        public string Topic { get; set; }
        [MaxLength(40)]
        public string Thread { get; set; }
        public string DetailJson { get; set; }
        public bool IsSent { get; set; }
    }
}
