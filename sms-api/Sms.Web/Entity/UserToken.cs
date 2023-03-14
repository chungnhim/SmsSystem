using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class UserToken: BaseEntity
    {

        public Guid Token { get; set; }

        public DateTime Expired { get; set; }
        [ForeignKey("User")]
        public int UserId { get; set; }

        public User User { get; set; }
    }
}
