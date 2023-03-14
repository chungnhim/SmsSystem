using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class Com : BaseEntity
    {
        [ForeignKey("GsmDevice")]
        public int GsmDeviceId { get; set; }
        public bool Disabled { get; set; }
        [MaxLength(10)]
        public string ComName { get; set; }
        [MaxLength(15)]
        public string PhoneNumber { get; set; }
        public int? NetworkProvider { get; set; }
        public GsmDevice GsmDevice { get; set; }
        public float? PhoneEfficiency { get; set; }
    }
}
