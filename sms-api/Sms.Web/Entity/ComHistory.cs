using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class ComHistory : BaseEntity
    {
        [ForeignKey("GsmDevice")]
        public int GsmDeviceId { get; set; }
        [MaxLength(10)]
        public string ComName { get; set; }
        [MaxLength(15)]
        public string OldPhoneNumber { get; set; }
        [MaxLength(15)]
        public string NewPhoneNumber { get; set; }
        public GsmDevice GsmDevice { get; set; }
    }
}
