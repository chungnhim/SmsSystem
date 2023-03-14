using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class UserProfile : BaseEntity
    {
        public int UserId { get; set; }

        public string Email { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}
