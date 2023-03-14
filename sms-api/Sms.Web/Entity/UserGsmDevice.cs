using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class UserGsmDevice : BaseEntity
    {
        public int UserId { get; set; }
        public int GsmDeviceId { get; set; }

        [ForeignKey("GsmDeviceId")]
        public GsmDevice GsmDevice { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}
