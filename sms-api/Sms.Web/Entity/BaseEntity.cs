using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public string Guid { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
