using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class NewServiceSuggestion : BaseEntity
    {
        public string Name { get; set; }
        public string Sender { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}
