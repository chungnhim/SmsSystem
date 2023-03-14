using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class Blog : BaseEntity
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public bool IsDisabled { get; set; }
        public int Priority { get; set; }
    }
}
