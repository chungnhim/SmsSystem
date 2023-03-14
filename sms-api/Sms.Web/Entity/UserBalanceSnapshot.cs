using Newtonsoft.Json;
using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class UserBalanceSnapshot : BaseEntity
    {
        public int UserId { get; set; }
        public decimal Balance { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Date { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}
