using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sms.Web.Entity
{
    public class ErrorPhoneLog : BaseEntity
    {
        public string PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public int ServiceProviderId { get; set; }
        [NotMapped]
        public int GsmDeviceId { get; set; }

        public virtual IList<ErrorPhoneLogUser> ErrorPhoneLogOrders { get; set; }
    }
}
